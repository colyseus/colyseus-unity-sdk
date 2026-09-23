using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Colyseus.Editor
{
    /// <summary>
    /// Runs <c>schema-codegen</c> (from the <c>@colyseus/schema</c> npm package) to
    /// generate client-side C# schema classes from the server's TypeScript
    /// definitions.
    ///
    /// The tool is a Node.js CLI, so this shells out to <c>npx</c>. Finding Node is
    /// <see cref="NodeLocator"/>'s job; what lives here is the argument building
    /// and the source/output configuration.
    /// </summary>
    public class SchemaCodegenWindow : ColyseusWindow
    {
        private static readonly GUIContent SourceLabel = new GUIContent("Schema source",
            "A TypeScript file, a folder of *.ts files, or a glob such as src/rooms/schema/**/*.ts. This usually lives in your server project.");
        private static readonly GUIContent DetectLabel = new GUIContent("Detect", "Search your server project for schema definitions");
        private static readonly GUIContent OutputLabel = new GUIContent("Output directory",
            "Where generated .cs files are written. Keep it under Assets/ so Unity imports them automatically.");
        private static readonly GUIContent NamespaceLabel = new GUIContent("Namespace (optional)",
            "Wraps generated classes in 'namespace <value> { ... }'.");
        private static readonly GUIContent VersionLabel = new GUIContent("@colyseus/schema version",
            "Pin a version to match your server (e.g. 3.0.42). Leave blank to use your project's local install, or the latest published version.");
        private static readonly GUIContent BundleLabel = new GUIContent("Bundle into single file",
            "Passes --bundle so all classes are written to a single .cs file.");
        private static readonly GUIContent CancelLabel = new GUIContent("Cancel", "Stop the running generation");
        private static readonly GUIContent OpenOutputLabel = new GUIContent("Open Output", "Reveal the output folder");
        private const string SharedHint = "shared via ProjectSettings/Packages/io.colyseus.sdk/" + CodegenSettings.FileName;

        private readonly LogView _logView = new LogView();
        private readonly LogBuffer _log = new LogBuffer();
        private CodegenSettings _settings;
        private bool _showNodeSettings;
        private double _nextRepaintAt;

        // Process state. The codegen run is short-lived, so unlike the game server
        // it uses ordinary pipes and does not try to survive a domain reload.
        private Process _process;
        private readonly ConcurrentQueue<string> _incoming = new ConcurrentQueue<string>();
        private volatile bool _running;
        private volatile bool _completed;
        private volatile int _exitCode;
        /// <summary>Set when the user cancels, so the exit is not reported as a failure.</summary>
        private volatile bool _cancelled;
        private bool _hasRun;

        // The "N files matched" hint walks the schema source, which for a **
        // glob means a recursive scan — far too expensive to redo every repaint.
        private string _matchedFor;
        private int _matchedCount;

        // Captured on the Layout event (see ColyseusWindow).
        private bool _uiRunning;
        private bool _uiHasRun;
        private int _uiExitCode;
        private string _uiOutput = "";
        private bool _uiOutputExists;

        [MenuItem("Window/Colyseus/Schema Codegen")]
        public static void ShowWindow()
        {
            var window = GetWindow<SchemaCodegenWindow>();
            window.minSize = new Vector2(480, 420);
            window.Show();
        }

        /// <summary>
        /// Runs a generation using the settings committed to the project, opening
        /// the window so the output is visible. False when nothing is configured yet.
        /// </summary>
        public static bool GenerateFromSavedSettings()
        {
            var saved = CodegenSettings.Load();
            if (!saved.IsConfigured) return false;

            var window = GetWindow<SchemaCodegenWindow>();
            window.Show();
            window.Focus();
            window._settings = saved;
            window.Generate();
            return true;
        }

        // ------------------------------------------------------------------
        // Lifetime
        // ------------------------------------------------------------------

        private void OnEnable()
        {
            titleContent = ColyseusEditorStyles.TitleContent("Schema Codegen");
            _settings = CodegenSettings.Load();
            NodeLocator.EnsureChecked();

            _logView.ClearRequested += ClearLog;
            NodeLocator.StatusChanged += Repaint;
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            _logView.ClearRequested -= ClearLog;
            NodeLocator.StatusChanged -= Repaint;
            EditorApplication.update -= OnEditorUpdate;
            _settings.Save();
            KillProcess();
        }

        private void ClearLog()
        {
            _log.Clear();
            Repaint();
        }

        private void OnEditorUpdate()
        {
            var appended = false;
            while (_incoming.TryDequeue(out var line)) appended |= _log.AppendAuto(line);

            if (_completed)
            {
                _completed = false;
                OnCompleted();
                appended = true;
            }

            // New output repaints at once; the "Generating…" state only needs a
            // slow heartbeat, not a repaint on every editor tick.
            var now = EditorApplication.timeSinceStartup;
            if (appended || (_running && now >= _nextRepaintAt))
            {
                _nextRepaintAt = now + 0.25;
                Repaint();
            }
        }

        private void OnCompleted()
        {
            if (_cancelled)
            {
                // The user stopped it; don't report the kill as a failure.
                _cancelled = false;
                KillProcess();
                return;
            }

            _running = false;
            _hasRun = true;

            KillProcess(); // waits for the readers, so late lines land first
            while (_incoming.TryDequeue(out var pending)) _log.AppendAuto(pending);

            if (_exitCode != 0)
            {
                _log.Append($"✘ Failed (exit {_exitCode})", LogLevel.Error);
                return;
            }

            _log.Append("✔ Done (exit 0)", LogLevel.Success);
            if (ProjectPaths.IsUnderAssets(_settings.ResolvedOutput))
                AssetDatabase.Refresh();
            else
                _log.Append("Note: the output is outside Assets/, so Unity did not import it.", LogLevel.Warning);
        }

        // ------------------------------------------------------------------
        // GUI
        // ------------------------------------------------------------------

        protected override void CaptureState()
        {
            _uiRunning = _running;
            _uiHasRun = _hasRun;
            _uiExitCode = _exitCode;
            _uiOutput = _settings.ResolvedOutput;
            _uiOutputExists = !string.IsNullOrEmpty(_uiOutput) && Directory.Exists(_uiOutput);
        }

        protected override void DrawContents()
        {
            DrawHeader();
            DrawStatus();
            DrawInputs();
            NodeLocator.DrawStatusRow(ref _showNodeSettings, SharedHint);

            GUILayout.Space(2);
            _logView.DrawToolbar(_log, DrawLogToolbarExtras);
            _logView.Draw(_log, "Press Generate to run schema-codegen.", GUILayout.ExpandHeight(true));
        }

        private void DrawHeader()
        {
            ColyseusEditorStyles.DrawHeader("Schema Codegen", () =>
            {
                using (new EditorGUI.DisabledScope(_uiRunning || !NodeLocator.Ok || string.IsNullOrEmpty(_settings.source)))
                {
                    if (ColyseusEditorStyles.TintedButton(
                            ColyseusEditorStyles.IconLabel("PlayButton", _uiRunning ? "Generating…" : "Generate",
                                "Run schema-codegen with the settings below"),
                            ColyseusEditorStyles.StartTint, 96))
                    {
                        ColyseusEditorStyles.Defer(Generate);
                    }
                }

                using (new EditorGUI.DisabledScope(!_uiRunning))
                {
                    if (ColyseusEditorStyles.TintedButton(CancelLabel, ColyseusEditorStyles.StopTint, 60))
                        ColyseusEditorStyles.Defer(Cancel);
                }

                DrawMenuButton();
            });
        }

        private void DrawStatus()
        {
            if (_uiRunning)
            {
                ColyseusEditorStyles.DrawStatusRow(ColyseusEditorStyles.Amber, "Generating…", null);
            }
            else if (!_uiHasRun)
            {
                var matched = MatchedFileCount();
                ColyseusEditorStyles.DrawStatusRow(ColyseusEditorStyles.Idle, "Ready",
                    string.IsNullOrEmpty(_settings.source)
                        ? "no schema source set"
                        : $"{matched} file{(matched == 1 ? "" : "s")} matched");
            }
            else if (_uiExitCode == 0)
            {
                ColyseusEditorStyles.DrawStatusRow(ColyseusEditorStyles.Green, "Generated", _settings.output);
            }
            else
            {
                ColyseusEditorStyles.DrawStatusRow(ColyseusEditorStyles.Red, $"Failed (exit {_uiExitCode})", null);
            }
        }

        /// <summary>Cached against the source string; call <see cref="InvalidateMatches"/> to force a re-scan.</summary>
        private int MatchedFileCount()
        {
            var source = _settings.ResolvedSource;
            if (_matchedFor == source) return _matchedCount;
            _matchedFor = source;
            _matchedCount = SafeResolveInputs(source).Count;
            return _matchedCount;
        }

        private void InvalidateMatches() => _matchedFor = null;

        private void DrawInputs()
        {
            GUILayout.Space(4);

            using (new EditorGUI.DisabledScope(_uiRunning))
            {
                EditorGUI.BeginChangeCheck();

                using (new EditorGUILayout.HorizontalScope())
                {
                    var edited = EditorGUILayout.DelayedTextField(SourceLabel, _settings.source);
                    if (edited != _settings.source) _settings.SetSource(ProjectPaths.FromStored(edited));

                    if (GUILayout.Button(DetectLabel, EditorStyles.miniButtonLeft, GUILayout.Width(54)))
                        ColyseusEditorStyles.Defer(DetectSchemaSource);
                    if (GUILayout.Button("File", EditorStyles.miniButtonMid, GUILayout.Width(40)))
                        PickPath(folder: false, output: false);
                    if (GUILayout.Button("Folder", EditorStyles.miniButtonRight, GUILayout.Width(50)))
                        PickPath(folder: true, output: false);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    var edited = EditorGUILayout.DelayedTextField(OutputLabel, _settings.output);
                    if (edited != _settings.output) _settings.SetOutput(ProjectPaths.FromStored(edited));

                    if (GUILayout.Button("Browse", EditorStyles.miniButton, GUILayout.Width(60)))
                        PickPath(folder: true, output: true);
                }

                _settings.@namespace = EditorGUILayout.DelayedTextField(NamespaceLabel, _settings.@namespace);
                _settings.version = EditorGUILayout.DelayedTextField(VersionLabel, _settings.version);
                _settings.bundle = EditorGUILayout.Toggle(BundleLabel, _settings.bundle);

                if (EditorGUI.EndChangeCheck())
                {
                    _settings.Save();
                    InvalidateMatches();
                }
            }

            if (string.IsNullOrEmpty(_settings.output))
            {
                EditorGUILayout.HelpBox(
                    "Set an output directory under Assets/ (for example Assets/Scripts/Schema) so Unity imports the generated files.",
                    MessageType.Info);
            }

            GUILayout.Space(2);
        }

        /// <summary>Opens a file or folder panel after the GUI pass and stores the pick.</summary>
        private void PickPath(bool folder, bool output)
        {
            var start = ProjectPaths.StartDir(output ? _settings.ResolvedOutput : _settings.ResolvedSource);
            ColyseusEditorStyles.Defer(() =>
            {
                var picked = folder
                    ? EditorUtility.OpenFolderPanel(output ? "Select output directory" : "Select schema folder", start, "")
                    : EditorUtility.OpenFilePanel("Select schema .ts file", start, "ts");
                if (string.IsNullOrEmpty(picked)) return;

                if (output) _settings.SetOutput(picked);
                else _settings.SetSource(picked);
                _settings.Save();
                InvalidateMatches();
                Repaint();
            });
        }

        private void DrawLogToolbarExtras()
        {
            using (new EditorGUI.DisabledScope(!_uiOutputExists))
            {
                if (GUILayout.Button(OpenOutputLabel, EditorStyles.toolbarButton, GUILayout.Width(80)))
                    EditorUtility.RevealInFinder(_uiOutput);
            }
        }

        public override void AddItemsToMenu(GenericMenu menu)
        {
            var settingsFile = CodegenSettings.FilePath;
            if (File.Exists(settingsFile))
                menu.AddItem(new GUIContent("Reveal Settings File"), false, () => EditorUtility.RevealInFinder(settingsFile));
            else
                menu.AddDisabledItem(new GUIContent("Reveal Settings File"));
            menu.AddSeparator("");

            base.AddItemsToMenu(menu);
        }

        private void Cancel()
        {
            if (!_running) return;
            _cancelled = true;
            KillProcess();
            _running = false;
            _hasRun = false;
            _log.Append("Cancelled.", LogLevel.Warning);
            Repaint();
        }

        // ------------------------------------------------------------------
        // Detection
        // ------------------------------------------------------------------

        /// <summary>
        /// Looks through the server project for .ts files that define schemas, and
        /// points the source at the narrowest path covering them.
        /// </summary>
        private void DetectSchemaSource()
        {
            var root = ServerSettings.Load().ResolvedWorkingDir;
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                root = ProjectPaths.NearestPackageRoot(_settings.ResolvedSource);
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                root = ServerProject.DetectWorkingDir();

            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
            {
                EditorUtility.DisplayDialog("Schema Codegen",
                    "No server project found to search.\n\nSet the working directory in the Game Server window, or pick the schema files manually.",
                    "OK");
                return;
            }

            var files = FindSchemaFiles(root);
            if (files.Count == 0)
            {
                EditorUtility.DisplayDialog("Schema Codegen",
                    $"No schema definitions found under:\n{root}\n\nLooked for .ts files importing @colyseus/schema.",
                    "OK");
                return;
            }

            var directories = files.Select(Path.GetDirectoryName).Distinct().ToList();
            if (directories.Count == 1)
            {
                _settings.SetSource(directories[0]);
            }
            else
            {
                // Several folders: a recursive glob keeps them all in scope.
                var common = CommonDirectory(directories);
                _settings.SetSource(Path.Combine(string.IsNullOrEmpty(common) ? root : common, "**", "*.ts"));
            }

            _settings.Save();
            InvalidateMatches();
            _log.Append($"Found {files.Count} schema file{(files.Count == 1 ? "" : "s")} under {ProjectPaths.ForDisplay(root)}.",
                LogLevel.Info);
            Repaint();
        }

        private static List<string> FindSchemaFiles(string root)
        {
            var found = new List<string>();
            try
            {
                foreach (var file in Directory.GetFiles(root, "*.ts", SearchOption.AllDirectories))
                {
                    if (IsIgnored(file) || file.EndsWith(".d.ts", StringComparison.OrdinalIgnoreCase)) continue;

                    string text;
                    try { text = File.ReadAllText(file); }
                    catch { continue; }

                    if (text.IndexOf("@colyseus/schema", StringComparison.OrdinalIgnoreCase) >= 0)
                        found.Add(file);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Colyseus] Schema search failed: " + ex.Message);
            }
            return found;
        }

        private static bool IsIgnored(string path)
        {
            var normalized = path.Replace('\\', '/');
            return normalized.IndexOf("/node_modules/", StringComparison.OrdinalIgnoreCase) >= 0
                   || normalized.IndexOf("/dist/", StringComparison.OrdinalIgnoreCase) >= 0
                   || normalized.IndexOf("/build/", StringComparison.OrdinalIgnoreCase) >= 0
                   || normalized.IndexOf("/.git/", StringComparison.Ordinal) >= 0;
        }

        private static string CommonDirectory(List<string> directories)
        {
            if (directories.Count == 0) return null;

            var split = directories
                .Select(d => d.Replace('\\', '/').TrimEnd('/').Split('/'))
                .ToList();

            var shortest = split.Min(parts => parts.Length);
            var common = new List<string>();
            for (var i = 0; i < shortest; i++)
            {
                var segment = split[0][i];
                if (split.Any(parts => !string.Equals(parts[i], segment, StringComparison.Ordinal))) break;
                common.Add(segment);
            }

            return common.Count == 0 ? null : string.Join(Path.DirectorySeparatorChar.ToString(), common.ToArray());
        }

        // ------------------------------------------------------------------
        // Generation
        // ------------------------------------------------------------------

        private void Generate()
        {
            _settings.Save();
            InvalidateMatches();

            var inputs = SafeResolveInputs(_settings.ResolvedSource);
            if (inputs.Count == 0)
            {
                EditorUtility.DisplayDialog("Schema Codegen", "No .ts files matched the schema source.", "OK");
                return;
            }
            var output = _settings.ResolvedOutput;
            if (string.IsNullOrEmpty(output))
            {
                EditorUtility.DisplayDialog("Schema Codegen", "Please choose an output directory.", "OK");
                return;
            }

            var workingDir = ProjectPaths.NearestPackageRoot(inputs[0]) ?? Path.GetDirectoryName(inputs[0]);
            var psi = BuildStartInfo(BuildArgs(inputs, workingDir, output), workingDir);

            _log.Clear();
            _log.Append($"$ {psi.FileName} {psi.Arguments}", LogLevel.Command);
            _log.Append($"  in {workingDir}", LogLevel.Info);
            _logView.AutoScroll = true;

            _running = true;
            _completed = false;
            _cancelled = false;

            try
            {
                _process = new Process { StartInfo = psi, EnableRaisingEvents = true };
                _process.OutputDataReceived += OnOutput;
                _process.ErrorDataReceived += OnOutput;
                _process.Exited += OnExited;
                _process.Start();
                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                _log.Append("Failed to launch: " + ex.Message, LogLevel.Error);
                _running = false;
                _hasRun = true;
                _exitCode = -1;
                KillProcess();
            }

            Repaint();
        }

        private void OnOutput(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null) _incoming.Enqueue(e.Data);
        }

        private void OnExited(object sender, EventArgs e)
        {
            try { _exitCode = ((Process)sender).ExitCode; } catch { _exitCode = -1; }
            _completed = true;
        }

        /// <summary>
        /// Prefers a locally installed schema-codegen, so the server project's
        /// pinned version is used; otherwise fetches it with <c>npx -p</c>.
        /// </summary>
        private List<string> BuildArgs(List<string> inputs, string workingDir, string output)
        {
            var args = new List<string>();

            var localBin = Path.Combine(workingDir, "node_modules", ".bin",
                NodeLocator.IsWindows ? "schema-codegen.cmd" : "schema-codegen");

            if (File.Exists(localBin) && string.IsNullOrEmpty(_settings.version))
            {
                args.Add("schema-codegen"); // npx resolves the local bin
            }
            else
            {
                args.Add("-p");
                args.Add("@colyseus/schema" + (string.IsNullOrEmpty(_settings.version) ? "" : "@" + _settings.version.Trim()));
                args.Add("schema-codegen");
            }

            args.AddRange(inputs);
            args.Add("--csharp");
            args.Add("--output");
            args.Add(output);

            if (!string.IsNullOrEmpty(_settings.@namespace))
            {
                args.Add("--namespace");
                args.Add(_settings.@namespace.Trim());
            }
            if (_settings.bundle) args.Add("--bundle");

            return args;
        }

        private static ProcessStartInfo BuildStartInfo(List<string> args, string workingDir)
        {
            var psi = new ProcessStartInfo
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDir,
            };
            NodeLocator.ApplyEnvironment(psi);

            var npx = NodeLocator.ToolPath("npx", NodeLocator.NodeDir);
            var quoted = string.Join(" ", args.Select(NodeLocator.Quote).ToArray());

            if (NodeLocator.IsWindows)
            {
                // npx is a .cmd shim, which only cmd.exe can run.
                NodeLocator.UseShell(psi, NodeLocator.Quote(npx) + " " + quoted);
            }
            else
            {
                // Started directly, not through sh, so Cancel's Kill() reaches npx itself.
                psi.FileName = npx;
                psi.Arguments = quoted;
            }
            return psi;
        }

        // ------------------------------------------------------------------
        // Input resolution
        // ------------------------------------------------------------------

        private static List<string> SafeResolveInputs(string src)
        {
            try { return ResolveInputs(src); }
            catch { return new List<string>(); }
        }

        /// <summary>
        /// Expands the schema source into concrete files: a single file, every .ts
        /// in a folder, or a glob — including a recursive <c>**</c> one.
        /// </summary>
        private static List<string> ResolveInputs(string src)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(src)) return result;

            if (Directory.Exists(src))
            {
                result.AddRange(Directory.GetFiles(src, "*.ts").Where(NotDeclaration));
            }
            else if (File.Exists(src))
            {
                result.Add(src);
            }
            else if (src.Contains("**"))
            {
                var normalized = src.Replace('\\', '/');
                var marker = normalized.IndexOf("**", StringComparison.Ordinal);
                var baseDir = normalized.Substring(0, marker).TrimEnd('/');
                var pattern = Path.GetFileName(normalized);
                if (string.IsNullOrEmpty(pattern) || pattern == "**") pattern = "*.ts";

                if (!string.IsNullOrEmpty(baseDir) && Directory.Exists(baseDir))
                {
                    result.AddRange(Directory
                        .GetFiles(baseDir, pattern, SearchOption.AllDirectories)
                        .Where(f => NotDeclaration(f) && !IsIgnored(f)));
                }
            }
            else if (src.Contains("*"))
            {
                var dir = Path.GetDirectoryName(src);
                var pattern = Path.GetFileName(src);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    result.AddRange(Directory.GetFiles(dir, pattern).Where(NotDeclaration));
            }

            result.Sort(StringComparer.Ordinal);
            return result;
        }

        private static bool NotDeclaration(string file) =>
            !file.EndsWith(".d.ts", StringComparison.OrdinalIgnoreCase);

        private void KillProcess()
        {
            if (_process == null) return;
            try { if (!_process.HasExited) _process.Kill(); } catch { }
            // The parameterless overload waits for the async output handlers to
            // drain; without it the tool's last lines are lost on Dispose.
            try { _process.WaitForExit(); } catch { }
            try { _process.Dispose(); } catch { }
            _process = null;
        }
    }
}
