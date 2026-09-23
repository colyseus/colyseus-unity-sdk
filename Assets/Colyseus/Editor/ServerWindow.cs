using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Colyseus.Editor
{
    /// <summary>
    /// Starts and stops the project's local game server without leaving Unity, and
    /// streams its output into the editor.
    ///
    /// The process itself is owned by <see cref="LocalServer"/>, which outlives both
    /// this window and the domain, so closing the window never stops the server.
    /// </summary>
    public class ServerWindow : ColyseusWindow
    {
        private static readonly GUIContent WorkingDirHint = new GUIContent("Working directory",
            "The folder holding your server's package.json");
        private static readonly GUIContent DetectLabel = new GUIContent("Detect", "Look for a server project next to this one");
        private static readonly GUIContent CommandLabel = new GUIContent("Command",
            "Run in the working directory, with PORT set from below");
        private static readonly GUIContent ScriptsLabel = new GUIContent("Scripts", "Pick a script from package.json");
        private static readonly GUIContent PortLabel = new GUIContent("Port",
            "Passed to the server as the PORT environment variable");
        private static readonly GUIContent AutoStartLabel = new GUIContent("Auto-start on Play",
            "Start the server when you enter Play mode");
        private static readonly GUIContent OpenFolderLabel = new GUIContent("Open Folder", "Reveal the server folder");

        private readonly LogView _logView = new LogView();
        private ServerSettings _settings;
        private bool _showNodeSettings;
        private double _nextRepaintAt;
        private double _nextProbeAt;

        // Captured on the Layout event (see ColyseusWindow).
        private ServerState _uiState;
        private int _uiPid;
        private int _uiPort;
        private int? _uiExitCode;
        private TimeSpan _uiUptime;
        private string _uiLabel = "Server";
        private bool _uiPortInUse;
        private bool UiBusy => _uiState != ServerState.Stopped && _uiState != ServerState.Exited;

        // Filesystem facts, refreshed on a timer instead of on every OnGUI pass.
        private string _probedDir = "";
        private bool _dirExists;
        private bool _hasPackageJson;
        private bool _hasNodeModules;

        [MenuItem("Window/Colyseus/Game Server")]
        public static void ShowWindow()
        {
            var window = GetWindow<ServerWindow>();
            window.minSize = new Vector2(460, 380);
            window.Show();
        }

        // ------------------------------------------------------------------
        // Lifetime
        // ------------------------------------------------------------------

        private void OnEnable()
        {
            titleContent = ColyseusEditorStyles.TitleContent("Game Server");

            _settings = ServerSettings.Load();
            if (!_settings.IsConfigured) AutoDetectProject(save: false);
            RefreshProbes();

            NodeLocator.EnsureChecked();

            _logView.ClearRequested += LocalServer.ClearLog;
            LocalServer.Changed += Repaint;
            NodeLocator.StatusChanged += Repaint;
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            _logView.ClearRequested -= LocalServer.ClearLog;
            LocalServer.Changed -= Repaint;
            NodeLocator.StatusChanged -= Repaint;
            EditorApplication.update -= OnEditorUpdate;
            _settings.Save();
        }

        private void OnEditorUpdate()
        {
            var now = EditorApplication.timeSinceStartup;

            if (now >= _nextProbeAt)
            {
                _nextProbeAt = now + 1.0;
                if (RefreshProbes()) Repaint();
            }

            // Keeps the uptime clock moving without repainting every frame.
            if (LocalServer.IsBusy && now >= _nextRepaintAt)
            {
                _nextRepaintAt = now + 0.5;
                Repaint();
            }
        }

        /// <summary>Re-reads what exists on disk. True when anything changed.</summary>
        private bool RefreshProbes()
        {
            var dir = _settings.ResolvedWorkingDir;
            var dirExists = !string.IsNullOrEmpty(dir) && Directory.Exists(dir);
            var hasPackageJson = dirExists && ProjectPaths.HasPackageJson(dir);
            var hasNodeModules = dirExists && ServerProject.HasNodeModules(dir);

            var changed = dir != _probedDir || dirExists != _dirExists ||
                          hasPackageJson != _hasPackageJson || hasNodeModules != _hasNodeModules;

            _probedDir = dir;
            _dirExists = dirExists;
            _hasPackageJson = hasPackageJson;
            _hasNodeModules = hasNodeModules;
            return changed;
        }

        /// <summary>Starts the server on Play when the project asked for it, window open or not.</summary>
        [InitializeOnLoadMethod]
        private static void HookAutoStart()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.ExitingEditMode || LocalServer.IsBusy) return;

                var settings = ServerSettings.Load();
                if (!settings.autoStartOnPlay || !settings.IsConfigured) return;

                if (!LocalServer.Start(settings.ResolvedWorkingDir, settings.command, settings.port, out var error))
                    Debug.LogWarning("[Colyseus] Auto-start failed: " + error);
            };
        }

        // ------------------------------------------------------------------
        // GUI
        // ------------------------------------------------------------------

        protected override void CaptureState()
        {
            _uiState = LocalServer.State;
            _uiPid = LocalServer.Pid;
            _uiPort = LocalServer.Port;
            _uiExitCode = LocalServer.ExitCode;
            _uiUptime = LocalServer.Uptime;
            _uiLabel = LocalServer.Label;
            _uiPortInUse = _uiState == ServerState.Exited && LocalServer.SawPortInUse;
        }

        protected override void DrawContents()
        {
            DrawHeader();
            DrawStatus();
            DrawConfiguration();
            DrawWarnings();

            GUILayout.Space(2);
            _logView.DrawToolbar(LocalServer.Log, DrawLogToolbarExtras);
            _logView.Draw(LocalServer.Log,
                "Output from the server appears here once you press Start.",
                GUILayout.ExpandHeight(true));
        }

        private void DrawHeader()
        {
            ColyseusEditorStyles.DrawHeader("Local Game Server", () =>
            {
                using (new EditorGUI.DisabledScope(UiBusy || !NodeLocator.Ok || !_settings.IsConfigured))
                {
                    if (ColyseusEditorStyles.TintedButton(
                            ColyseusEditorStyles.IconLabel("PlayButton", "Start Server", "Run the command below"),
                            ColyseusEditorStyles.StartTint, 104))
                    {
                        ColyseusEditorStyles.Defer(StartServer);
                    }
                }

                using (new EditorGUI.DisabledScope(!UiBusy || _uiState == ServerState.Stopping))
                {
                    if (ColyseusEditorStyles.TintedButton(
                            ColyseusEditorStyles.IconLabel("PreMatQuad", "Stop Server",
                                "Terminate the server and everything it spawned"),
                            ColyseusEditorStyles.StopTint, 100))
                    {
                        ColyseusEditorStyles.Defer(LocalServer.Stop);
                    }
                }

                DrawMenuButton();
            });
        }

        private void DrawStatus()
        {
            Color dot;
            string status;
            string detail = null;

            switch (_uiState)
            {
                case ServerState.Running:
                    dot = ColyseusEditorStyles.Green;
                    status = _uiLabel + " is running";
                    detail = $"pid {_uiPid}   ·   up {Format(_uiUptime)}" + (_uiPort > 0 ? $"   ·   port {_uiPort}" : "");
                    break;

                case ServerState.Attaching:
                    dot = ColyseusEditorStyles.Amber;
                    status = "Re-attaching…";
                    detail = $"pid {_uiPid}";
                    break;

                case ServerState.Stopping:
                    dot = ColyseusEditorStyles.Amber;
                    status = "Stopping…";
                    detail = $"pid {_uiPid}";
                    break;

                case ServerState.Exited:
                    // An unknown exit code is not a clean one: after a reload we
                    // cannot tell a normal shutdown from a crash.
                    dot = !_uiExitCode.HasValue ? ColyseusEditorStyles.Amber
                        : _uiExitCode.Value == 0 ? ColyseusEditorStyles.Muted
                        : ColyseusEditorStyles.Red;
                    status = _uiExitCode.HasValue ? $"Exited (code {_uiExitCode.Value})" : "Exited (code unknown)";
                    break;

                default:
                    dot = ColyseusEditorStyles.Idle;
                    status = "Server is stopped";
                    break;
            }

            ColyseusEditorStyles.DrawStatusRow(dot, status, detail);
        }

        private void DrawConfiguration()
        {
            GUILayout.Space(4);

            using (new EditorGUI.DisabledScope(UiBusy))
            {
                EditorGUI.BeginChangeCheck();

                using (new EditorGUILayout.HorizontalScope())
                {
                    var label = _dirExists ? new GUIContent(WorkingDirHint.text, _probedDir) : WorkingDirHint;
                    var edited = EditorGUILayout.DelayedTextField(label, _settings.workingDir);
                    // Typed paths may be absolute; normalize to the stored form.
                    if (edited != _settings.workingDir) _settings.SetWorkingDir(ProjectPaths.FromStored(edited));

                    if (GUILayout.Button("Browse", EditorStyles.miniButtonLeft, GUILayout.Width(60)))
                    {
                        var start = _dirExists ? _probedDir : ProjectPaths.ProjectRoot;
                        ColyseusEditorStyles.Defer(() =>
                        {
                            var picked = EditorUtility.OpenFolderPanel("Select your server folder", start, "");
                            if (!string.IsNullOrEmpty(picked)) UseProject(picked, save: true);
                        });
                    }
                    if (GUILayout.Button(DetectLabel, EditorStyles.miniButtonRight, GUILayout.Width(60)))
                    {
                        ColyseusEditorStyles.Defer(() => AutoDetectProject(save: true));
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    _settings.command = EditorGUILayout.DelayedTextField(CommandLabel, _settings.command);

                    using (new EditorGUI.DisabledScope(!_hasPackageJson))
                    {
                        if (GUILayout.Button(ScriptsLabel, EditorStyles.miniButton, GUILayout.Width(56)))
                            ShowScriptMenu();
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    _settings.port = Mathf.Clamp(EditorGUILayout.IntField(PortLabel, _settings.port), 0, 65535);
                    GUILayout.Space(12);
                    _settings.autoStartOnPlay = GUILayout.Toggle(_settings.autoStartOnPlay, AutoStartLabel);
                    GUILayout.FlexibleSpace();
                }

                if (EditorGUI.EndChangeCheck())
                {
                    _settings.Save();
                    RefreshProbes();
                }
            }

            NodeLocator.DrawStatusRow(ref _showNodeSettings);
            GUILayout.Space(2);
        }

        private void DrawWarnings()
        {
            if (string.IsNullOrEmpty(_settings.workingDir))
            {
                EditorGUILayout.HelpBox(
                    "Point \"Working directory\" at the folder containing your server's package.json, then press Detect to fill in the command.",
                    MessageType.Info);
            }
            else if (!_dirExists)
            {
                EditorGUILayout.HelpBox($"The working directory does not exist:\n{_probedDir}", MessageType.Error);
            }
            else if (!_hasPackageJson)
            {
                EditorGUILayout.HelpBox("No package.json in the working directory. This is probably not a Node.js server project.",
                    MessageType.Warning);
            }
            else if (!_hasNodeModules && !UiBusy)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.HelpBox("Dependencies are not installed in the server project.", MessageType.Warning);
                    if (GUILayout.Button("Install", GUILayout.Width(70), GUILayout.Height(38)))
                        ColyseusEditorStyles.Defer(InstallDependencies);
                }
            }
            else if (string.IsNullOrEmpty(_settings.command))
            {
                EditorGUILayout.HelpBox("No command set. Pick one from package.json with the Scripts button.",
                    MessageType.Warning);
            }
            else if (_uiPortInUse)
            {
                EditorGUILayout.HelpBox(
                    $"Port {_uiPort} is already in use. Another server is probably still running — stop it, or change the port above.",
                    MessageType.Error);
            }
        }

        private void DrawLogToolbarExtras()
        {
            using (new EditorGUI.DisabledScope(!_dirExists))
            {
                if (GUILayout.Button(OpenFolderLabel, EditorStyles.toolbarButton, GUILayout.Width(78)))
                    EditorUtility.RevealInFinder(_probedDir);
            }
        }

        // ------------------------------------------------------------------
        // Menus
        // ------------------------------------------------------------------

        public override void AddItemsToMenu(GenericMenu menu)
        {
            if (_settings == null) return;

            if (_dirExists && !LocalServer.IsBusy)
                menu.AddItem(new GUIContent("Install Dependencies"), false, () => ColyseusEditorStyles.Defer(InstallDependencies));
            else
                menu.AddDisabledItem(new GUIContent("Install Dependencies"));

            menu.AddItem(new GUIContent("Generate Schema Files"), false, () => ColyseusEditorStyles.Defer(() =>
            {
                if (SchemaCodegenWindow.GenerateFromSavedSettings()) return;
                SchemaCodegenWindow.ShowWindow();
                Debug.Log("[Colyseus] Configure the schema source and output, then press Generate.");
            }));
            menu.AddSeparator("");

            if (_dirExists)
                menu.AddItem(new GUIContent("Open Working Directory"), false, () => EditorUtility.RevealInFinder(_probedDir));
            else
                menu.AddDisabledItem(new GUIContent("Open Working Directory"));

            if (File.Exists(LocalServer.LogPath))
                menu.AddItem(new GUIContent("Open Log File"), false, () => EditorUtility.OpenWithDefaultApp(LocalServer.LogPath));
            else
                menu.AddDisabledItem(new GUIContent("Open Log File"));
            menu.AddSeparator("");

            menu.AddItem(new GUIContent("Stop Server When Unity Quits"), LocalServer.StopOnQuit,
                () => LocalServer.StopOnQuit = !LocalServer.StopOnQuit);
            menu.AddItem(new GUIContent("Auto-start When Entering Play Mode"), _settings.autoStartOnPlay, () =>
            {
                _settings.autoStartOnPlay = !_settings.autoStartOnPlay;
                _settings.Save();
            });
            menu.AddSeparator("");

            base.AddItemsToMenu(menu);
        }

        private void ShowScriptMenu()
        {
            var package = PackageInfo.Read(_probedDir);
            var menu = new GenericMenu();

            if (package == null || package.Scripts.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No scripts in package.json"));
            }
            else
            {
                foreach (var script in package.Scripts)
                {
                    var command = package.RunScript(script);
                    menu.AddItem(new GUIContent(command), _settings.command == command, () =>
                    {
                        _settings.command = command;
                        _settings.Save();
                        Repaint();
                    });
                }
            }
            menu.ShowAsContext();
        }

        // ------------------------------------------------------------------
        // Actions
        // ------------------------------------------------------------------

        private void StartServer()
        {
            _settings.Save();
            _logView.AutoScroll = true;

            if (!LocalServer.Start(_settings.ResolvedWorkingDir, _settings.command, _settings.port, out var error))
                EditorUtility.DisplayDialog("Colyseus", "Could not start the server:\n\n" + error, "OK");
        }

        private void InstallDependencies()
        {
            var package = PackageInfo.Read(_probedDir);
            if (package == null) return;
            _logView.AutoScroll = true;

            if (!LocalServer.Start(_probedDir, package.InstallCommand, 0, out var error, label: "Install"))
                EditorUtility.DisplayDialog("Colyseus", "Could not run the install:\n\n" + error, "OK");
        }

        private void AutoDetectProject(bool save)
        {
            var dir = ServerProject.DetectWorkingDir();
            if (!string.IsNullOrEmpty(dir))
            {
                UseProject(dir, save);
            }
            else if (save)
            {
                EditorUtility.DisplayDialog("Colyseus",
                    "No server project found.\n\nLooked for a package.json in ./server, ../server and next to your schema source. Use Browse to pick it manually.",
                    "OK");
            }
        }

        /// <summary>Points the window at <paramref name="dir"/>, taking its start command when it has one.</summary>
        private void UseProject(string dir, bool save)
        {
            _settings.SetWorkingDir(dir);
            var command = PackageInfo.Read(dir)?.DefaultCommand;
            if (!string.IsNullOrEmpty(command)) _settings.command = command;

            if (save) _settings.Save();
            RefreshProbes();
            Repaint();
        }

        private static string Format(TimeSpan span)
        {
            return span.TotalHours >= 1
                ? $"{(int)span.TotalHours}:{span.Minutes:00}:{span.Seconds:00}"
                : $"{span.Minutes:00}:{span.Seconds:00}";
        }
    }
}
