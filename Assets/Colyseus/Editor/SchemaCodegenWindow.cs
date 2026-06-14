using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Colyseus.Editor
{
    /// <summary>
    /// Unity Editor window that runs the <c>schema-codegen</c> tool (from the
    /// <c>@colyseus/schema</c> npm package) to generate client-side C# schema
    /// classes from the server's TypeScript schema definitions.
    ///
    /// The tool is a Node.js CLI, so this window shells out to <c>npx</c> via
    /// <see cref="System.Diagnostics.Process"/>. The trickiest part on macOS is
    /// that a Unity Editor launched from Finder/Dock does NOT inherit the user's
    /// shell PATH, so an nvm-installed Node is invisible by default. We handle
    /// that with: (1) a configurable, persisted Node directory, (2) a best-effort
    /// auto-detect, and (3) a documented /usr/local/bin symlink fallback shown in
    /// the UI when Node can't be found.
    /// </summary>
    public class SchemaCodegenWindow : EditorWindow
    {
        private static bool IsWindows => Application.platform == RuntimePlatform.WindowsEditor;

        // --- Persisted settings (project-scoped) ---
        private string _nodeDir;     // directory containing node / npx
        private string _source;      // .ts file, folder, or glob (e.g. "src/schema/*.ts")
        private string _output;      // output directory for generated .cs
        private string _namespace;   // optional --namespace
        private string _version;     // optional @colyseus/schema version pin (blank = latest / local)
        private bool _bundle;        // --bundle (single file)

        // --- Node presence check ---
        private string _nodeVersion;
        private bool _nodeOk;

        // --- Process / log state ---
        private Process _process;
        private readonly StringBuilder _log = new StringBuilder();
        private readonly object _logLock = new object();
        private volatile bool _running;
        private volatile bool _completed;
        private volatile int _exitCode;
        private string _logSnapshot = "";
        private Vector2 _logScroll;

        [MenuItem("Window/Colyseus/Schema Codegen")]
        public static void ShowWindow()
        {
            Debug.LogWarning("Found an issue with the Colyseus Schema Codegen window? Please report it at https://github.com/colyseus/colyseus-unity-sdk/issues");
            var window = GetWindow<SchemaCodegenWindow>("Schema Codegen");
            window.minSize = new Vector2(460, 420);
            window.Show();
        }

        private void OnEnable()
        {
            LoadPrefs();
            EditorApplication.update += OnEditorUpdate;
            if (string.IsNullOrEmpty(_nodeDir))
                _nodeDir = DetectNodeDir();
            CheckNode();
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            SaveNodeDir();
            SaveShared();
            KillProcess();
        }

        // ------------------------------------------------------------------
        // GUI
        // ------------------------------------------------------------------

        private void OnGUI()
        {
            EditorGUILayout.Space();
            DrawNodeSection();
            EditorGUILayout.Space();
            DrawInputsSection();
            EditorGUILayout.Space();
            DrawActionsSection();
            EditorGUILayout.Space();
            DrawLogSection();
        }

        private void DrawNodeSection()
        {
            EditorGUILayout.LabelField("Node.js", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            // Delayed: only commits (and re-checks Node) on Enter / focus-loss,
            // so we don't spawn 'node --version' on every keystroke.
            _nodeDir = EditorGUILayout.DelayedTextField(new GUIContent("Node bin directory",
                "Folder containing the 'node' and 'npx' executables. On macOS, a Unity Editor opened from Finder does not see nvm's Node — set this explicitly if auto-detect fails. Stored per-machine; not committed to git."), _nodeDir);
            if (EditorGUI.EndChangeCheck())
            {
                CheckNode();
                SaveNodeDir();
            }

            if (GUILayout.Button("Browse", GUILayout.Width(70)))
            {
                var picked = EditorUtility.OpenFolderPanel("Select Node bin directory", _nodeDir ?? "", "");
                if (!string.IsNullOrEmpty(picked))
                {
                    _nodeDir = picked;
                    CheckNode();
                    SaveNodeDir();
                }
            }
            if (GUILayout.Button("Detect", GUILayout.Width(70)))
            {
                _nodeDir = DetectNodeDir();
                CheckNode();
                SaveNodeDir();
            }
            EditorGUILayout.EndHorizontal();

            if (_nodeOk)
            {
                EditorGUILayout.LabelField(" ", $"✔ node {_nodeVersion}", MiniGreen());
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Node.js was not found. Set the bin directory above, or (nvm users on macOS) symlink Node into a directory Unity can see:\n\n" +
                    "  ln -s -f \"$(which node)\" /usr/local/bin/node\n" +
                    "  ln -s -f \"$(which npx)\" /usr/local/bin/npx",
                    MessageType.Warning);
            }
        }

        private void DrawInputsSection()
        {
            EditorGUILayout.LabelField("Generation", EditorStyles.boldLabel);

            // Delayed fields commit on Enter / focus-loss, so we persist to the
            // shared settings file once per edit instead of on every keystroke.
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginHorizontal();
            _source = EditorGUILayout.DelayedTextField(new GUIContent("Schema source",
                "A TypeScript file, a folder of *.ts files, or a glob (e.g. src/rooms/schema/*.ts). This usually lives in your server project."), _source);
            if (GUILayout.Button("File", GUILayout.Width(50)))
            {
                var picked = EditorUtility.OpenFilePanel("Select schema .ts file", StartDir(_source), "ts");
                if (!string.IsNullOrEmpty(picked)) { _source = picked; SaveShared(); }
            }
            if (GUILayout.Button("Folder", GUILayout.Width(60)))
            {
                var picked = EditorUtility.OpenFolderPanel("Select schema folder", StartDir(_source), "");
                if (!string.IsNullOrEmpty(picked)) { _source = picked; SaveShared(); }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _output = EditorGUILayout.DelayedTextField(new GUIContent("Output directory",
                "Where generated .cs files are written. Keep it under Assets/ so Unity imports them automatically."), _output);
            if (GUILayout.Button("Browse", GUILayout.Width(70)))
            {
                var picked = EditorUtility.OpenFolderPanel("Select output directory", StartDir(_output) ?? Application.dataPath, "");
                if (!string.IsNullOrEmpty(picked)) { _output = picked; SaveShared(); }
            }
            EditorGUILayout.EndHorizontal();

            _namespace = EditorGUILayout.DelayedTextField(new GUIContent("Namespace (optional)",
                "Wraps generated classes in 'namespace <value> { ... }'."), _namespace);

            _version = EditorGUILayout.DelayedTextField(new GUIContent("@colyseus/schema version",
                "Pin a version to match your server (e.g. 4.0.7). Leave blank to use your project's local install, or the latest published version."), _version);

            _bundle = EditorGUILayout.Toggle(new GUIContent("Bundle into single file",
                "Passes --bundle so all classes are written to a single .cs file."), _bundle);

            if (EditorGUI.EndChangeCheck())
                SaveShared();

            // Resolved-files preview
            var resolved = SafeResolveInputs(_source);
            if (!string.IsNullOrEmpty(_source))
            {
                EditorGUILayout.LabelField(" ",
                    resolved.Count == 0 ? "No .ts files matched." : $"{resolved.Count} file(s) matched.",
                    EditorStyles.miniLabel);
            }

            EditorGUILayout.LabelField(" ",
                "Shared via git: ProjectSettings/Packages/io.colyseus.sdk/SchemaCodegen.json",
                EditorStyles.miniLabel);
        }

        private void DrawActionsSection()
        {
            using (new EditorGUI.DisabledScope(_running || !_nodeOk))
            {
                if (GUILayout.Button(_running ? "Generating…" : "Generate", GUILayout.Height(28)))
                    Generate();
            }

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(!_running))
            {
                if (GUILayout.Button("Cancel"))
                    KillProcess();
            }
            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_output) || !Directory.Exists(_output)))
            {
                if (GUILayout.Button("Open Output Folder"))
                    EditorUtility.RevealInFinder(_output);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawLogSection()
        {
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
            _logScroll = EditorGUILayout.BeginScrollView(_logScroll, GUILayout.ExpandHeight(true));
            EditorGUILayout.TextArea(_logSnapshot, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        // ------------------------------------------------------------------
        // Update loop (marshal process results back onto the main thread)
        // ------------------------------------------------------------------

        private void OnEditorUpdate()
        {
            if (_running)
            {
                lock (_logLock) { _logSnapshot = _log.ToString(); }
                Repaint();
            }

            if (_completed)
            {
                _completed = false;
                _running = false;

                AppendLine(_exitCode == 0
                    ? "\n✔ Done (exit 0)"
                    : $"\n✘ Failed (exit {_exitCode})");
                lock (_logLock) { _logSnapshot = _log.ToString(); }

                if (_exitCode == 0 && IsUnderAssets(_output))
                    AssetDatabase.Refresh();
                else if (_exitCode == 0)
                    AppendLine("Note: output is outside Assets/, so it was not imported automatically.");

                KillProcess();
                Repaint();
            }
        }

        // ------------------------------------------------------------------
        // Generation
        // ------------------------------------------------------------------

        private void Generate()
        {
            SaveNodeDir();
            SaveShared();

            var inputs = SafeResolveInputs(_source);
            if (inputs.Count == 0)
            {
                EditorUtility.DisplayDialog("Schema Codegen", "No .ts files matched the schema source.", "OK");
                return;
            }
            if (string.IsNullOrEmpty(_output))
            {
                EditorUtility.DisplayDialog("Schema Codegen", "Please choose an output directory.", "OK");
                return;
            }

            var workingDir = WorkingDirFor(inputs[0]);
            var args = BuildArgs(inputs, workingDir);
            var psi = BuildStartInfo(args, workingDir);

            lock (_logLock)
            {
                _log.Clear();
                _log.AppendLine($"$ {psi.FileName} {psi.Arguments}");
                _log.AppendLine($"(cwd: {workingDir})");
                _log.AppendLine();
            }

            _running = true;
            _completed = false;

            try
            {
                _process = new Process { StartInfo = psi, EnableRaisingEvents = true };
                _process.OutputDataReceived += (s, e) => { if (e.Data != null) AppendLine(e.Data); };
                _process.ErrorDataReceived += (s, e) => { if (e.Data != null) AppendLine(e.Data); };
                _process.Exited += (s, e) =>
                {
                    try { _exitCode = _process.ExitCode; } catch { _exitCode = -1; }
                    _completed = true;
                };
                _process.Start();
                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                AppendLine("ERROR launching process: " + ex.Message);
                _running = false;
                KillProcess();
            }
        }

        /// <summary>
        /// Builds the npx argument list. Prefers a locally installed
        /// schema-codegen (so the server project's pinned version is used);
        /// otherwise fetches via <c>-p @colyseus/schema[@version]</c>.
        /// </summary>
        private List<string> BuildArgs(List<string> inputs, string workingDir)
        {
            var args = new List<string>();

            bool hasLocalBin = File.Exists(Path.Combine(workingDir, "node_modules", ".bin",
                IsWindows ? "schema-codegen.cmd" : "schema-codegen"));

            if (hasLocalBin && string.IsNullOrEmpty(_version))
            {
                args.Add("schema-codegen"); // npx resolves the local bin
            }
            else
            {
                args.Add("-p");
                args.Add("@colyseus/schema" + (string.IsNullOrEmpty(_version) ? "" : "@" + _version.Trim()));
                args.Add("schema-codegen");
            }

            args.AddRange(inputs);
            args.Add("--csharp");
            args.Add("--output");
            args.Add(_output);

            if (!string.IsNullOrEmpty(_namespace))
            {
                args.Add("--namespace");
                args.Add(_namespace.Trim());
            }
            if (_bundle)
                args.Add("--bundle");

            return args;
        }

        private ProcessStartInfo BuildStartInfo(List<string> args, string workingDir)
        {
            var psi = new ProcessStartInfo
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDir
            };

            // Make the configured Node visible to the child process.
            var path = Environment.GetEnvironmentVariable("PATH") ?? "";
            if (!string.IsNullOrEmpty(_nodeDir))
                path = _nodeDir + Path.PathSeparator + path;
            psi.EnvironmentVariables["PATH"] = path;

            if (IsWindows)
            {
                // .cmd shims must run through cmd.exe.
                var npx = NpxPath();
                var inner = Quote(npx) + " " + string.Join(" ", args.Select(Quote));
                psi.FileName = "cmd.exe";
                psi.Arguments = "/c \"" + inner + "\"";
            }
            else
            {
                psi.FileName = NpxPath();
                psi.Arguments = string.Join(" ", args.Select(Quote));
            }

            return psi;
        }

        private string NpxPath()
        {
            var exe = IsWindows ? "npx.cmd" : "npx";
            if (!string.IsNullOrEmpty(_nodeDir))
            {
                var full = Path.Combine(_nodeDir, exe);
                if (File.Exists(full)) return full;
            }
            return exe; // fall back to PATH resolution
        }

        // ------------------------------------------------------------------
        // Node discovery & presence check
        // ------------------------------------------------------------------

        private void CheckNode()
        {
            _nodeOk = false;
            _nodeVersion = null;

            var node = IsWindows ? "node.exe" : "node";
            var nodePath = !string.IsNullOrEmpty(_nodeDir) ? Path.Combine(_nodeDir, node) : node;

            try
            {
                var result = RunQuick(nodePath, "--version", null, 5000);
                if (result.exitCode == 0 && !string.IsNullOrEmpty(result.stdout))
                {
                    _nodeVersion = result.stdout.Trim();
                    _nodeOk = true;
                }
            }
            catch { /* not found */ }
        }

        /// <summary>
        /// Best-effort discovery of the directory containing node/npx.
        /// Mirrors how BabylonToolkit handles it: probe well-known locations
        /// (where GUI-launched Unity can see them) and the user's nvm install.
        /// </summary>
        private string DetectNodeDir()
        {
            // 1) Ask a login+interactive shell where node lives (picks up nvm).
            try
            {
                var shell = Environment.GetEnvironmentVariable("SHELL");
                if (!IsWindows)
                {
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

            return _nodeDir ?? "";
        }

        private (int exitCode, string stdout, string stderr) RunQuick(string fileName, string arguments, string workingDir, int timeoutMs)
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
                p.Start();
                var stdout = p.StandardOutput.ReadToEnd();
                var stderr = p.StandardError.ReadToEnd();
                if (!p.WaitForExit(timeoutMs))
                {
                    try { p.Kill(); } catch { }
                    return (-1, stdout, stderr);
                }
                return (p.ExitCode, stdout, stderr);
            }
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private List<string> SafeResolveInputs(string src)
        {
            try { return ResolveInputs(src); }
            catch { return new List<string>(); }
        }

        private static List<string> ResolveInputs(string src)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(src)) return result;

            if (Directory.Exists(src))
            {
                result.AddRange(Directory.GetFiles(src, "*.ts").Where(f => !f.EndsWith(".d.ts")));
            }
            else if (File.Exists(src))
            {
                result.Add(src);
            }
            else if (src.Contains("*"))
            {
                var dir = Path.GetDirectoryName(src);
                var pattern = Path.GetFileName(src);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    result.AddRange(Directory.GetFiles(dir, pattern));
            }
            return result;
        }

        private static string WorkingDirFor(string firstInput)
        {
            var dir = Directory.Exists(firstInput) ? firstInput : Path.GetDirectoryName(firstInput);
            // Walk up to a package.json so npx can find a local install.
            var probe = dir;
            for (int i = 0; i < 8 && !string.IsNullOrEmpty(probe); i++)
            {
                if (File.Exists(Path.Combine(probe, "package.json")))
                    return probe;
                probe = Path.GetDirectoryName(probe);
            }
            return dir;
        }

        private static bool IsUnderAssets(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            var full = Path.GetFullPath(path).Replace('\\', '/');
            var assets = Path.GetFullPath(Application.dataPath).Replace('\\', '/');
            return full.StartsWith(assets, StringComparison.OrdinalIgnoreCase);
        }

        private static string StartDir(string path)
        {
            if (string.IsNullOrEmpty(path)) return Application.dataPath;
            if (Directory.Exists(path)) return path;
            var dir = Path.GetDirectoryName(path);
            return Directory.Exists(dir) ? dir : Application.dataPath;
        }

        private static string Quote(string s)
        {
            if (string.IsNullOrEmpty(s)) return "\"\"";
            // Quote when there is whitespace or shell-significant chars.
            if (s.IndexOfAny(new[] { ' ', '\t', '"', '*', '&', '|', '(', ')' }) < 0)
                return s;
            return "\"" + s.Replace("\"", "\\\"") + "\"";
        }

        private void AppendLine(string line)
        {
            lock (_logLock) { _log.AppendLine(line); }
        }

        private void KillProcess()
        {
            if (_process == null) return;
            try { if (!_process.HasExited) _process.Kill(); } catch { }
            try { _process.Dispose(); } catch { }
            _process = null;
        }

        private static GUIStyle MiniGreen()
        {
            var style = new GUIStyle(EditorStyles.miniLabel);
            style.normal.textColor = new Color(0.3f, 0.7f, 0.3f);
            return style;
        }

        // ------------------------------------------------------------------
        // Persistence
        //   - Node bin directory: per-machine (EditorPrefs), never committed.
        //   - Everything else: shared via git in ProjectSettings/, so the whole
        //     team uses the same source/output/namespace/version/bundle.
        // ------------------------------------------------------------------

        private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;

        private static string SettingsFilePath => Path.Combine(
            ProjectRoot, "ProjectSettings", "Packages", "io.colyseus.sdk", "SchemaCodegen.json");

        // Project-scoped so different projects on the same machine don't collide.
        private static string NodeDirPrefKey =>
            $"Colyseus.SchemaCodegen.{Application.dataPath.GetHashCode():X8}.NodeDir";

        [Serializable]
        private class CodegenSettings
        {
            public string source = "";
            public string output = "";
            public string @namespace = "";
            public string version = "";
            public bool bundle = false;
        }

        private void LoadPrefs()
        {
            _nodeDir = EditorPrefs.GetString(NodeDirPrefKey, "");
            LoadShared();
        }

        private void LoadShared()
        {
            try
            {
                var path = SettingsFilePath;
                if (!File.Exists(path)) return;

                var data = JsonUtility.FromJson<CodegenSettings>(File.ReadAllText(path));
                if (data == null) return;

                _source = FromStored(data.source);
                _output = FromStored(data.output);
                _namespace = data.@namespace ?? "";
                _version = data.version ?? "";
                _bundle = data.bundle;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Colyseus] Failed to read codegen settings: {ex.Message}");
            }
        }

        private void SaveShared()
        {
            try
            {
                var path = SettingsFilePath;

                // Don't create an empty committed file before anything is configured.
                bool empty = string.IsNullOrEmpty(_source) && string.IsNullOrEmpty(_output)
                    && string.IsNullOrEmpty(_namespace) && string.IsNullOrEmpty(_version) && !_bundle;
                if (empty && !File.Exists(path)) return;

                var data = new CodegenSettings
                {
                    source = ToStored(_source),
                    output = ToStored(_output),
                    @namespace = _namespace ?? "",
                    version = _version ?? "",
                    bundle = _bundle,
                };

                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, JsonUtility.ToJson(data, true));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Colyseus] Failed to write codegen settings: {ex.Message}");
            }
        }

        private void SaveNodeDir()
        {
            EditorPrefs.SetString(NodeDirPrefKey, _nodeDir ?? "");
        }

        // Paths are stored relative to the project root (forward slashes) so they
        // resolve on a teammate's machine. Absolute paths are kept only when no
        // sensible relative path exists (e.g. a different Windows drive).
        private static string ToStored(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";
            string full;
            try { full = Path.GetFullPath(path); }
            catch { return path; }
            return MakeRelative(ProjectRoot, full).Replace('\\', '/');
        }

        private static string FromStored(string stored)
        {
            if (string.IsNullOrEmpty(stored)) return "";
            var p = stored.Replace('/', Path.DirectorySeparatorChar);
            if (Path.IsPathRooted(p)) return p;
            try { return Path.GetFullPath(Path.Combine(ProjectRoot, p)); }
            catch { return p; }
        }

        private static string MakeRelative(string fromDir, string toPath)
        {
            try
            {
                var fromUri = new Uri(AppendSlash(fromDir));
                var toUri = new Uri(toPath);
                if (fromUri.Scheme != toUri.Scheme) return toPath; // different volume/scheme
                var rel = Uri.UnescapeDataString(fromUri.MakeRelativeUri(toUri).ToString());
                return string.IsNullOrEmpty(rel) ? toPath : rel;
            }
            catch { return toPath; }
        }

        private static string AppendSlash(string dir)
        {
            return dir.EndsWith(Path.DirectorySeparatorChar.ToString()) || dir.EndsWith("/")
                ? dir : dir + Path.DirectorySeparatorChar;
        }
    }
}
