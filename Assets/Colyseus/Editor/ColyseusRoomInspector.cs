using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Colyseus;
using Colyseus.Schema;
using UnityEditor;
using UnityEngine;
using System.Text;
using GameDevWare.Serialization;

namespace Colyseus.Editor
{
    /// <summary>
    /// Static initializer to automatically capture message types from rooms
    /// </summary>
    static class RoomMessageType
    {
        // Shared storage for message types across all rooms
        public static readonly Dictionary<string, Dictionary<string, object>> CapturedMessageTypes = new Dictionary<string, Dictionary<string, object>>();
        
        [UnityEditor.InitializeOnLoadMethod]
        private static void Initialize()
        {
            // Subscribe to play mode state changes to reset tracking
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode)
            {
                // Clear captured data when exiting play mode
                CapturedMessageTypes.Clear();
            }
        }
    }

    /// <summary>
    /// Unity Editor window for inspecting connected Colyseus room states in real-time
    /// </summary>
    public class ColyseusRoomInspector : EditorWindow
    {
        private Vector2 _scrollPosition;
        private bool _autoRefresh = true;
        private double _lastRefreshTime;
        private const double RefreshInterval = 0.5; // Refresh every 0.5 seconds
        private Dictionary<string, bool> _foldouts = new Dictionary<string, bool>();
        private Dictionary<string, Dictionary<string, string>> _messageInputs = new Dictionary<string, Dictionary<string, string>>();
        private Dictionary<string, int> _selectedMessageTypeIndex = new Dictionary<string, int>();
        private Dictionary<string, string> _lastSelectedMessageType = new Dictionary<string, string>();
        private Dictionary<string, string> _rawJsonInputs = new Dictionary<string, string>();
        private Dictionary<string, bool> _useRawJson = new Dictionary<string, bool>();

        [MenuItem("Window/Colyseus/Room Inspector")]
        public static void ShowWindow()
        {
            var window = GetWindow<ColyseusRoomInspector>("Colyseus Room Inspector");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (_autoRefresh && EditorApplication.timeSinceStartup - _lastRefreshTime > RefreshInterval)
            {
                _lastRefreshTime = EditorApplication.timeSinceStartup;
                Repaint();
            }
        }

        private void OnGUI()
        {
            DrawToolbar();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            var rooms = FindAllColyseusRooms();

            if (rooms.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No active Colyseus rooms found.\n\n" +
                    "Make sure:\n" +
                    "• A scene is running (Play mode)\n" +
                    "• You have a MonoBehaviour with a ColyseusRoom field\n" +
                    "• The room is connected to the server",
                    MessageType.Info
                );
            }
            else
            {
                foreach (var roomInfo in rooms)
                {
                    DrawRoomInspector(roomInfo);
                    EditorGUILayout.Space(10);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto Refresh", EditorStyles.toolbarButton, GUILayout.Width(100));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Refresh Now", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                Repaint();
            }

            if (GUILayout.Button("Copy State JSON", EditorStyles.toolbarButton, GUILayout.Width(120)))
            {
                CopyStateToClipboard();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
        }

        private void CopyStateToClipboard()
        {
            try
            {
                var rooms = FindAllColyseusRooms();
                if (rooms.Count == 0)
                {
                    EditorUtility.DisplayDialog("Copy State", "No active rooms found to copy.", "OK");
                    return;
                }

                var stateData = new System.Text.StringBuilder();
                stateData.AppendLine("=== Colyseus Room State ===");
                stateData.AppendLine($"Captured at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                stateData.AppendLine();

                foreach (var room in rooms)
                {
                    stateData.AppendLine($"Room: {room.Name} ({room.RoomId})");
                    stateData.AppendLine($"Session ID: {room.SessionId}");
                    stateData.AppendLine($"Connected: {room.IsConnected}");
                    stateData.AppendLine();

                    if (room.State != null)
                    {
                        stateData.AppendLine("State:");
                        SerializeStateToText(room.State, room.StateType, stateData, 1);
                    }
                    else
                    {
                        stateData.AppendLine("State: null");
                    }

                    stateData.AppendLine();
                    stateData.AppendLine("---");
                    stateData.AppendLine();
                }

                EditorGUIUtility.systemCopyBuffer = stateData.ToString();
                Debug.Log("Room state copied to clipboard!");
                EditorUtility.DisplayDialog("Copy State", "Room state has been copied to clipboard!", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to copy state: {ex.Message}");
                EditorUtility.DisplayDialog("Copy State", $"Failed to copy state:\n{ex.Message}", "OK");
            }
        }

        private void SerializeStateToText(object obj, System.Type type, System.Text.StringBuilder sb, int indent)
        {
            if (obj == null || indent > 10)
            {
                return;
            }

            var indentStr = new string(' ', indent * 2);
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => f.GetCustomAttribute<Colyseus.Schema.Type>() != null)
                .OrderBy(f => f.GetCustomAttribute<Colyseus.Schema.Type>().Index)
                .ToList();

            foreach (var field in fields)
            {
                var fieldValue = field.GetValue(obj);
                var fieldType = field.FieldType;

                if (IsPrimitiveOrString(fieldType))
                {
                    sb.AppendLine($"{indentStr}{field.Name}: {fieldValue ?? "null"}");
                }
                else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(MapSchema<>))
                {
                    var itemsProperty = fieldType.GetField("items");
                    var itemsValue = itemsProperty?.GetValue(fieldValue);
                    var enumerable = itemsValue as IDictionary;
                    var count = enumerable?.Count ?? 0;
                    sb.AppendLine($"{indentStr}{field.Name} (MapSchema): {count} items");

                    if (enumerable != null && count > 0)
                    {
                        foreach (DictionaryEntry kvp in enumerable)
                        {
                            var key = kvp.Key?.ToString() ?? "null";
                            sb.AppendLine($"{indentStr}  [{key}]:");
                            if (kvp.Value != null && typeof(Schema.Schema).IsAssignableFrom(kvp.Value.GetType()))
                            {
                                SerializeStateToText(kvp.Value, kvp.Value.GetType(), sb, indent + 2);
                            }
                            else
                            {
                                sb.AppendLine($"{indentStr}    {kvp.Value}");
                            }
                        }
                    }
                }
                else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(ArraySchema<>))
                {
                    var enumerable = fieldValue as IEnumerable;
                    var countProp = fieldType.GetProperty("Count");
                    var count = countProp?.GetValue(fieldValue) as int? ?? 0;
                    sb.AppendLine($"{indentStr}{field.Name} (ArraySchema): {count} items");

                    if (enumerable != null && count > 0)
                    {
                        var index = 0;
                        foreach (var item in enumerable)
                        {
                            sb.AppendLine($"{indentStr}  [{index}]:");
                            if (item != null && typeof(Schema.Schema).IsAssignableFrom(item.GetType()))
                            {
                                SerializeStateToText(item, item.GetType(), sb, indent + 2);
                            }
                            else
                            {
                                sb.AppendLine($"{indentStr}    {item}");
                            }
                            index++;
                        }
                    }
                }
                else if (typeof(Schema.Schema).IsAssignableFrom(fieldType))
                {
                    sb.AppendLine($"{indentStr}{field.Name} ({fieldType.Name}):");
                    if (fieldValue != null)
                    {
                        SerializeStateToText(fieldValue, fieldType, sb, indent + 1);
                    }
                    else
                    {
                        sb.AppendLine($"{indentStr}  null");
                    }
                }
            }
        }

        private void DrawRoomInspector(RoomInfo roomInfo)
        {
            var foldoutKey = $"room_{roomInfo.RoomId}";
            if (!_foldouts.ContainsKey(foldoutKey))
            {
                _foldouts[foldoutKey] = true;
            }

            var headerStyle = new GUIStyle(EditorStyles.foldoutHeader);
            _foldouts[foldoutKey] = EditorGUILayout.BeginFoldoutHeaderGroup(
                _foldouts[foldoutKey],
                $"Room: {roomInfo.Name ?? "Unnamed"} ({roomInfo.RoomId})",
                headerStyle
            );

            if (_foldouts[foldoutKey])
            {
                EditorGUI.indentLevel++;

                // Room Information Section
                DrawSection("Connection Info", () =>
                {
                    DrawReadOnlyField("Room ID", roomInfo.RoomId ?? "N/A");
                    DrawReadOnlyField("Session ID", roomInfo.SessionId ?? "N/A");
                    DrawReadOnlyField("Connection", roomInfo.IsConnected ? "Connected" : "Disconnected");
                    DrawReadOnlyObjectField("Source Object", roomInfo.SourceObject, typeof(MonoBehaviour));
                });

                EditorGUILayout.Space(5);

                // State Section
                if (roomInfo.State != null)
                {
                    DrawSection("Room State", () =>
                    {
                        DrawReadOnlyField("State Type", roomInfo.StateType?.Name ?? "Unknown");
                        EditorGUILayout.Space(3);
                        DrawSchemaObject(roomInfo.State, roomInfo.StateType, "state");
                    });
                }
                else
                {
                    EditorGUILayout.HelpBox("State is null or not yet initialized", MessageType.Warning);
                }

                EditorGUILayout.Space(5);

                // Messages Section
                DrawMessagesSection(roomInfo);

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            // Draw separator line
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        }

        private void DrawSection(string title, Action content)
        {
            var foldoutKey = $"section_{title}_{EditorGUI.indentLevel}";
            if (!_foldouts.ContainsKey(foldoutKey))
            {
                _foldouts[foldoutKey] = true;
            }

            _foldouts[foldoutKey] = EditorGUILayout.Foldout(_foldouts[foldoutKey], title, true, EditorStyles.foldoutHeader);

            if (_foldouts[foldoutKey])
            {
                EditorGUI.indentLevel++;
                content?.Invoke();
                EditorGUI.indentLevel--;
            }
        }

        private void DrawSchemaObject(object obj, System.Type type, string path, int depth = 0)
        {
            if (obj == null || depth > 10) // Prevent infinite recursion
            {
                EditorGUILayout.LabelField("null", EditorStyles.miniLabel);
                return;
            }

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => f.GetCustomAttribute<Colyseus.Schema.Type>() != null)
                .OrderBy(f => f.GetCustomAttribute<Colyseus.Schema.Type>().Index)
                .ToList();

            foreach (var field in fields)
            {
                var fieldValue = field.GetValue(obj);
                var fieldType = field.FieldType;
                var fieldPath = $"{path}.{field.Name}";

                // Handle different field types
                if (IsPrimitiveOrString(fieldType))
                {
                    DrawReadOnlyField(field.Name, fieldValue?.ToString() ?? "null");
                }
                else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(MapSchema<>))
                {
                    DrawMapSchema(field.Name, fieldValue, fieldPath, depth);
                }
                else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(ArraySchema<>))
                {
                    DrawArraySchema(field.Name, fieldValue, fieldPath, depth);
                }
                else if (typeof(Schema.Schema).IsAssignableFrom(fieldType))
                {
                    DrawNestedSchema(field.Name, fieldValue, fieldType, fieldPath, depth);
                }
                else
                {
                    DrawReadOnlyField(field.Name, $"<{fieldType.Name}>");
                }
            }
        }

        private void DrawMapSchema(string fieldName, object mapObj, string path, int depth)
        {
            var foldoutKey = $"map_{path}";
            if (!_foldouts.ContainsKey(foldoutKey))
            {
                _foldouts[foldoutKey] = true; // Expand by default
            }

            if (mapObj == null)
            {
                DrawReadOnlyField(fieldName, "null (MapSchema)");
                return;
            }

            var mapType = mapObj.GetType();
            var countProp = mapType.GetProperty("Count");
            var count = countProp?.GetValue(mapObj) as int? ?? 0;

            _foldouts[foldoutKey] = EditorGUILayout.Foldout(
                _foldouts[foldoutKey],
                $"{fieldName} (MapSchema) [{count} items]",
                true
            );

            if (_foldouts[foldoutKey])
            {
                EditorGUI.indentLevel++;

                if (count == 0)
                {
                    EditorGUILayout.LabelField("(empty)", EditorStyles.miniLabel);
                }
                else
                {
                    // Access the items property of MapSchema
                    var itemsProperty = mapType.GetField("items");
                    var itemsValue = itemsProperty?.GetValue(mapObj);
                    var enumerable = itemsValue as IDictionary;
                    
                    if (enumerable != null)
                    {
                        var index = 0;
                        foreach (DictionaryEntry kvp in enumerable)
                        {
                            var key = kvp.Key?.ToString() ?? "null";
                            var value = kvp.Value;
                            var itemPath = $"{path}[{key}]";

                            if (value != null && typeof(Schema.Schema).IsAssignableFrom(value.GetType()))
                            {
                                DrawNestedSchema($"[{key}]", value, value.GetType(), itemPath, depth + 1);
                            }
                            else
                            {
                                DrawReadOnlyField($"[{key}]", value?.ToString() ?? "null");
                            }

                            index++;
                            if (index > 100) // Limit display to prevent performance issues
                            {
                                EditorGUILayout.LabelField($"... and {count - 100} more items", EditorStyles.miniLabel);
                                break;
                            }
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"Error: Could not access MapSchema items", EditorStyles.miniLabel);
                    }
                }

                EditorGUI.indentLevel--;
            }
        }

        private void DrawArraySchema(string fieldName, object arrayObj, string path, int depth)
        {
            var foldoutKey = $"array_{path}";
            if (!_foldouts.ContainsKey(foldoutKey))
            {
                _foldouts[foldoutKey] = true; // Expand by default
            }

            if (arrayObj == null)
            {
                DrawReadOnlyField(fieldName, "null (ArraySchema)");
                return;
            }

            var arrayType = arrayObj.GetType();
            var countProp = arrayType.GetProperty("Count");
            var count = countProp?.GetValue(arrayObj) as int? ?? 0;

            _foldouts[foldoutKey] = EditorGUILayout.Foldout(
                _foldouts[foldoutKey],
                $"{fieldName} (ArraySchema) [{count} items]",
                true
            );

            if (_foldouts[foldoutKey])
            {
                EditorGUI.indentLevel++;

                if (count == 0)
                {
                    EditorGUILayout.LabelField("(empty)", EditorStyles.miniLabel);
                }
                else
                {
                    var enumerable = arrayObj as IEnumerable;
                    if (enumerable != null)
                    {
                        var index = 0;
                        foreach (var item in enumerable)
                        {
                            var itemPath = $"{path}[{index}]";

                            if (item != null && typeof(Schema.Schema).IsAssignableFrom(item.GetType()))
                            {
                                DrawNestedSchema($"[{index}]", item, item.GetType(), itemPath, depth + 1);
                            }
                            else
                            {
                                DrawReadOnlyField($"[{index}]", item?.ToString() ?? "null");
                            }

                            index++;
                            if (index > 100) // Limit display to prevent performance issues
                            {
                                EditorGUILayout.LabelField($"... and {count - 100} more items", EditorStyles.miniLabel);
                                break;
                            }
                        }
                    }
                }

                EditorGUI.indentLevel--;
            }
        }

        private void DrawNestedSchema(string fieldName, object schemaObj, System.Type schemaType, string path, int depth)
        {
            var foldoutKey = $"schema_{path}";
            if (!_foldouts.ContainsKey(foldoutKey))
            {
                _foldouts[foldoutKey] = false; // Collapsed by default for nested schemas
            }

            if (schemaObj == null)
            {
                DrawReadOnlyField(fieldName, $"null ({schemaType.Name})");
                return;
            }

            _foldouts[foldoutKey] = EditorGUILayout.Foldout(
                _foldouts[foldoutKey],
                $"{fieldName} ({schemaType.Name})",
                true
            );

            if (_foldouts[foldoutKey])
            {
                EditorGUI.indentLevel++;
                DrawSchemaObject(schemaObj, schemaType, path, depth + 1);
                EditorGUI.indentLevel--;
            }
        }

        private void DrawReadOnlyField(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(EditorGUIUtility.labelWidth - 20));
            EditorGUILayout.SelectableLabel(value, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawReadOnlyObjectField(string label, UnityEngine.Object obj, System.Type type)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(EditorGUIUtility.labelWidth - 20));
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(obj, type, true);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        }

        private bool IsPrimitiveOrString(System.Type type)
        {
            return type.IsPrimitive || type == typeof(string) || type == typeof(decimal) ||
                   type.IsEnum || type == typeof(DateTime);
        }

        private List<RoomInfo> FindAllColyseusRooms()
        {
            var roomInfos = new List<RoomInfo>();
            var processedRooms = new HashSet<object>(); // Avoid duplicates

            if (!Application.isPlaying)
            {
                return roomInfos;
            }

            // Find all MonoBehaviours in the scene
            var allMonoBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

            foreach (var behaviour in allMonoBehaviours)
            {
                if (behaviour == null) continue;

                var behaviourType = behaviour.GetType();

                // Check instance fields
                var instanceFields = behaviourType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var field in instanceFields)
                {
                    if (IsColyseusRoomType(field.FieldType))
                    {
                        var roomObj = field.GetValue(behaviour);
                        if (roomObj != null && !processedRooms.Contains(roomObj))
                        {
                            processedRooms.Add(roomObj);
                            var roomInfo = ExtractRoomInfo(roomObj, field.FieldType, behaviour);
                            if (roomInfo != null)
                            {
                                roomInfos.Add(roomInfo);
                            }
                        }
                    }
                }

                // Check static fields
                var staticFields = behaviourType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                foreach (var field in staticFields)
                {
                    if (IsColyseusRoomType(field.FieldType))
                    {
                        var roomObj = field.GetValue(null); // null for static fields
                        if (roomObj != null && !processedRooms.Contains(roomObj))
                        {
                            processedRooms.Add(roomObj);
                            var roomInfo = ExtractRoomInfo(roomObj, field.FieldType, behaviour);
                            if (roomInfo != null)
                            {
                                roomInfos.Add(roomInfo);
                            }
                        }
                    }
                }
            }

            return roomInfos;
        }

        private bool IsColyseusRoomType(System.Type type)
        {
            if (type == null) return false;

            // Check if it's a generic ColyseusRoom<T>
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ColyseusRoom<>))
            {
                return true;
            }

            // Check if it implements IColyseusRoom
            return typeof(IColyseusRoom).IsAssignableFrom(type);
        }

        private RoomInfo ExtractRoomInfo(object room, System.Type roomType, MonoBehaviour source)
        {
            try
            {

                var roomInfo = new RoomInfo
                {
                    SourceObject = source,
                    RoomType = roomType,
                    RoomInstance = room
                };

                // Get RoomId (field)
                var roomIdField = roomType.GetField("RoomId");
                roomInfo.RoomId = roomIdField?.GetValue(room) as string;

                // Get SessionId (field)
                var sessionIdField = roomType.GetField("SessionId");
                roomInfo.SessionId = sessionIdField?.GetValue(room) as string;

                // Get Name (field)
                var nameField = roomType.GetField("Name");
                roomInfo.Name = nameField?.GetValue(room) as string;

                // Get Connection status (field)
                var connectionField = roomType.GetField("Connection");
                var connection = connectionField?.GetValue(room);
                if (connection != null)
                {
                    var isOpenProp = connection.GetType().GetField("IsOpen");
                    roomInfo.IsConnected = (isOpenProp?.GetValue(connection) as bool?) ?? false;
                }

                // Get State (property)
                var stateProp = roomType.GetProperty("State");
                roomInfo.State = stateProp?.GetValue(room);

                if (roomInfo.State != null)
                {
                    roomInfo.StateType = roomInfo.State.GetType();
                }

                // Note: Room message type capture is now handled automatically
                // via compile-time checks in ColyseusRoom.OnMessage()

                return roomInfo;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to extract room info: {ex.Message}");
                return null;
            }
        }



        private void DrawMessagesSection(RoomInfo roomInfo)
        {
            var roomId = roomInfo.RoomId;
            if (string.IsNullOrEmpty(roomId))
            {
                return;
            }

            DrawSection("Messages", () =>
            {
                // Access message types from static capture
                if (!RoomMessageType.CapturedMessageTypes.ContainsKey(roomId) || 
                    RoomMessageType.CapturedMessageTypes[roomId] == null || 
                    RoomMessageType.CapturedMessageTypes[roomId].Count == 0)
                {
                    EditorGUILayout.HelpBox(
                        "No message types available.\n\n" +
                        "Waiting for '__playground_message_types' message from server.",
                        MessageType.Info
                    );
                    return;
                }

                if (!_messageInputs.ContainsKey(roomId))
                {
                    _messageInputs[roomId] = new Dictionary<string, string>();
                }

                if (!_selectedMessageTypeIndex.ContainsKey(roomId))
                {
                    _selectedMessageTypeIndex[roomId] = 0;
                }

                var messageTypes = RoomMessageType.CapturedMessageTypes[roomId];
                var messageInputs = _messageInputs[roomId];

                // Create array of message type names for popup
                var messageTypeNames = messageTypes.Keys.ToArray();
                
                if (messageTypeNames.Length == 0)
                {
                    EditorGUILayout.HelpBox("No message types available. Enable playground in the server to see message types here.", MessageType.Info);
                    return;
                }

                // Ensure index is valid
                if (_selectedMessageTypeIndex[roomId] >= messageTypeNames.Length)
                {
                    _selectedMessageTypeIndex[roomId] = 0;
                }

                // Draw message type selector
                EditorGUILayout.LabelField("Message Type:", EditorStyles.boldLabel);
                var previousIndex = _selectedMessageTypeIndex[roomId];
                _selectedMessageTypeIndex[roomId] = EditorGUILayout.Popup(
                    _selectedMessageTypeIndex[roomId],
                    messageTypeNames,
                    GUILayout.Height(20)
                );

                var selectedMessageName = messageTypeNames[_selectedMessageTypeIndex[roomId]];
                IDictionary messageSchema = messageTypes[selectedMessageName] as IDictionary;
                
                // Initialize raw JSON toggle state
                var rawJsonKey = $"{roomId}_{selectedMessageName}";
                if (!_useRawJson.ContainsKey(rawJsonKey))
                {
                    _useRawJson[rawJsonKey] = false;
                }
                
                // Initialize raw JSON input
                if (!_rawJsonInputs.ContainsKey(rawJsonKey))
                {
                    _rawJsonInputs[rawJsonKey] = messageSchema != null ? GenerateDefaultJSON(messageSchema) : "{}";
                }
                
                // Check if message type changed - update raw JSON when switching message types
                if (!_lastSelectedMessageType.ContainsKey(roomId) || 
                    _lastSelectedMessageType[roomId] != selectedMessageName)
                {
                    _lastSelectedMessageType[roomId] = selectedMessageName;
                    _rawJsonInputs[rawJsonKey] = messageSchema != null ? GenerateDefaultJSON(messageSchema) : "{}";
                    
                    // Clear all field inputs for this message type
                    var keysToRemove = messageInputs.Keys.Where(k => k.StartsWith($"{roomId}_{selectedMessageName}_field_")).ToList();
                    foreach (var key in keysToRemove)
                    {
                        messageInputs.Remove(key);
                    }
                }

                // Toggle between form fields and raw JSON
                var previousUseRawJson = _useRawJson[rawJsonKey];

                // Only show toggle if schema is available
                if (messageSchema != null)
                {
                    EditorGUILayout.Space(4);
                    _useRawJson[rawJsonKey] = EditorGUILayout.Toggle("Use Raw JSON", _useRawJson[rawJsonKey]);
                }
                
                // When switching to raw JSON mode, populate from field values
                if (_useRawJson[rawJsonKey] && !previousUseRawJson && messageSchema != null)
                {
                    _rawJsonInputs[rawJsonKey] = GenerateJSONFromFields(roomId, selectedMessageName, messageSchema, messageInputs);
                }
                
                // If raw JSON mode is enabled or schema is invalid, show JSON text area
                if (_useRawJson[rawJsonKey] || messageSchema == null)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("JSON Payload:", EditorStyles.boldLabel);
                    
                    // Multi-line text area for JSON input
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    var textAreaStyle = new GUIStyle(EditorStyles.textArea);
                    textAreaStyle.wordWrap = true;
                    textAreaStyle.stretchHeight = true;
                    
                    _rawJsonInputs[rawJsonKey] = EditorGUILayout.TextArea(
                        _rawJsonInputs[rawJsonKey], 
                        textAreaStyle, 
                        GUILayout.MinHeight(40),
                        GUILayout.MaxHeight(80)
                    );
                    EditorGUILayout.EndVertical();
                    
                    EditorGUILayout.Space(5);

                    // Send button for raw JSON
                    if (GUILayout.Button($"Send '{selectedMessageName}'", GUILayout.Height(30)))
                    {
                        SendMessageFromRawJson(roomInfo, selectedMessageName, _rawJsonInputs[rawJsonKey]);
                    }
                    
                    return;
                }

                // Get required fields
                List<object> requiredFields = null;
                if (messageSchema.Contains("required"))
                {
                    requiredFields = messageSchema["required"] as List<object>;
                }

                // Draw individual field inputs
                if (messageSchema.Contains("properties"))
                {
                    var properties = messageSchema["properties"] as IDictionary;
                    if (properties != null && properties.Count > 0)
                    {
                        EditorGUILayout.LabelField("Payload:", EditorStyles.boldLabel);
                        EditorGUI.indentLevel++;

                        foreach (DictionaryEntry prop in properties)
                        {
                            var propName = prop.Key as string;
                            var propSchema = prop.Value as IDictionary;
                            
                            if (propSchema != null && propSchema.Contains("type"))
                            {
                                var isRequired = requiredFields != null && requiredFields.Any(r => r.ToString() == propName);
                                DrawMessageField(roomId, selectedMessageName, propName, propSchema, isRequired, messageInputs);
                            }
                        }

                        EditorGUI.indentLevel--;
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("No fields defined for this message type.", MessageType.Info);
                }

                EditorGUILayout.Space(5);

                // Send button
                if (GUILayout.Button($"Send '{selectedMessageName}'", GUILayout.Height(30)))
                {
                    SendMessageFromFields(roomInfo, selectedMessageName, messageSchema, messageInputs);
                }
            });
        }

        private void DrawMessageField(string roomId, string messageName, string fieldName, IDictionary fieldSchema, bool isRequired, Dictionary<string, string> messageInputs)
        {
            var fieldKey = $"{roomId}_{messageName}_field_{fieldName}";
            var fieldType = fieldSchema["type"] as string;
            
            // Initialize with default value if not exists
            if (!messageInputs.ContainsKey(fieldKey))
            {
                messageInputs[fieldKey] = GetDefaultValueForType(fieldSchema);
            }

            // Create label with asterisk for required fields
            var label = isRequired ? $"{fieldName} *" : fieldName;
            
            // Add description as tooltip if available
            GUIContent labelContent;
            if (fieldSchema.Contains("description"))
            {
                var description = fieldSchema["description"].ToString();
                labelContent = new GUIContent(label, description);
            }
            else
            {
                labelContent = new GUIContent(label);
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(labelContent, GUILayout.Width(EditorGUIUtility.labelWidth - 20));

            // Draw appropriate input control based on field type
            switch (fieldType)
            {
                case "boolean":
                    var boolValue = messageInputs[fieldKey] == "true";
                    boolValue = EditorGUILayout.Toggle(boolValue);
                    messageInputs[fieldKey] = boolValue.ToString().ToLower();
                    break;

                case "integer":
                    if (int.TryParse(messageInputs[fieldKey], out int intValue))
                    {
                        intValue = EditorGUILayout.IntField(intValue);
                        messageInputs[fieldKey] = intValue.ToString();
                    }
                    else
                    {
                        messageInputs[fieldKey] = EditorGUILayout.TextField(messageInputs[fieldKey]);
                    }
                    break;

                case "number":
                    if (float.TryParse(messageInputs[fieldKey], out float floatValue))
                    {
                        floatValue = EditorGUILayout.FloatField(floatValue);
                        messageInputs[fieldKey] = floatValue.ToString();
                    }
                    else
                    {
                        messageInputs[fieldKey] = EditorGUILayout.TextField(messageInputs[fieldKey]);
                    }
                    break;

                case "string":
                    // Remove quotes if present for display
                    var stringValue = messageInputs[fieldKey];
                    if (stringValue.StartsWith("\"") && stringValue.EndsWith("\""))
                    {
                        stringValue = stringValue.Substring(1, stringValue.Length - 2);
                    }
                    stringValue = EditorGUILayout.TextField(stringValue);
                    messageInputs[fieldKey] = stringValue;
                    break;

                case "array":
                case "object":
                    // For complex types, use a text field with JSON
                    EditorGUILayout.LabelField($"({fieldType})", GUILayout.Width(60));
                    messageInputs[fieldKey] = EditorGUILayout.TextField(messageInputs[fieldKey]);
                    break;

                default:
                    messageInputs[fieldKey] = EditorGUILayout.TextField(messageInputs[fieldKey]);
                    break;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void SendMessageFromFields(RoomInfo roomInfo, string messageName, IDictionary messageSchema, Dictionary<string, string> messageInputs)
        {
            try
            {
                var roomId = roomInfo.RoomId;
                var messageData = new Dictionary<string, object>();

                // Build message object from field values
                if (messageSchema.Contains("properties"))
                {
                    var properties = messageSchema["properties"] as IDictionary;
                    if (properties != null)
                    {
                        foreach (DictionaryEntry prop in properties)
                        {
                            var propName = prop.Key as string;
                            var propSchema = prop.Value as IDictionary;
                            if (propSchema == null || !propSchema.Contains("type")) continue;

                            var fieldKey = $"{roomId}_{messageName}_field_{propName}";
                            if (!messageInputs.ContainsKey(fieldKey)) continue;

                            var fieldValue = messageInputs[fieldKey];
                            var fieldType = propSchema["type"] as string;

                            // Convert field value to appropriate type
                            object typedValue = ConvertFieldValue(fieldValue, fieldType);
                            messageData[propName] = typedValue;
                        }
                    }
                }

                // Get the Send method from the room
                var sendMethod = roomInfo.RoomType.GetMethod("Send", new[] { typeof(string), typeof(object) });
                if (sendMethod == null)
                {
                    EditorUtility.DisplayDialog("Error", "Failed to find Send method on room.", "OK");
                    return;
                }

                // Invoke Send method
                var task = sendMethod.Invoke(roomInfo.RoomInstance, new object[] { messageName, messageData }) as System.Threading.Tasks.Task;
                if (task != null)
                {
                    // Note: We can't await in a non-async void method, but the task will execute
                    Debug.Log($"Message '{messageName}' sent");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to send message '{messageName}': {ex.Message}\n{ex.StackTrace}");
                EditorUtility.DisplayDialog("Error", $"Failed to send message:\n{ex.Message}", "OK");
            }
        }

        private void SendMessageFromRawJson(RoomInfo roomInfo, string messageName, string jsonData)
        {
            try
            {
                // Parse JSON to object
                object messageData;
                try
                {
                    messageData = Json.Deserialize(typeof(Dictionary<string, object>), jsonData);
                }
                catch (Exception jsonEx)
                {
                    EditorUtility.DisplayDialog("JSON Parse Error", $"Failed to parse JSON:\n{jsonEx.Message}", "OK");
                    Debug.LogError($"JSON parse error: {jsonEx.Message}\n{jsonData}");
                    return;
                }

                // Get the Send method from the room
                var sendMethod = roomInfo.RoomType.GetMethod("Send", new[] { typeof(string), typeof(object) });
                if (sendMethod == null)
                {
                    EditorUtility.DisplayDialog("Error", "Failed to find Send method on room.", "OK");
                    return;
                }

                // Invoke Send method
                var task = sendMethod.Invoke(roomInfo.RoomInstance, new object[] { messageName, messageData }) as System.Threading.Tasks.Task;
                if (task != null)
                {
                    Debug.Log($"Message '{messageName}' sent with raw JSON");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to send message '{messageName}': {ex.Message}\n{ex.StackTrace}");
                EditorUtility.DisplayDialog("Error", $"Failed to send message:\n{ex.Message}", "OK");
            }
        }

        private object ConvertFieldValue(string fieldValue, string fieldType)
        {
            switch (fieldType)
            {
                case "boolean":
                    return fieldValue == "true";

                case "integer":
                    if (int.TryParse(fieldValue, out int intValue))
                        return intValue;
                    return 0;

                case "number":
                    if (double.TryParse(fieldValue, out double doubleValue))
                        return doubleValue;
                    return 0.0;

                case "string":
                    return fieldValue;

                case "array":
                    try
                    {
                        return Json.Deserialize(typeof(List<object>), fieldValue);
                    }
                    catch
                    {
                        return new List<object>();
                    }

                case "object":
                    try
                    {
                        return Json.Deserialize(typeof(Dictionary<string, object>), fieldValue);
                    }
                    catch
                    {
                        return new Dictionary<string, object>();
                    }

                default:
                    return fieldValue;
            }
        }

        private string GenerateDefaultJSON(IDictionary schema)
        {
            try
            {
                if (!schema.Contains("properties"))
                {
                    return "{}";
                }

                var properties = schema["properties"] as IDictionary;
                if (properties == null || properties.Count == 0)
                {
                    return "{}";
                }

                var sb = new StringBuilder();
                sb.AppendLine("{");

                var propCount = 0;
                foreach (DictionaryEntry prop in properties)
                {
                    var propName = prop.Key as string;
                    var propSchema = prop.Value as IDictionary;
                    if (propSchema == null) continue;

                    sb.Append($"  \"{propName}\": ");

                    var defaultValue = GetDefaultValueForType(propSchema);
                    sb.Append(defaultValue);

                    propCount++;
                    if (propCount < properties.Count)
                    {
                        sb.AppendLine(",");
                    }
                    else
                    {
                        sb.AppendLine();
                    }
                }

                sb.Append("}");
                return sb.ToString();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to generate default JSON: {ex.Message}");
                return "{}";
            }
        }

        private string GetDefaultValueForType(IDictionary propSchema)
        {
            if (!propSchema.Contains("type"))
            {
                return "null";
            }

            var type = propSchema["type"] as string;
            switch (type)
            {
                case "string":
                    return "\"\"";
                case "number":
                case "integer":
                    return "0";
                case "boolean":
                    return "false";
                case "array":
                    return "[]";
                case "object":
                    return "{}";
                default:
                    return "null";
            }
        }

        private string GenerateJSONFromFields(string roomId, string messageName, IDictionary messageSchema, Dictionary<string, string> messageInputs)
        {
            try
            {
                if (!messageSchema.Contains("properties"))
                {
                    return "{}";
                }

                var properties = messageSchema["properties"] as IDictionary;
                if (properties == null || properties.Count == 0)
                {
                    return "{}";
                }

                var sb = new StringBuilder();
                sb.AppendLine("{");

                var propCount = 0;
                var totalProps = properties.Count;
                
                foreach (DictionaryEntry prop in properties)
                {
                    var propName = prop.Key as string;
                    var propSchema = prop.Value as IDictionary;
                    if (propSchema == null || !propSchema.Contains("type")) continue;

                    var fieldKey = $"{roomId}_{messageName}_field_{propName}";
                    var fieldType = propSchema["type"] as string;
                    
                    sb.Append($"  \"{propName}\": ");

                    // Get value from field input or use default
                    string jsonValue;
                    if (messageInputs.ContainsKey(fieldKey))
                    {
                        var fieldValue = messageInputs[fieldKey];
                        jsonValue = ConvertFieldValueToJSON(fieldValue, fieldType);
                    }
                    else
                    {
                        jsonValue = GetDefaultValueForType(propSchema);
                    }
                    
                    sb.Append(jsonValue);

                    propCount++;
                    if (propCount < totalProps)
                    {
                        sb.AppendLine(",");
                    }
                    else
                    {
                        sb.AppendLine();
                    }
                }

                sb.Append("}");
                return sb.ToString();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to generate JSON from fields: {ex.Message}");
                return GenerateDefaultJSON(messageSchema);
            }
        }

        private string ConvertFieldValueToJSON(string fieldValue, string fieldType)
        {
            switch (fieldType)
            {
                case "boolean":
                    return fieldValue == "true" ? "true" : "false";

                case "integer":
                    if (int.TryParse(fieldValue, out int intValue))
                        return intValue.ToString();
                    return "0";

                case "number":
                    if (double.TryParse(fieldValue, out double doubleValue))
                        return doubleValue.ToString();
                    return "0";

                case "string":
                    // Escape quotes and wrap in quotes
                    var escaped = fieldValue.Replace("\\", "\\\\").Replace("\"", "\\\"");
                    return $"\"{ escaped}\"";

                case "array":
                case "object":
                    // Try to parse as JSON, otherwise treat as string
                    try
                    {
                        var parsed = Json.Deserialize(typeof(object), fieldValue);
                        return fieldValue; // If it parses, use as-is
                    }
                    catch
                    {
                        return fieldValue.StartsWith("[") || fieldValue.StartsWith("{") ? fieldValue : "{}";
                    }

                default:
                    return $"\"{fieldValue}\"";
            }
        }

        private async void SendMessage(RoomInfo roomInfo, string messageName, string jsonData)
        {
            try
            {
                // Parse JSON to object
                var messageData = ParseJSON(jsonData);

                // Get the Send method from the room
                var sendMethod = roomInfo.RoomType.GetMethod("Send", new[] { typeof(string), typeof(object) });
                if (sendMethod == null)
                {
                    EditorUtility.DisplayDialog("Error", "Failed to find Send method on room.", "OK");
                    return;
                }

                // Invoke Send method
                var task = sendMethod.Invoke(roomInfo.RoomInstance, new object[] { messageName, messageData }) as System.Threading.Tasks.Task;
                if (task != null)
                {
                    await task;
                    Debug.Log($"Message '{messageName}' sent successfully!");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to send message '{messageName}': {ex.Message}\n{ex.StackTrace}");
                EditorUtility.DisplayDialog("Error", $"Failed to send message:\n{ex.Message}", "OK");
            }
        }

        private object ParseJSON(string json)
        {
            // Parse JSON using GameDevWare.Serialization.Json
            try
            {
                return Json.Deserialize(typeof(Dictionary<string, object>), json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to parse JSON: {ex.Message}");
                throw;
            }
        }

        private void DrawSchemaInfo(IDictionary schema)
        {
            if (schema == null)
            {
                EditorGUILayout.LabelField("No schema information available", EditorStyles.miniLabel);
                return;
            }

            // Display schema type
            if (schema.Contains("type"))
            {
                DrawReadOnlyField("Type", schema["type"].ToString());
            }

            // Display properties
            if (schema.Contains("properties"))
            {
                var properties = schema["properties"] as IDictionary;
                if (properties != null && properties.Count > 0)
                {
                    EditorGUILayout.LabelField("Properties:", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    
                    foreach (DictionaryEntry prop in properties)
                    {
                        var propName = prop.Key as string;
                        var propSchema = prop.Value as IDictionary;
                        
                        if (propSchema != null && propSchema.Contains("type"))
                        {
                            var propType = propSchema["type"].ToString();
                            var propInfo = propType;
                            
                            // Add description if available
                            if (propSchema.Contains("description"))
                            {
                                propInfo += $" - {propSchema["description"]}";
                            }
                            
                            DrawReadOnlyField(propName, propInfo);
                        }
                    }
                    
                    EditorGUI.indentLevel--;
                }
            }

            // Display required fields
            if (schema.Contains("required"))
            {
                var required = schema["required"] as List<object>;
                if (required != null && required.Count > 0)
                {
                    var requiredStr = string.Join(", ", required.Select(r => r.ToString()));
                    DrawReadOnlyField("Required", requiredStr);
                }
            }
        }

        private class RoomInfo
        {
            public string RoomId;
            public string SessionId;
            public string Name;
            public bool IsConnected;
            public object State;
            public System.Type StateType;
            public System.Type RoomType;
            public MonoBehaviour SourceObject;
            public object RoomInstance;
        }
    }
}

