using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Colyseus.Editor
{
    /// <summary>
    /// Shared look and chrome for the Colyseus editor windows: colors, styles,
    /// icons, the few drawing primitives they all use, and their common menus.
    ///
    /// GUIStyles can only be built inside OnGUI (EditorStyles is null before the
    /// first repaint), so everything here is created lazily through <see cref="Ensure"/>.
    /// </summary>
    internal static class ColyseusEditorStyles
    {
        public const string DocumentationUrl = "https://docs.colyseus.io/?utm_source=unity-editor";
        public const string IssuesUrl = "https://github.com/colyseus/colyseus-unity-sdk/issues";

        private static bool _built;
        private static bool _wasProSkin;

        // ------------------------------------------------------------------
        // Palette
        // ------------------------------------------------------------------

        private static bool Pro => EditorGUIUtility.isProSkin;

        public static Color Green   => Pro ? new Color(0.40f, 0.78f, 0.44f) : new Color(0.18f, 0.55f, 0.24f);
        public static Color Red     => Pro ? new Color(0.90f, 0.40f, 0.38f) : new Color(0.72f, 0.18f, 0.16f);
        public static Color Amber   => Pro ? new Color(0.95f, 0.72f, 0.28f) : new Color(0.70f, 0.48f, 0.05f);
        public static Color Accent  => Pro ? new Color(0.40f, 0.72f, 1.00f) : new Color(0.10f, 0.40f, 0.80f);
        public static Color Muted   => Pro ? new Color(0.58f, 0.58f, 0.58f) : new Color(0.42f, 0.42f, 0.42f);
        public static Color Idle    => Pro ? new Color(0.45f, 0.45f, 0.45f) : new Color(0.55f, 0.55f, 0.55f);

        /// <summary>Background for the header bar and the log surface.</summary>
        public static Color HeaderBg  => Pro ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.78f, 0.78f, 0.78f);
        public static Color LogBg     => Pro ? new Color(0.16f, 0.16f, 0.16f) : new Color(0.87f, 0.87f, 0.87f);
        public static Color Separator => Pro ? new Color(0.13f, 0.13f, 0.13f) : new Color(0.60f, 0.60f, 0.60f);

        // Button tints. IMGUI multiplies these into the skin's button texture, so
        // they have to stay close to white or the button loses its bevel.
        public static Color StartTint => Pro ? new Color(0.55f, 0.95f, 0.60f) : new Color(0.70f, 1.00f, 0.75f);
        public static Color StopTint  => Pro ? new Color(1.00f, 0.55f, 0.50f) : new Color(1.00f, 0.70f, 0.66f);

        // ------------------------------------------------------------------
        // Styles
        // ------------------------------------------------------------------

        public static GUIStyle HeaderTitle   { get; private set; }
        public static GUIStyle HeaderBarBg   { get; private set; }
        public static GUIStyle StatusText    { get; private set; }
        public static GUIStyle StatusDetail  { get; private set; }
        public static GUIStyle Mono          { get; private set; }
        public static GUIStyle MonoMuted     { get; private set; }
        public static GUIStyle MiniMuted     { get; private set; }
        public static GUIStyle ToolbarLabel  { get; private set; }
        public static GUIStyle TabButton     { get; private set; }
        public static GUIStyle TabButtonSelected { get; private set; }
        public static GUIStyle JsonTextArea  { get; private set; }

        private static Font _monoFont;

        public static void Ensure()
        {
            // Rebuild when the user switches skin, otherwise colors stay stale.
            if (_built && _wasProSkin == Pro) return;
            _built = true;
            _wasProSkin = Pro;
            IconCache.Clear();

            if (_monoFont == null)
            {
                _monoFont = Font.CreateDynamicFontFromOSFont(
                    new[] { "Menlo", "Consolas", "DejaVu Sans Mono", "Liberation Mono", "Courier New" }, 11);
            }

            HeaderTitle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(6, 6, 0, 0),
            };

            HeaderBarBg = new GUIStyle(EditorStyles.toolbar)
            {
                fixedHeight = 26,
                padding = new RectOffset(4, 4, 0, 0),
            };

            StatusText = new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            StatusDetail = Variant(EditorStyles.miniLabel, Muted);
            StatusDetail.alignment = TextAnchor.MiddleLeft;

            Mono = new GUIStyle(EditorStyles.label)
            {
                font = _monoFont,
                fontSize = 11,
                alignment = TextAnchor.UpperLeft,
                wordWrap = false,
                richText = false,
                padding = new RectOffset(4, 4, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
            };
            MonoMuted = Variant(Mono, Muted);
            MiniMuted = Variant(EditorStyles.miniLabel, Muted);

            ToolbarLabel = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(6, 6, 0, 0),
            };

            TabButton = new GUIStyle(EditorStyles.toolbarButton);
            TabButtonSelected = Variant(EditorStyles.toolbarButton, Accent, FontStyle.Bold);
            JsonTextArea = new GUIStyle(EditorStyles.textArea) { wordWrap = true, stretchHeight = true };
        }

        /// <summary>A copy of <paramref name="from"/> with another text color (and weight).</summary>
        public static GUIStyle Variant(GUIStyle from, Color color, FontStyle fontStyle = FontStyle.Normal)
        {
            var style = new GUIStyle(from) { fontStyle = fontStyle };
            style.normal.textColor = color;
            return style;
        }

        // ------------------------------------------------------------------
        // Icons
        // ------------------------------------------------------------------

        private static Texture2D _icon;
        private static bool _iconSearched;

        // IconContent is a lookup per call, and headers ask on every OnGUI.
        private static readonly Dictionary<string, GUIContent> IconCache = new Dictionary<string, GUIContent>();

        /// <summary>
        /// The Colyseus mark, for window title tabs. EditorGUIUtility.IconContent
        /// only searches "Assets/Editor Default Resources", which is not where this
        /// package keeps it (and can't be, for a UPM install) — so look it up as an
        /// asset instead.
        /// </summary>
        public static Texture2D WindowIcon
        {
            get
            {
                if (_iconSearched) return _icon;
                _iconSearched = true;
                try
                {
                    foreach (var guid in AssetDatabase.FindAssets("ColyseusSettings t:Texture2D"))
                    {
                        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guid));
                        if (tex != null) { _icon = tex; break; }
                    }
                }
                catch { /* icon is cosmetic; never break a window over it */ }
                return _icon;
            }
        }

        public static GUIContent TitleContent(string title) => new GUIContent(title, WindowIcon, title);

        /// <summary>A builtin editor icon texture, or null when this Unity version lacks it.</summary>
        private static Texture Icon(string name)
        {
            try { return EditorGUIUtility.IconContent(name)?.image; }
            catch { return null; } // name not present in this Unity version
        }

        /// <summary>
        /// Unity's own "pane options" glyph for an overflow menu button, falling
        /// back to ASCII rather than a character the editor font may not have.
        /// </summary>
        public static GUIContent MenuButton
        {
            get
            {
                if (IconCache.TryGetValue("", out var cached)) return cached;
                var icon = Icon("_Menu") ?? Icon("pane options") ?? Icon("_Popup");
                return IconCache[""] = icon != null
                    ? new GUIContent(icon, "More actions")
                    : new GUIContent("...", "More actions");
            }
        }

        /// <summary>A builtin icon with a text label, falling back to text alone. Cached.</summary>
        public static GUIContent IconLabel(string iconName, string text, string tooltip)
        {
            var key = iconName + "\n" + text;
            if (IconCache.TryGetValue(key, out var cached)) return cached;
            var icon = Icon(iconName);
            return IconCache[key] = icon != null
                ? new GUIContent(" " + text, icon, tooltip)
                : new GUIContent(text, tooltip);
        }

        // ------------------------------------------------------------------
        // Drawing primitives
        // ------------------------------------------------------------------

        /// <summary>Window header: bold title on the left, caller-drawn controls on the right.</summary>
        public static void DrawHeader(string title, Action rightSide)
        {
            var rect = EditorGUILayout.BeginHorizontal(HeaderBarBg, GUILayout.Height(26));
            EditorGUI.DrawRect(rect, HeaderBg);

            GUILayout.Label(title, HeaderTitle, GUILayout.Height(26));
            GUILayout.FlexibleSpace();
            rightSide();

            EditorGUILayout.EndHorizontal();
            DrawSeparator();
        }

        public static void DrawSeparator()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, Separator);
        }

        /// <summary>
        /// "■ Server is running  pid 40504 · up 00:12:41" — a colored dot, a bold
        /// status and a muted detail tail.
        /// </summary>
        public static void DrawStatusRow(Color dotColor, string status, string detail)
        {
            using (new EditorGUILayout.HorizontalScope(GUILayout.Height(22)))
            {
                GUILayout.Space(6);

                var dotRect = GUILayoutUtility.GetRect(10, 10, GUILayout.Width(10), GUILayout.Height(22));
                EditorGUI.DrawRect(new Rect(dotRect.x, dotRect.y + 7, 8, 8), dotColor);

                GUILayout.Space(6);
                GUILayout.Label(status, StatusText, GUILayout.Height(22));

                // Always emitted, empty or not: a control that comes and goes
                // desynchronises the layout cache against later events.
                GUILayout.Label(detail ?? "", StatusDetail, GUILayout.Height(22));

                GUILayout.FlexibleSpace();
            }
            DrawSeparator();
        }

        /// <summary>A button tinted with <paramref name="tint"/>, restoring the GUI color after.</summary>
        public static bool TintedButton(GUIContent content, Color tint, float width)
        {
            var previous = GUI.backgroundColor;
            GUI.backgroundColor = GUI.enabled ? tint : previous;
            var clicked = GUILayout.Button(content, EditorStyles.toolbarButton, GUILayout.Width(width));
            GUI.backgroundColor = previous;
            return clicked;
        }

        public static void ColoredLabel(string text, Color color, GUIStyle style, float width) =>
            ColoredLabel(new GUIContent(text), color, style, width);

        public static void ColoredLabel(GUIContent content, Color color, GUIStyle style, float width)
        {
            var previous = GUI.contentColor;
            GUI.contentColor = color;
            GUILayout.Label(content, style, GUILayout.Width(width));
            GUI.contentColor = previous;
        }

        // ------------------------------------------------------------------
        // Behaviour shared by the windows
        // ------------------------------------------------------------------

        /// <summary>
        /// Runs an action after the current GUI pass. Mutating state (or opening a
        /// modal panel) inside OnGUI changes which controls the remaining draw calls
        /// emit, and IMGUI's layout cache then no longer matches.
        /// </summary>
        public static void Defer(Action action)
        {
            EditorApplication.delayCall += () => action();
        }

        /// <summary>Links to the sibling windows, then documentation and issue links.</summary>
        public static void AddCommonMenuItems(GenericMenu menu, Type current)
        {
            if (current != typeof(ServerWindow))
                menu.AddItem(new GUIContent("Game Server Window"), false, ServerWindow.ShowWindow);
            if (current != typeof(SchemaCodegenWindow))
                menu.AddItem(new GUIContent("Schema Codegen Window"), false, SchemaCodegenWindow.ShowWindow);
            if (current != typeof(RoomInspector))
                menu.AddItem(new GUIContent("Room Inspector Window"), false, RoomInspector.ShowWindow);

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Documentation"), false, () => Application.OpenURL(DocumentationUrl));
            menu.AddItem(new GUIContent("Report an Issue"), false, () => Application.OpenURL(IssuesUrl));
        }
    }

    /// <summary>
    /// Base for the Colyseus windows. OnGUI is split so that the state a window
    /// draws from is captured once per frame, on the Layout event, before any
    /// drawing: IMGUI replays the layout it built then for every later event, so
    /// drawing from live state that changed in between desynchronises it.
    /// </summary>
    public abstract class ColyseusWindow : EditorWindow, IHasCustomMenu
    {
        private void OnGUI()
        {
            ColyseusEditorStyles.Ensure();
            if (Event.current.type == EventType.Layout) CaptureState();
            DrawContents();
        }

        /// <summary>Copy whatever live state the frame will read into fields.</summary>
        protected virtual void CaptureState() { }

        protected abstract void DrawContents();

        /// <summary>Also fills Unity's built-in window overflow menu.</summary>
        public virtual void AddItemsToMenu(GenericMenu menu)
        {
            ColyseusEditorStyles.AddCommonMenuItems(menu, GetType());
        }

        /// <summary>The overflow button drawn at the right of a header.</summary>
        protected void DrawMenuButton()
        {
            if (GUILayout.Button(ColyseusEditorStyles.MenuButton, EditorStyles.toolbarButton, GUILayout.Width(26)))
            {
                var menu = new GenericMenu();
                AddItemsToMenu(menu);
                menu.ShowAsContext();
            }
        }
    }
}
