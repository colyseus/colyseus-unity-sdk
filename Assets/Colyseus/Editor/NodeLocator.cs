using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Colyseus.Editor
{
    /// <summary>
    /// Runs work off the main thread and hands results back to it. Unity's editor
    /// APIs (EditorPrefs, AssetDatabase, GUI) are main-thread only, and
    /// <c>EditorApplication.delayCall +=</c> is not safe from a worker thread.
    /// </summary>
    [InitializeOnLoad]
    internal static class MainThread
    {
        private static readonly ConcurrentQueue<Action> Queue = new ConcurrentQueue<Action>();

        static MainThread()
        {
            EditorApplication.update += Drain;
        }

        public static void Post(Action action) => Queue.Enqueue(action);

        public static void Drain()
        {
            while (Queue.TryDequeue(out var action))
            {
                try { action(); }
                catch (Exception ex) { Debug.LogException(ex); }
            }
        }

        public static void RunInBackground(string name, Action work)
        {
            new Thread(() => work()) { IsBackground = true, Name = "Colyseus." + name }.Start();
        }
    }

    /// <summary>
    /// Locates the user's Node.js installation and runs short-lived commands with it.
    ///
    /// A Unity Editor launched from Finder/Dock does NOT inherit the user's shell
    /// PATH, so an nvm-installed Node is invisible by default. Every tool in this
    /// package that shells out to Node goes through here so they share one
    /// (per-machine, project-scoped) configured directory and one detection result.
    /// </summary>
    internal static class NodeLocator
    {
        public static bool IsWindows => Application.platform == RuntimePlatform.WindowsEditor;

        private static readonly HashSet<string> KnownTools = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "node", "npm", "npx", "pnpm", "pnpx", "yarn", "bun", "bunx", "corepack",
        };

        // Scoped ".SchemaCodegen." for backwards compatibility: it predates this class.
        private static string NodeDirPrefKey => ProjectPaths.PrefKey("SchemaCodegen", "NodeDir");

        /// <summary>Main thread only (EditorPrefs).</summary>
        public static string NodeDir
        {
            get => EditorPrefs.GetString(NodeDirPrefKey, "");
            set => EditorPrefs.SetString(NodeDirPrefKey, value ?? "");
        }

        // ------------------------------------------------------------------
        // Status, shared by every window
        // ------------------------------------------------------------------

        public static bool Checked { get; private set; }
        public static bool Ok { get; private set; }
        public static string Version { get; private set; }

        /// <summary>Raised on the main thread when a check finishes.</summary>
        public static event Action StatusChanged;

        private static bool _checking;

        /// <summary>
        /// Makes sure a check has run, without blocking. The result survives domain
        /// reloads in SessionState, so a recompile does not spawn a login shell.
        /// </summary>
        public static void EnsureChecked()
        {
            if (Checked || _checking) return;

            var dir = NodeDir;
            if (SessionState.GetBool("Colyseus.Node.Checked", false) &&
                SessionState.GetString("Colyseus.Node.Dir", "") == dir)
            {
                Ok = SessionState.GetBool("Colyseus.Node.Ok", false);
                Version = SessionState.GetString("Colyseus.Node.Version", "");
                Checked = true;
                return;
            }

            Recheck(detect: string.IsNullOrEmpty(dir));
        }

        /// <summary>
        /// Re-runs detection (when asked, or nothing is configured) and the version
        /// check on a worker thread. Both spawn processes that can take seconds.
        /// </summary>
        public static void Recheck(bool detect = false)
        {
            if (_checking) return;
            _checking = true;

            var configured = NodeDir;
            MainThread.RunInBackground("NodeCheck", () =>
            {
                var dir = configured;
                if (detect || string.IsNullOrEmpty(dir))
                {
                    var found = DetectNodeDir();
                    if (!string.IsNullOrEmpty(found)) dir = found;
                }
                var result = CheckNode(dir);

                MainThread.Post(() =>
                {
                    _checking = false;
                    if (dir != configured) NodeDir = dir;
                    Ok = result.ok;
                    Version = result.version ?? "";
                    Checked = true;

                    SessionState.SetBool("Colyseus.Node.Checked", true);
                    SessionState.SetString("Colyseus.Node.Dir", dir ?? "");
                    SessionState.SetBool("Colyseus.Node.Ok", Ok);
                    SessionState.SetString("Colyseus.Node.Version", Version);

                    StatusChanged?.Invoke();
                });
            });
        }

        /// <summary>Runs `node --version` from <paramref name="dir"/>. Blocking.</summary>
        private static (bool ok, string version) CheckNode(string dir)
        {
            try
            {
                var result = RunQuick(ToolPath("node", dir), "--version", null, 5000);
                if (result.exitCode == 0 && !string.IsNullOrEmpty(result.stdout))
                    return (true, result.stdout.Trim());
            }
            catch { /* not found */ }
            return (false, null);
        }

        /// <summary>
        /// Best-effort discovery of the directory containing node/npx, or "".
        /// Blocking: it asks the user's login shell, which may load nvm.
        /// </summary>
        private static string DetectNodeDir()
        {
            // 1) Ask a login+interactive shell where node lives (picks up nvm).
            try
            {
                if (!IsWindows)
                {
                    var shell = Environment.GetEnvironmentVariable("SHELL");
                    var which = RunQuick(string.IsNullOrEmpty(shell) ? "/bin/zsh" : shell, "-lic \"command -v node\"", null, 5000);
                    var line = which.stdout?.Split('\n').Select(l => l.Trim()).LastOrDefault(l => l.EndsWith("/node"));
                    if (!string.IsNullOrEmpty(line) && File.Exists(line))
                        return Path.GetDirectoryName(line);
                }
                else
                {
                    var where = RunQuick("where", "node", null, 5000);
                    var line = where.stdout?.Split('\n').Select(l => l.Trim()).FirstOrDefault(l => l.Length > 0);
                    if (!string.IsNullOrEmpty(line) && File.Exists(line))
                        return Path.GetDirectoryName(line);
                }
            }
            catch { /* ignore */ }

            // 2) Well-known absolute locations.
            var candidates = IsWindows
                ? new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "nodejs"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "nodejs"),
                }
                : new[] { "/opt/homebrew/bin", "/usr/local/bin", "/usr/bin" };

            var nodeExe = IsWindows ? "node.exe" : "node";
            foreach (var dir in candidates)
            {
                if (File.Exists(Path.Combine(dir, nodeExe)))
                    return dir;
            }

            // 3) Newest nvm-installed version.
            try
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var nvm = Path.Combine(home, ".nvm", "versions", "node");
                if (Directory.Exists(nvm))
                {
                    var newest = Directory.GetDirectories(nvm)
                        .OrderByDescending(d => d) // version dirs sort lexically close enough for a default
                        .Select(d => Path.Combine(d, "bin"))
                        .FirstOrDefault(b => File.Exists(Path.Combine(b, "node")));
                    if (!string.IsNullOrEmpty(newest))
                        return newest;
                }
            }
            catch { /* ignore */ }

            return "";
        }

        // ------------------------------------------------------------------
        // Paths, environment and shells
        // ------------------------------------------------------------------

        /// <summary>
        /// Absolute path to a Node-family executable in <paramref name="dir"/>, or
        /// the bare name, which CreateProcess (node.exe) and cmd.exe (the package
        /// managers' .cmd shims) both resolve through PATH.
        /// </summary>
        public static string ToolPath(string tool, string dir)
        {
            if (string.IsNullOrEmpty(dir)) return tool;

            if (IsWindows)
            {
                // node is a real .exe; the package managers are .cmd shims.
                foreach (var ext in new[] { ".exe", ".cmd", ".bat", "" })
                {
                    var full = Path.Combine(dir, tool + ext);
                    if (File.Exists(full)) return full;
                }
                return tool;
            }

            var unix = Path.Combine(dir, tool);
            return File.Exists(unix) ? unix : tool;
        }

        public static bool IsKnownTool(string tool) => KnownTools.Contains(tool);

        /// <summary>
        /// PATH with the configured Node directory in front, and colors off: every
        /// child we start writes to a log view, not a terminal.
        /// </summary>
        public static void ApplyEnvironment(ProcessStartInfo psi)
        {
            var path = Environment.GetEnvironmentVariable("PATH") ?? "";
            var dir = NodeDir;
            psi.EnvironmentVariables["PATH"] = string.IsNullOrEmpty(dir) ? path : dir + Path.PathSeparator + path;
            psi.EnvironmentVariables["FORCE_COLOR"] = "0";
            psi.EnvironmentVariables["NO_COLOR"] = "1";
            if (!IsWindows) psi.EnvironmentVariables["TERM"] = "dumb";
        }

        /// <summary>
        /// Runs <paramref name="commandLine"/> through the platform shell: cmd.exe
        /// (the package managers are .cmd shims) or /bin/sh.
        /// </summary>
        public static void UseShell(ProcessStartInfo psi, string commandLine)
        {
            if (IsWindows)
            {
                psi.FileName = "cmd.exe";
                // /S makes cmd strip only the outermost quotes, which is what keeps
                // several quoted paths on one line working together.
                psi.Arguments = "/S /C \"" + commandLine + "\"";
            }
            else
            {
                psi.FileName = "/bin/sh";
                psi.Arguments = "-c " + ShellQuote(commandLine);
            }
        }

        /// <summary>Double-quotes an argument when it contains whitespace or shell-significant characters.</summary>
        public static string Quote(string s)
        {
            if (string.IsNullOrEmpty(s)) return "\"\"";
            if (s.IndexOfAny(new[] { ' ', '\t', '"', '*', '&', '|', '(', ')' }) < 0)
                return s;
            return "\"" + s.Replace("\"", "\\\"") + "\"";
        }

        /// <summary>POSIX single-quoting: everything inside is literal, unlike <see cref="Quote"/>.</summary>
        public static string ShellQuote(string s) => "'" + (s ?? "").Replace("'", "'\\''") + "'";

        // ------------------------------------------------------------------
        // Running commands
        // ------------------------------------------------------------------

        /// <summary>
        /// Runs a command to completion and returns its output. For short-lived
        /// probes only — it blocks the calling thread for up to the timeout.
        /// </summary>
        public static (int exitCode, string stdout, string stderr) RunQuick(
            string fileName, string arguments, string workingDir, int timeoutMs)
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            if (!string.IsNullOrEmpty(workingDir)) psi.WorkingDirectory = workingDir;

            using (var p = new Process { StartInfo = psi })
            {
                // Both streams are drained asynchronously. Reading either one to
                // the end synchronously would outlive the timeout whenever the
                // child keeps its pipe open — an interactive login shell will.
                var stdout = new StringBuilder();
                var stderr = new StringBuilder();
                p.OutputDataReceived += (s, e) => { if (e.Data != null) lock (stdout) stdout.AppendLine(e.Data); };
                p.ErrorDataReceived += (s, e) => { if (e.Data != null) lock (stderr) stderr.AppendLine(e.Data); };

                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();

                if (!p.WaitForExit(timeoutMs))
                {
                    try { p.Kill(); } catch { }
                    lock (stdout) lock (stderr) return (-1, stdout.ToString(), stderr.ToString());
                }

                // Lets the async handlers deliver whatever is still buffered.
                try { p.WaitForExit(); } catch { }
                lock (stdout) lock (stderr) return (p.ExitCode, stdout.ToString(), stderr.ToString());
            }
        }

        // ------------------------------------------------------------------
        // Shared UI
        // ------------------------------------------------------------------

        private const string NotFoundHelp =
            "Node.js was not found. Set the bin directory above, or (nvm users on macOS) symlink Node into a directory Unity can see:\n\n" +
            "  ln -s -f \"$(which node)\" /usr/local/bin/node\n" +
            "  ln -s -f \"$(which npx)\" /usr/local/bin/npx";

        private static readonly GUIContent NodeDirLabel = new GUIContent("Node bin directory",
            "Folder containing the 'node' and 'npx' executables. On macOS, a Unity Editor opened from Finder does not see nvm's Node — set this explicitly if auto-detect fails. Stored per-machine; not committed to git.");
        private static readonly GUIContent ConfigureLabel =
            new GUIContent("Configure…", "Point Unity at a different Node.js install");

        /// <summary>
        /// The Node status line every Node-backed window shows: a compact "node vX"
        /// once found, the directory field and help text otherwise.
        /// </summary>
        public static void DrawStatusRow(ref bool expanded, string trailing = null)
        {
            if (!Checked || (Ok && !expanded))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(4);
                    ColyseusEditorStyles.ColoredLabel("●", Checked ? ColyseusEditorStyles.Green : ColyseusEditorStyles.Idle,
                        ColyseusEditorStyles.MiniMuted, 12);
                    GUILayout.Label(Checked ? "node " + Version : "looking for node…", ColyseusEditorStyles.MiniMuted);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(trailing ?? "", ColyseusEditorStyles.MiniMuted);
                    GUILayout.Space(6);
                    using (new EditorGUI.DisabledScope(!Checked))
                    {
                        if (GUILayout.Button(ConfigureLabel, EditorStyles.miniLabel, GUILayout.Width(74))) expanded = true;
                    }
                    GUILayout.Space(4);
                }
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                var current = NodeDir;
                // Delayed: commits on Enter / focus-loss, not on every keystroke.
                var edited = EditorGUILayout.DelayedTextField(NodeDirLabel, current);
                if (edited != current)
                {
                    NodeDir = edited;
                    Recheck();
                }

                if (GUILayout.Button("Browse", EditorStyles.miniButtonLeft, GUILayout.Width(60)))
                {
                    ColyseusEditorStyles.Defer(() =>
                    {
                        var picked = EditorUtility.OpenFolderPanel("Select Node bin directory", NodeDir, "");
                        if (string.IsNullOrEmpty(picked)) return;
                        NodeDir = picked;
                        Recheck();
                    });
                }
                if (GUILayout.Button("Detect", EditorStyles.miniButtonRight, GUILayout.Width(60)))
                {
                    Recheck(detect: true);
                }
            }

            if (!Ok)
            {
                EditorGUILayout.HelpBox(NotFoundHelp, MessageType.Warning);
            }
            else
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Hide", EditorStyles.miniLabel, GUILayout.Width(40))) expanded = false;
                }
            }
        }
    }
}
