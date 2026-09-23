using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Colyseus.Editor
{
    internal enum LogLevel { Info, Command, Success, Warning, Error }

    internal struct LogLine
    {
        public string Text;
        public LogLevel Level;

        /// <summary>"[HH:mm:ss]", formatted once on append; null when the time is unknown.</summary>
        public string Stamp;
    }

    /// <summary>
    /// Bounded, append-only line store behind a <see cref="LogView"/>. It also owns
    /// cleaning and classifying raw process output, so every producer gets the
    /// same treatment. Single-threaded: producers marshal onto the main thread.
    /// </summary>
    internal sealed class LogBuffer
    {
        /// <summary>Oldest lines are dropped past this, so a chatty server can't grow the editor's heap.</summary>
        private const int Capacity = 5000;

        private readonly List<LogLine> _lines = new List<LogLine>();
        private bool _sawOutput;

        public IReadOnlyList<LogLine> Lines => _lines;
        public int Count => _lines.Count;

        /// <summary>Lines dropped from the front so far; turns an index into a stable absolute position.</summary>
        public long Removed { get; private set; }

        /// <summary>Bumped by <see cref="Clear"/>, which invalidates every absolute position.</summary>
        public int Generation { get; private set; }

        /// <summary>Widest line held, in characters — drives the horizontal scroll extent.</summary>
        public int MaxColumns { get; private set; }

        public int ErrorCount { get; private set; }
        public int WarningCount { get; private set; }

        /// <summary>Adds a line the caller composed (headers, status), as-is.</summary>
        public void Append(string text, LogLevel level) => Add(text ?? "", level, DateTime.Now);

        /// <summary>Adds a raw line of process output: cleaned, then classified.</summary>
        public bool AppendAuto(string raw) => AppendAuto(raw, DateTime.Now);

        /// <summary>
        /// As <see cref="AppendAuto(string)"/>, with an explicit time; pass
        /// <c>default</c> when it is unknown. False when the line was dropped.
        /// </summary>
        public bool AppendAuto(string raw, DateTime time)
        {
            var line = Sanitize(raw);
            // Leading blanks are noise; blank lines between real output are not.
            if (line.Length == 0 && !_sawOutput) return false;
            _sawOutput = true;
            Add(line, Classify(line), time);
            return true;
        }

        private void Add(string text, LogLevel level, DateTime time)
        {
            _lines.Add(new LogLine
            {
                Text = text,
                Level = level,
                Stamp = time == default(DateTime) ? null : "[" + time.ToString("HH:mm:ss") + "]",
            });

            if (text.Length > MaxColumns) MaxColumns = text.Length;
            if (level == LogLevel.Error) ErrorCount++;
            else if (level == LogLevel.Warning) WarningCount++;

            // Trim in batches; RemoveRange(0, n) is O(count) so we don't want it per line.
            if (_lines.Count > Capacity + 256)
            {
                var drop = _lines.Count - Capacity;
                _lines.RemoveRange(0, drop);
                Removed += drop;
                Recount(); // the badges must describe what is still in the buffer
            }
        }

        public void Clear()
        {
            _lines.Clear();
            _sawOutput = false;
            Removed = 0;
            Generation++;
            Recount();
        }

        private void Recount()
        {
            ErrorCount = 0;
            WarningCount = 0;
            MaxColumns = 0;
            foreach (var line in _lines)
            {
                if (line.Level == LogLevel.Error) ErrorCount++;
                else if (line.Level == LogLevel.Warning) WarningCount++;
                if (line.Text.Length > MaxColumns) MaxColumns = line.Text.Length;
            }
        }

        public string ToPlainText(bool withTimestamps = true)
        {
            var sb = new StringBuilder(_lines.Count * 64);
            foreach (var line in _lines) sb.AppendLine(Format(line, withTimestamps));
            return sb.ToString();
        }

        public static string Format(LogLine line, bool withTimestamps) =>
            withTimestamps && line.Stamp != null ? line.Stamp + " " + line.Text : line.Text;

        // ------------------------------------------------------------------
        // Cleaning
        // ------------------------------------------------------------------

        // Colors, cursor moves and OSC title sequences, which even NO_COLOR-aware
        // tools sometimes emit.
        private static readonly Regex AnsiEscape = new Regex(
            @"\x1B\[[0-9;?]*[ -/]*[@-~]|\x1B\][^\x07\x1B]*(?:\x07|\x1B\\)|\x1B[@-Z\\-_]",
            RegexOptions.Compiled);

        /// <summary>
        /// Strips the terminal control noise a TTY-oriented tool emits anyway, and
        /// collapses a progress-bar line to its final state.
        /// </summary>
        public static string Sanitize(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";

            var line = raw.TrimEnd('\r', '\n');

            // Carriage returns rewrite the same line; only the last write matters.
            var lastCr = line.LastIndexOf('\r');
            if (lastCr >= 0) line = line.Substring(lastCr + 1);

            // NO_COLOR is set for every child, so most lines have no escapes at all.
            if (line.IndexOf('\x1B') >= 0) line = AnsiEscape.Replace(line, "");
            if (line.IndexOf('\t') >= 0) line = line.Replace("\t", "    ");
            if (line.IndexOf('\b') >= 0) line = line.Replace("\b", "");
            return line;
        }

        // ------------------------------------------------------------------
        // Classifying
        // ------------------------------------------------------------------

        // "Found 3 errors", "0 warnings": a count decides by its number, and is
        // removed before the keyword scan so "0 errors" can't read as a failure.
        private static readonly Regex Counts = new Regex(
            @"\b(\d+)\s+(error|warning)s?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex ErrorMarker = new Regex(
            @"ERR!|\b(ERROR|FATAL|EADDRINUSE|ECONNREFUSED|MODULE_NOT_FOUND|UnhandledPromiseRejection\w*)\b|\berror TS\d+|Error:|^\s*(✘|✖|❌)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex WarningMarker = new Regex(
            @"\bWARN(ING)?\b|DeprecationWarning|^\s*⚠", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex SuccessMarker = new Regex(
            @"^\s*(✔|✓)|\blistening on\b|\bready in\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Best-effort severity from the line's text, matched on whole words.
        /// Deliberately conservative: a false "error" color on ordinary output is
        /// worse than a missed one.
        /// </summary>
        public static LogLevel Classify(string line)
        {
            if (string.IsNullOrEmpty(line)) return LogLevel.Info;
            if (line.StartsWith("$ ", StringComparison.Ordinal)) return LogLevel.Command;
            if (!MayClassify(line)) return LogLevel.Info;

            var countedErrors = false;
            var countedWarnings = false;
            var rest = line;
            if (line.IndexOf(' ') >= 0 && Counts.IsMatch(line))
            {
                foreach (Match m in Counts.Matches(line))
                {
                    if (m.Groups[1].Value.TrimStart('0').Length == 0) continue; // zero
                    if (char.ToLowerInvariant(m.Groups[2].Value[0]) == 'e') countedErrors = true;
                    else countedWarnings = true;
                }
                rest = Counts.Replace(line, "");
            }

            if (countedErrors || ErrorMarker.IsMatch(rest)) return LogLevel.Error;
            if (countedWarnings || WarningMarker.IsMatch(rest)) return LogLevel.Warning;
            if (SuccessMarker.IsMatch(rest)) return LogLevel.Success;
            return LogLevel.Info;
        }

        // Every marker above contains one of these, so a line with none of them
        // can skip the regexes — Mono's regex engine is slow, and most server
        // output is ordinary text.
        private static readonly string[] Triggers =
            { "err", "warn", "fatal", "econn", "eaddr", "module_not", "unhandled", "listening", "ready in" };
        private const string SymbolTriggers = "✔✓✘✖❌⚠";

        private static bool MayClassify(string line)
        {
            foreach (var trigger in Triggers)
            {
                if (line.IndexOf(trigger, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            for (var i = 0; i < line.Length; i++)
            {
                if (char.IsWhiteSpace(line[i])) continue;
                return SymbolTriggers.IndexOf(line[i]) >= 0;
            }
            return false;
        }
    }

    /// <summary>
    /// Scrollable, virtualized console for a <see cref="LogBuffer"/>: monospace,
    /// timestamped, colored by severity, with search, auto-scroll and copy.
    ///
    /// Only the lines intersecting the viewport are drawn, so a full buffer costs
    /// the same to repaint as an empty one.
    /// </summary>
    internal sealed class LogView
    {
        private const int MaxScrollColumns = 2000;

        private static readonly GUIContent AutoScrollLabel =
            new GUIContent("Auto-scroll", "Follow new output. Turns off when you scroll up.");
        private static readonly GUIContent CopyAllLabel =
            new GUIContent("Copy All", "Copy the whole log to the clipboard");
        private static readonly GUIContent ClearLabel =
            new GUIContent("Clear", "Discard the output shown here");

        private Vector2 _scroll;
        private string _filter = "";
        private int _selected = -1;

        // Absolute positions (see LogBuffer.Removed) of the lines passing the
        // filter, extended incrementally as lines arrive rather than rebuilt.
        private readonly List<long> _filtered = new List<long>();
        private long _scannedTo;
        private int _filteredGeneration = -1;
        private string _filteredFor;

        private GUIStyle[] _levelStyles;
        private GUIStyle _searchStyle;
        private bool _wasProSkin;
        private float _charWidth = 7f;
        private float _lineHeight = 15f;

        public bool AutoScroll = true;
        public bool ShowTimestamps = true;

        /// <summary>Raised when the user picks "Clear" from the toolbar or context menu.</summary>
        public event Action ClearRequested;

        // ------------------------------------------------------------------
        // Toolbar
        // ------------------------------------------------------------------

        /// <summary>
        /// The strip above the log: auto-scroll, search, counts, then whatever the
        /// window wants to add on the right.
        /// </summary>
        public void DrawToolbar(LogBuffer buffer, Action extraButtons = null)
        {
            ColyseusEditorStyles.Ensure();
            EnsureStyles();

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Output", ColyseusEditorStyles.ToolbarLabel, GUILayout.Width(48));

                AutoScroll = GUILayout.Toggle(AutoScroll, AutoScrollLabel, EditorStyles.toolbarButton, GUILayout.Width(80));

                // Both badges are always emitted (blank when zero), so the control
                // count never depends on the output.
                GUILayout.Space(4);
                DrawCount(buffer.ErrorCount, ColyseusEditorStyles.Red, "error");
                DrawCount(buffer.WarningCount, ColyseusEditorStyles.Amber, "warning");

                GUILayout.FlexibleSpace();

                var next = GUILayout.TextField(_filter, _searchStyle, GUILayout.MinWidth(60), GUILayout.MaxWidth(160));
                if (next != _filter)
                {
                    _filter = next;
                    _selected = -1;
                }

                using (new EditorGUI.DisabledScope(buffer.Count == 0))
                {
                    if (GUILayout.Button(CopyAllLabel, EditorStyles.toolbarButton, GUILayout.Width(62)))
                        EditorGUIUtility.systemCopyBuffer = buffer.ToPlainText(ShowTimestamps);

                    if (GUILayout.Button(ClearLabel, EditorStyles.toolbarButton, GUILayout.Width(46)))
                        ClearRequested?.Invoke();
                }

                extraButtons?.Invoke();
            }
        }

        private static void DrawCount(int count, Color color, string noun)
        {
            var content = count > 0
                ? new GUIContent("● " + count, $"{count} {noun}{(count == 1 ? "" : "s")}")
                : GUIContent.none;
            ColyseusEditorStyles.ColoredLabel(content, color, EditorStyles.miniLabel, count > 0 ? 34 : 0);
        }

        // ------------------------------------------------------------------
        // Body
        // ------------------------------------------------------------------

        public void Draw(LogBuffer buffer, string emptyMessage = null, params GUILayoutOption[] options)
        {
            ColyseusEditorStyles.Ensure();
            EnsureStyles();
            Refilter(buffer);

            var viewRect = GUILayoutUtility.GetRect(0, float.MaxValue, 0, float.MaxValue, options);
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(viewRect, ColyseusEditorStyles.LogBg);

            if (_filtered.Count == 0)
            {
                var message = buffer.Count == 0
                    ? (emptyMessage ?? "No output yet.")
                    : $"No lines match “{_filter}”.";
                GUI.Label(new Rect(viewRect.x + 8, viewRect.y + 8, viewRect.width - 16, 40), message,
                    ColyseusEditorStyles.MiniMuted);
                return;
            }

            var timestampWidth = ShowTimestamps ? _charWidth * 10f + 8f : 0f;
            // Clamped: one pathological line (a minified stack trace, say) would
            // otherwise stretch the scroll extent to hundreds of thousands of pixels.
            var widest = Mathf.Min(buffer.MaxColumns, MaxScrollColumns) * _charWidth;
            var contentWidth = Mathf.Max(viewRect.width - 16f, timestampWidth + widest + 16f);
            var contentHeight = _filtered.Count * _lineHeight + 6f;
            var contentRect = new Rect(0, 0, contentWidth, contentHeight);

            var maxScrollY = Mathf.Max(0f, contentHeight - viewRect.height);
            if (AutoScroll) _scroll.y = maxScrollY;

            // Captured before the scroll view consumes it.
            var eventType = Event.current.type;
            var mouseInView = viewRect.Contains(Event.current.mousePosition);

            _scroll = GUI.BeginScrollView(viewRect, _scroll, contentRect);

            var first = Mathf.Max(0, Mathf.FloorToInt(_scroll.y / _lineHeight) - 1);
            var last = Mathf.Min(_filtered.Count, first + Mathf.CeilToInt(viewRect.height / _lineHeight) + 2);

            for (var i = first; i < last; i++)
            {
                var line = LineAt(buffer, _filtered[i]);
                var y = i * _lineHeight + 3f;

                if (i == _selected && Event.current.type == EventType.Repaint)
                {
                    var highlight = ColyseusEditorStyles.Accent;
                    highlight.a = 0.22f;
                    EditorGUI.DrawRect(new Rect(0, y, contentWidth, _lineHeight), highlight);
                }

                if (ShowTimestamps && line.Stamp != null)
                    GUI.Label(new Rect(4, y, timestampWidth, _lineHeight), line.Stamp, ColyseusEditorStyles.MonoMuted);

                GUI.Label(new Rect(timestampWidth + 4, y, contentWidth - timestampWidth - 8, _lineHeight),
                    line.Text, _levelStyles[(int)line.Level]);
            }

            GUI.EndScrollView();

            HandleInput(buffer, viewRect, eventType, mouseInView, maxScrollY);
        }

        private void HandleInput(LogBuffer buffer, Rect viewRect, EventType eventType, bool mouseInView, float maxScrollY)
        {
            // A wheel or a scrollbar drag means the user took over; re-arm
            // auto-scroll only once they come back to the bottom.
            if ((eventType == EventType.ScrollWheel || eventType == EventType.MouseDrag) && mouseInView)
                AutoScroll = _scroll.y >= maxScrollY - 4f;

            var e = Event.current;

            if ((e.type == EventType.MouseDown || e.type == EventType.ContextClick) && mouseInView)
            {
                var index = Mathf.FloorToInt((e.mousePosition.y - viewRect.y + _scroll.y - 3f) / _lineHeight);
                _selected = index >= 0 && index < _filtered.Count ? index : -1;

                if (e.type == EventType.ContextClick || e.button == 1) ShowContextMenu(buffer);
                else GUI.FocusControl(null);
                e.Use();
            }

            if (e.type == EventType.ValidateCommand && e.commandName == "Copy")
            {
                e.Use();
            }
            else if (e.type == EventType.ExecuteCommand && e.commandName == "Copy")
            {
                if (_selected >= 0) CopyLine(buffer, _selected);
                else EditorGUIUtility.systemCopyBuffer = buffer.ToPlainText(ShowTimestamps);
                e.Use();
            }
        }

        private void ShowContextMenu(LogBuffer buffer)
        {
            var menu = new GenericMenu();
            var selected = _selected;

            AddItem(menu, "Copy Line", selected >= 0, () => CopyLine(buffer, selected));
            AddItem(menu, "Copy All", buffer.Count > 0,
                () => EditorGUIUtility.systemCopyBuffer = buffer.ToPlainText(ShowTimestamps));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Show Timestamps"), ShowTimestamps, () => ShowTimestamps = !ShowTimestamps);
            menu.AddItem(new GUIContent("Auto-scroll"), AutoScroll, () => AutoScroll = !AutoScroll);
            menu.AddSeparator("");
            AddItem(menu, "Clear", buffer.Count > 0, () => ClearRequested?.Invoke());

            menu.ShowAsContext();
        }

        private static void AddItem(GenericMenu menu, string label, bool enabled, GenericMenu.MenuFunction action)
        {
            if (enabled) menu.AddItem(new GUIContent(label), false, action);
            else menu.AddDisabledItem(new GUIContent(label));
        }

        private void CopyLine(LogBuffer buffer, int filteredIndex)
        {
            if (filteredIndex < 0 || filteredIndex >= _filtered.Count) return;
            var position = _filtered[filteredIndex] - buffer.Removed;
            // The buffer may have been trimmed since the menu opened.
            if (position < 0 || position >= buffer.Count) return;
            EditorGUIUtility.systemCopyBuffer = LogBuffer.Format(buffer.Lines[(int)position], ShowTimestamps);
        }

        private static LogLine LineAt(LogBuffer buffer, long absolute) =>
            buffer.Lines[(int)(absolute - buffer.Removed)];

        // ------------------------------------------------------------------
        // Filtering & styles
        // ------------------------------------------------------------------

        /// <summary>
        /// Keeps <see cref="_filtered"/> in step with the buffer. Only lines that
        /// arrived since the last call are tested; a full rescan happens only when
        /// the filter text changes or the buffer is cleared.
        /// </summary>
        private void Refilter(LogBuffer buffer)
        {
            var end = buffer.Removed + buffer.Count;

            if (_filteredGeneration != buffer.Generation || _filteredFor != _filter)
            {
                _filteredGeneration = buffer.Generation;
                _filteredFor = _filter;
                _filtered.Clear();
                _scannedTo = buffer.Removed;
            }
            else if (_scannedTo == end)
            {
                return; // nothing new (trims only happen on append, so nothing trimmed either)
            }

            // Forget positions the buffer has trimmed away.
            var dropped = 0;
            while (dropped < _filtered.Count && _filtered[dropped] < buffer.Removed) dropped++;
            if (dropped > 0)
            {
                _filtered.RemoveRange(0, dropped);
                _selected = _selected >= dropped ? _selected - dropped : -1;
            }

            var hasFilter = _filter.Length > 0;
            for (var absolute = Math.Max(_scannedTo, buffer.Removed); absolute < end; absolute++)
            {
                if (hasFilter && LineAt(buffer, absolute).Text.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                _filtered.Add(absolute);
            }
            _scannedTo = end;

            if (_selected >= _filtered.Count) _selected = -1;
        }

        private void EnsureStyles()
        {
            if (_levelStyles != null && _wasProSkin == EditorGUIUtility.isProSkin) return;
            _wasProSkin = EditorGUIUtility.isProSkin;

            var baseStyle = ColyseusEditorStyles.Mono;
            _levelStyles = new GUIStyle[5];
            _levelStyles[(int)LogLevel.Info] = baseStyle;
            _levelStyles[(int)LogLevel.Command] = ColyseusEditorStyles.Variant(baseStyle, ColyseusEditorStyles.Accent, FontStyle.Bold);
            _levelStyles[(int)LogLevel.Success] = ColyseusEditorStyles.Variant(baseStyle, ColyseusEditorStyles.Green);
            _levelStyles[(int)LogLevel.Warning] = ColyseusEditorStyles.Variant(baseStyle, ColyseusEditorStyles.Amber);
            _levelStyles[(int)LogLevel.Error] = ColyseusEditorStyles.Variant(baseStyle, ColyseusEditorStyles.Red);

            // toolbarSearchField is not exposed on every Unity version this package supports.
            _searchStyle = GUI.skin.FindStyle("ToolbarSearchTextField")
                           ?? GUI.skin.FindStyle("ToolbarSeachTextField") // Unity's own historical typo
                           ?? EditorStyles.toolbarTextField;

            _charWidth = Mathf.Max(4f, baseStyle.CalcSize(new GUIContent("0")).x);
            _lineHeight = Mathf.Max(14f, Mathf.Ceil(baseStyle.lineHeight) + 2f);
        }
    }
}
