using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Colyseus.Editor
{
    internal enum ServerState
    {
        Stopped,
        /// <summary>Re-attaching to a process a previous domain (or editor session) started.</summary>
        Attaching,
        Running,
        Stopping,
        Exited,
    }

    /// <summary>
    /// Runs the developer's local game server from inside the editor and keeps it
    /// alive across domain reloads.
    ///
    /// The child is never wired to Unity's pipes: a domain reload would leave
    /// nobody draining them and the server would wedge as soon as it filled the
    /// 64KB buffer. Instead it is launched through a shell that redirects output
    /// to a file, and the editor tails that file. What survives a reload is
    /// therefore just a pid and a log path, kept in EditorPrefs.
    ///
    /// State is owned by the main thread. Background work (probing, killing)
    /// posts its results back through <see cref="MainThread"/>.
    /// </summary>
    [InitializeOnLoad]
    internal static class LocalServer
    {
        private const int MaxReadPerTick = 256 * 1024;
        private const int MaxLineChars = 64 * 1024;
        private const double TailIntervalSeconds = 0.15;
        private const double ProbeIntervalSeconds = 2.0;

        // ------------------------------------------------------------------
        // State
        // ------------------------------------------------------------------

        private static ServerState _state = ServerState.Stopped;
        private static int _pid;
        private static string _startToken = "";
        private static DateTime _startedAtUtc;
        private static int? _exitCode;
        private static string _workingDir = "";
        private static string _command = "";
        private static string _logPath = "";
        private static string _label = "Server";
        private static int _port;

        /// <summary>Non-null only while this domain owns the process; lost on reload.</summary>
        private static Process _process;

        private static readonly LogBuffer _log = new LogBuffer();

        // Tailer. The buffers are reused: a chatty server is read ~7 times a second.
        private static long _tailOffset;
        private static readonly Decoder _decoder = Encoding.UTF8.GetDecoder();
        private static readonly StringBuilder _partial = new StringBuilder();
        private static readonly byte[] _readBuffer = new byte[MaxReadPerTick];
        private static readonly char[] _charBuffer = new char[Encoding.UTF8.GetMaxCharCount(MaxReadPerTick)];
        private static bool _skipPartialLine;
        private static bool _backfilling;
        private static bool _missingLogWarned;

        private static double _nextTailAt;
        private static double _nextProbeAt;
        private static bool _probeInFlight;

        public static ServerState State => _state;
        public static int Pid => _pid;
        public static int? ExitCode => _exitCode;
        public static int Port => _port;
        public static string LogPath => _logPath;
        public static string Label => _label;
        public static LogBuffer Log => _log;

        public static bool IsBusy =>
            _state == ServerState.Attaching || _state == ServerState.Running || _state == ServerState.Stopping;

        public static TimeSpan Uptime =>
            IsBusy && _startedAtUtc.Ticks > 0 ? DateTime.UtcNow - _startedAtUtc : TimeSpan.Zero;

        /// <summary>Raised on the main thread whenever state or output changed.</summary>
        public static event Action Changed;

        public static bool StopOnQuit
        {
            get => EditorPrefs.GetBool(ProjectPaths.PrefKey("LocalServer", "StopOnQuit"), true);
            set => EditorPrefs.SetBool(ProjectPaths.PrefKey("LocalServer", "StopOnQuit"), value);
        }

        private static string RecordKey => ProjectPaths.PrefKey("LocalServer", "Process");

        // ------------------------------------------------------------------
        // Lifetime
        // ------------------------------------------------------------------

        static LocalServer()
        {
            EditorApplication.update += Tick;
            EditorApplication.quitting += OnQuitting;

            // Must not block: this runs on every single domain reload.
            TryReattach();
        }

        private static void Tick()
        {
            if (_state != ServerState.Running && _state != ServerState.Stopping) return;

            var now = EditorApplication.timeSinceStartup;
            if (now >= _nextTailAt)
            {
                _nextTailAt = now + TailIntervalSeconds;
                TailStep();
            }

            if (_state == ServerState.Running && now >= _nextProbeAt)
            {
                _nextProbeAt = now + ProbeIntervalSeconds;
                PollLiveness();
            }
        }

        private static void RaiseChanged()
        {
            try { Changed?.Invoke(); }
            catch (Exception ex) { Debug.LogException(ex); }
        }

        // ------------------------------------------------------------------
        // Start
        // ------------------------------------------------------------------

        /// <summary>
        /// Launches <paramref name="command"/> in <paramref name="workingDir"/>.
        /// <paramref name="label"/> names the job in status lines ("Install", say);
        /// a <paramref name="port"/> of 0 leaves PORT unset.
        /// </summary>
        public static bool Start(string workingDir, string command, int port, out string error, string label = "Server")
        {
            error = null;

            if (IsBusy)
            {
                error = "Something is already running. Stop it first.";
                return false;
            }
            if (string.IsNullOrEmpty(workingDir) || !Directory.Exists(workingDir))
            {
                error = "Working directory does not exist.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(command))
            {
                error = "No command to run.";
                return false;
            }

            _workingDir = Path.GetFullPath(workingDir);
            _command = command.Trim();
            _port = port;
            _label = label;
            _exitCode = null;
            _logPath = Path.Combine(ProjectPaths.ProjectRoot, "Library", "Colyseus", "server.log");
            _missingLogWarned = false;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_logPath));
                // Truncate: each run starts with a clean file so offsets stay honest.
                using (new FileStream(_logPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)) { }
            }
            catch (Exception ex)
            {
                error = "Could not prepare the log file: " + ex.Message;
                return false;
            }

            ResetTail();
            _log.Clear();
            _log.Append("$ " + _command, LogLevel.Command);
            _log.Append($"  in {_workingDir}" + (port > 0 ? $"   PORT={port}" : ""), LogLevel.Info);

            try
            {
                _process = new Process { StartInfo = BuildStartInfo(), EnableRaisingEvents = true };
                _process.Exited += OnProcessExited;
                _process.Start();
            }
            catch (Exception ex)
            {
                error = ex.Message;
                _log.Append("Failed to launch: " + ex.Message, LogLevel.Error);
                _state = ServerState.Exited;
                _exitCode = -1;
                DisposeProcess();
                RaiseChanged();
                return false;
            }

            _pid = _process.Id;
            _startedAtUtc = DateTime.UtcNow;
            _startToken = "";
            _state = ServerState.Running;
            SaveRecord();

            // The identity token needs a `ps` round-trip; keep it off the main thread.
            var pid = _pid;
            var process = _process;
            MainThread.RunInBackground("LocalServer.Token", () =>
            {
                var token = ProcessTree.CaptureStartToken(pid, process);
                MainThread.Post(() =>
                {
                    if (_pid != pid || !IsBusy) return;
                    _startToken = token;
                    SaveRecord();
                });
            });

            _nextTailAt = 0;
            _nextProbeAt = EditorApplication.timeSinceStartup + ProbeIntervalSeconds;
            RaiseChanged();
            return true;
        }

        /// <summary>
        /// Wraps the command in a shell that redirects both streams to the log
        /// file and detaches stdin, so nothing ever blocks on the editor.
        /// </summary>
        private static ProcessStartInfo BuildStartInfo()
        {
            var psi = new ProcessStartInfo
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = _workingDir,
            };

            var command = ResolveCommand(_command);
            NodeLocator.UseShell(psi, NodeLocator.IsWindows
                ? $"{command} <NUL >\"{_logPath}\" 2>&1"
                : $"{command} </dev/null >{NodeLocator.ShellQuote(_logPath)} 2>&1");

            NodeLocator.ApplyEnvironment(psi);
            if (_port > 0) psi.EnvironmentVariables["PORT"] = _port.ToString(CultureInfo.InvariantCulture);
            return psi;
        }

        /// <summary>
        /// Rewrites a leading `npm`/`pnpm`/`yarn`/`bun` to its absolute path, so the
        /// command works even when the editor's PATH has no Node in it.
        /// </summary>
        private static string ResolveCommand(string command)
        {
            if (command.StartsWith("\"", StringComparison.Ordinal)) return command; // already an explicit path

            var space = command.IndexOf(' ');
            var head = space < 0 ? command : command.Substring(0, space);
            if (!NodeLocator.IsKnownTool(head)) return command;

            var resolved = NodeLocator.ToolPath(head, NodeLocator.NodeDir);
            if (!Path.IsPathRooted(resolved)) return command; // leave it to PATH

            return "\"" + resolved + "\"" + (space < 0 ? "" : command.Substring(space));
        }

        // ------------------------------------------------------------------
        // Stop
        // ------------------------------------------------------------------

        public static void Stop()
        {
            if (_state != ServerState.Running && _state != ServerState.Attaching) return;

            var pid = _pid;
            _state = ServerState.Stopping;
            _log.Append($"Stopping {_label.ToLowerInvariant()} (pid {pid})…", LogLevel.Info);
            RaiseChanged();

            MainThread.RunInBackground("LocalServer.Stop", () =>
            {
                var result = ProcessTree.Kill(pid);
                MainThread.Post(() => OnStopped(result));
            });
        }

        private static void OnStopped(TreeKillResult result)
        {
            TailStep();
            FlushPartial();

            if (result.AllGone)
            {
                _log.Append("Stopped.", LogLevel.Info);
                _state = ServerState.Stopped;
                _pid = 0;
                DisposeProcess();
                ClearRecord();
            }
            else
            {
                // Still alive: stay attached (the record stays too) so the user,
                // or the next session, can see it and try again.
                _log.Append("Could not stop every process: " + (result.Error ?? "unknown error"), LogLevel.Error);
                _log.Append($"pid {_pid} is still running and still holds the port.", LogLevel.Warning);
                _state = ServerState.Running;
            }

            _exitCode = null;
            RaiseChanged();
        }

        private static void OnQuitting()
        {
            if (!StopOnQuit || !IsBusy || _pid <= 0) return;

            // Synchronous and short: the editor is already tearing down. If it
            // survives, leave the record so the next session can re-attach to it.
            if (ProcessTree.Kill(_pid, 1500, 500).AllGone) ClearRecord();
        }

        // ------------------------------------------------------------------
        // Exit detection
        // ------------------------------------------------------------------

        private static void OnProcessExited(object sender, EventArgs e)
        {
            int? code = null;
            try { code = (sender as Process)?.ExitCode; } catch { /* unavailable */ }
            MainThread.Post(() => OnExited(code));
        }

        private static void OnExited(int? exitCode)
        {
            // Stop() reports its own outcome.
            if (_state != ServerState.Running && _state != ServerState.Attaching) return;

            TailStep();
            FlushPartial();

            _exitCode = exitCode;
            _state = ServerState.Exited;

            if (!exitCode.HasValue)
                _log.Append($"{_label} exited. Exit code is unavailable after an editor reload.", LogLevel.Warning);
            else if (exitCode.Value == 0)
                _log.Append($"✔ {_label} exited (code 0)", LogLevel.Success);
            else
                _log.Append($"✘ {_label} exited (code {exitCode.Value})", LogLevel.Error);

            _pid = 0;
            DisposeProcess();
            ClearRecord();
            RaiseChanged();
        }

        /// <summary>Checks whether the process is still there, without blocking the editor.</summary>
        private static void PollLiveness()
        {
            if (_pid <= 0) return;

            if (_process != null)
            {
                // We own it: HasExited is authoritative and cheap.
                try
                {
                    if (_process.HasExited)
                    {
                        int? code = null;
                        try { code = _process.ExitCode; } catch { }
                        OnExited(code);
                    }
                }
                catch { /* the Exited event will cover it */ }
                return;
            }

            if (_probeInFlight) return;
            _probeInFlight = true;

            var pid = _pid;
            MainThread.RunInBackground("LocalServer.Probe", () =>
            {
                // Identity was settled when we adopted this pid; now only liveness matters.
                var alive = ProcessTree.IsRunning(pid);
                MainThread.Post(() =>
                {
                    _probeInFlight = false;
                    if (_pid == pid && !alive) OnExited(null);
                });
            });
        }

        private static void DisposeProcess()
        {
            if (_process == null) return;
            try { _process.Exited -= OnProcessExited; } catch { }
            try { _process.Dispose(); } catch { }
            _process = null;
        }

        // ------------------------------------------------------------------
        // Re-attaching
        // ------------------------------------------------------------------

        private static void TryReattach()
        {
            var record = LoadRecord();
            if (record == null || record.pid <= 0) return;

            _pid = record.pid;
            _startToken = record.startToken ?? "";
            _logPath = record.logPath;
            _workingDir = record.workingDir;
            _command = record.command;
            _port = record.port;
            _label = string.IsNullOrEmpty(record.label) ? "Server" : record.label;
            _startedAtUtc = new DateTime(record.startedAtTicks, DateTimeKind.Utc);
            _state = ServerState.Attaching;

            var pid = _pid;
            var token = _startToken;
            MainThread.RunInBackground("LocalServer.Attach", () =>
            {
                var ours = ProcessTree.IsSameProcess(pid, token);
                MainThread.Post(() =>
                {
                    if (_pid != pid || _state != ServerState.Attaching) return; // Stop() took over

                    Backfill();
                    if (ours)
                    {
                        _state = ServerState.Running;
                    }
                    else
                    {
                        // Either it died while nobody was watching, or we cannot
                        // prove the pid is still the process we started.
                        _state = ServerState.Exited;
                        _exitCode = null;
                        _log.Append(string.IsNullOrEmpty(token)
                                ? $"Could not verify that pid {pid} is still the {_label.ToLowerInvariant()} this project started, so it was not re-attached. If it is still running, stop it yourself."
                                : $"{_label} is no longer running. It exited while the editor was reloading.",
                            LogLevel.Warning);
                        _pid = 0;
                        ClearRecord();
                    }
                    RaiseChanged();
                });
            });
        }

        /// <summary>
        /// Repopulates the view after a reload, since the in-memory buffer does not
        /// survive it: the tailer re-reads the end of the file. Those lines carry
        /// no timestamp — we only know when we read them, not when they were written.
        /// </summary>
        private static void Backfill()
        {
            ResetTail();
            var marker = $"── re-attached to {_label.ToLowerInvariant()} (pid {_pid}) ──";

            long length;
            try { length = new FileInfo(_logPath).Length; }
            catch
            {
                _log.Append(marker, LogLevel.Command);
                _log.Append("Earlier output is unavailable: the log file is gone.", LogLevel.Warning);
                return;
            }

            _tailOffset = Math.Max(0, length - MaxReadPerTick);
            _skipPartialLine = _tailOffset > 0; // a mid-line (or mid-character) start would render as garbage
            _backfilling = true;
            TailStep();
            _backfilling = false;
            _log.Append(marker, LogLevel.Command);
        }

        // ------------------------------------------------------------------
        // Tailing
        // ------------------------------------------------------------------

        private static void ResetTail()
        {
            _tailOffset = 0;
            _decoder.Reset();
            _partial.Length = 0;
            _skipPartialLine = false;
        }

        private static void TailStep()
        {
            if (string.IsNullOrEmpty(_logPath)) return;

            try
            {
                int read;
                using (var fs = new FileStream(_logPath, FileMode.Open, FileAccess.Read,
                           FileShare.ReadWrite | FileShare.Delete))
                {
                    if (fs.Length < _tailOffset) ResetTail(); // truncated or rotated under us
                    if (fs.Length == _tailOffset) return;

                    fs.Seek(_tailOffset, SeekOrigin.Begin);
                    read = fs.Read(_readBuffer, 0, (int)Math.Min(MaxReadPerTick, fs.Length - _tailOffset));
                }
                if (read <= 0) return;
                _tailOffset += read;

                // A persistent decoder keeps multi-byte characters split across
                // reads from turning into replacement chars.
                var decoded = _decoder.GetChars(_readBuffer, 0, read, _charBuffer, 0);
                Ingest(_charBuffer, decoded);
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
            {
                if (_missingLogWarned) return;
                _missingLogWarned = true;
                _log.Append("The log file went missing. Output will stop here; Stop still works.", LogLevel.Warning);
                RaiseChanged();
            }
            catch (IOException)
            {
                // Someone else is mid-write; next tick will catch up.
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Colyseus] Failed to read server log: " + ex.Message);
            }
        }

        /// <summary>
        /// Splits freshly decoded text into lines. Complete lines are cut straight
        /// from <paramref name="chars"/>; only an unterminated tail is carried over.
        /// </summary>
        private static void Ingest(char[] chars, int count)
        {
            var time = _backfilling ? default(DateTime) : DateTime.Now;
            var appended = false;
            var start = 0;

            for (var i = 0; i < count; i++)
            {
                if (chars[i] != '\n') continue;

                if (_skipPartialLine)
                {
                    _skipPartialLine = false;
                    _partial.Length = 0;
                }
                else if (_partial.Length > 0)
                {
                    _partial.Append(chars, start, i - start);
                    appended |= _log.AppendAuto(_partial.ToString(), time);
                    _partial.Length = 0;
                }
                else
                {
                    appended |= _log.AppendAuto(new string(chars, start, i - start), time);
                }
                start = i + 1;
            }

            if (start < count) _partial.Append(chars, start, count - start);

            // Guard against a single enormous line pinning memory.
            if (_partial.Length > MaxLineChars)
            {
                if (!_skipPartialLine) appended |= _log.AppendAuto(_partial.ToString(), time);
                _partial.Length = 0;
            }

            if (appended) RaiseChanged();
        }

        private static void FlushPartial()
        {
            if (_partial.Length == 0 || _skipPartialLine) return;
            _log.AppendAuto(_partial.ToString());
            _partial.Length = 0;
        }

        public static void ClearLog()
        {
            // The file keeps growing underneath; only the view is reset, so the
            // tail offset stays valid and we don't corrupt a live redirect.
            _log.Clear();
            RaiseChanged();
        }

        /// <summary>True when recent output mentions the port already being taken.</summary>
        public static bool SawPortInUse
        {
            get
            {
                var lines = _log.Lines;
                for (var i = lines.Count - 1; i >= 0 && i >= lines.Count - 40; i--)
                {
                    if (lines[i].Text.IndexOf("EADDRINUSE", StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
                return false;
            }
        }

        // ------------------------------------------------------------------
        // Persistence
        // ------------------------------------------------------------------

        [Serializable]
        private class ProcessRecord
        {
            public int pid;
            public string startToken;
            public string logPath;
            public string workingDir;
            public string command;
            public int port;
            public long startedAtTicks;
            public string label;
        }

        /// <summary>
        /// EditorPrefs rather than SessionState: SessionState is wiped on quit and
        /// on a crash, but a server left running on purpose (or orphaned by a
        /// crash, still holding the port) needs to be found by the next session.
        /// </summary>
        private static void SaveRecord()
        {
            var record = new ProcessRecord
            {
                pid = _pid,
                startToken = _startToken,
                logPath = _logPath,
                workingDir = _workingDir,
                command = _command,
                port = _port,
                startedAtTicks = _startedAtUtc.Ticks,
                label = _label,
            };

            try { EditorPrefs.SetString(RecordKey, JsonUtility.ToJson(record)); }
            catch (Exception ex) { Debug.LogWarning("[Colyseus] Could not persist server state: " + ex.Message); }
        }

        private static ProcessRecord LoadRecord()
        {
            try
            {
                var json = EditorPrefs.GetString(RecordKey, "");
                return string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<ProcessRecord>(json);
            }
            catch { return null; }
        }

        private static void ClearRecord()
        {
            try { EditorPrefs.DeleteKey(RecordKey); } catch { }
        }
    }
}
