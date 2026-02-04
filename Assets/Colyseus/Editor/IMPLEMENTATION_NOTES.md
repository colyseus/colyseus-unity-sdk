# Colyseus Room Inspector - Implementation Notes

## Overview

The Colyseus Room Inspector is a Unity Editor window that provides real-time inspection of connected Colyseus room states during Play mode. This tool was designed to aid developers in debugging and monitoring their multiplayer game state.

## Architecture

### Core Components

1. **ColyseusRoomInspector.cs** - Main Editor window class
   - Extends `EditorWindow`
   - Uses Unity's `EditorApplication.update` for periodic refreshes
   - Implements reflection-based discovery and inspection

2. **Colyseus.Editor.asmdef** - Assembly definition
   - References the ColyseusSDK assembly
   - Editor-only platform restriction
   - Uses `ColyseusEditor` namespace

### Key Features Implemented

#### 1. Room Discovery
- **Reflection-based scanning**: Searches all MonoBehaviours in the scene
- **Instance and Static field support**: Handles both field types
- **Type checking**: Validates fields as `ColyseusRoom<T>` or `IColyseusRoom`
- **Duplicate prevention**: Uses HashSet to avoid showing the same room multiple times

```csharp
private List<RoomInfo> FindAllColyseusRooms()
{
    // Scans all MonoBehaviours for ColyseusRoom fields
    // Checks both instance and static fields
    // Returns list of discovered rooms
}
```

#### 2. State Inspection
- **Recursive traversal**: Walks through Schema objects up to 10 levels deep
- **Type-aware rendering**: Different display for primitives, Schema objects, collections
- **Attribute-based ordering**: Uses `[Type]` attribute's Index property
- **Collection support**:
  - MapSchema<T> - displayed as dictionaries
  - ArraySchema<T> - displayed as indexed lists

```csharp
private void DrawSchemaObject(object obj, Type type, string path, int depth = 0)
{
    // Recursively inspects and displays Schema properties
    // Handles primitives, nested schemas, and collections
}
```

#### 3. User Interface

**Toolbar**:
- Auto Refresh toggle (enabled by default)
- Manual Refresh button
- Copy State to Clipboard button

**Room Display**:
- Collapsible foldout groups
- Connection information section
- State inspection section
- Read-only selectable fields for easy copying

#### 4. Performance Optimizations

- **Refresh throttling**: Updates every 0.5 seconds when auto-refresh is enabled
- **Collection limits**: Shows max 100 items in MapSchema/ArraySchema
- **Depth limiting**: Prevents infinite recursion (max 10 levels)
- **Lazy evaluation**: Only computes values for expanded sections

### Technical Decisions

#### Why Reflection?

Reflection was chosen because:
1. **Type-agnostic**: Works with any Schema type without code generation
2. **Dynamic discovery**: Finds rooms without manual registration
3. **Schema inspection**: Accesses private/internal fields as needed
4. **Attribute reading**: Parses `[Type]` attributes for proper ordering

#### Why Editor Window?

EditorWindow provides:
1. **Persistent UI**: Stays open and updates continuously
2. **Dockable**: Can be arranged with other Unity windows
3. **Menu integration**: Easy access via Window menu
4. **Editor-only**: No runtime overhead

#### Why Not Custom Inspector?

Custom inspectors were not used because:
1. Rooms are not UnityEngine.Object instances
2. Need to inspect multiple rooms simultaneously
3. Want centralized monitoring tool
4. State is dynamic and needs continuous updates

### State Serialization

The "Copy State JSON" feature serializes room state to text format:
- Uses indentation for hierarchy
- Shows collection counts
- Recursively formats nested objects
- Includes timestamp and connection metadata

```csharp
private void SerializeStateToText(object obj, Type type, StringBuilder sb, int indent)
{
    // Converts Schema state to human-readable text
    // Handles all supported Schema types
}
```

## Usage Patterns

### Pattern 1: Instance Field
```csharp
public class GameManager : MonoBehaviour
{
    private ColyseusRoom<GameState> room;
    // Inspector finds this via instance field reflection
}
```

### Pattern 2: Static Field
```csharp
public class NetworkManager : MonoBehaviour
{
    private static ColyseusRoom<MyRoomState> _room;
    // Inspector finds this via static field reflection
}
```

### Pattern 3: Property with Backing Field
```csharp
public class RoomController : MonoBehaviour
{
    private ColyseusRoom<RoomState> _room;
    public ColyseusRoom<RoomState> Room => _room;
    // Inspector finds via the private backing field
}
```

## Testing

### Manual Testing Steps

1. **Setup**:
   - Open Unity project
   - Start Colyseus server (in Server folder)
   - Open example scene

2. **Test Discovery**:
   - Open Room Inspector window
   - Enter Play mode
   - Verify "No rooms found" message appears
   - Connect to room
   - Verify room appears in inspector

3. **Test State Display**:
   - Verify connection info shows correctly
   - Verify state fields are displayed
   - Test collapsing/expanding sections
   - Modify state on server
   - Verify state updates in inspector

4. **Test Features**:
   - Toggle Auto Refresh off/on
   - Click Refresh Now button
   - Click Copy State JSON button
   - Verify clipboard contains state data

### Known Limitations

1. **Play Mode Only**: Inspector only works when application is playing
2. **No State Modification**: Read-only, cannot edit values
3. **Collection Limit**: Large collections truncated at 100 items
4. **Recursion Limit**: Deep nesting stops at 10 levels
5. **No JSON Export**: Text format only, not true JSON

## Future Enhancements

Possible improvements:
1. **True JSON export**: Use JSON serializer for proper formatting
2. **State comparison**: Diff view showing state changes
3. **History tracking**: Record state changes over time
4. **Message inspection**: Show sent/received messages
5. **Performance metrics**: Display network stats
6. **Custom formatters**: Allow user-defined rendering for types
7. **Search/filter**: Find specific fields in large states
8. **Multi-room comparison**: Side-by-side state comparison
9. **Export to file**: Save state snapshots
10. **Context menu integration**: Right-click GameObject to inspect room

## File Structure

```
Assets/Colyseus/Editor/
├── ColyseusRoomInspector.cs     (Main window implementation)
├── Colyseus.Editor.asmdef        (Assembly definition)
├── RoomInspector-README.md       (User documentation)
└── IMPLEMENTATION_NOTES.md       (This file)
```

## Dependencies

- **Unity Editor**: Any version with EditorWindow support
- **ColyseusSDK**: Reference to runtime assembly
- **System.Reflection**: For type inspection
- **System.Linq**: For LINQ queries
- **System.Collections**: For IDictionary/IEnumerable

## Integration Points

The inspector integrates with:
1. **ColyseusRoom<T>**: Main room class
2. **IColyseusRoom**: Room interface
3. **Schema**: Base state class
4. **Type attribute**: Schema field metadata
5. **MapSchema<T>**: Dictionary collection
6. **ArraySchema<T>**: Array collection
7. **ColyseusConnection**: Connection status

## Code Quality

- **Namespace**: `ColyseusEditor` for clear separation
- **Documentation**: XML comments on public members
- **Error handling**: Try-catch blocks with logging
- **Null safety**: Defensive checks throughout
- **SOLID principles**: Single responsibility per method
- **Performance**: Optimized for editor refresh cycles

## Maintenance Notes

### When to Update

Update this tool when:
1. Schema class changes structure
2. ColyseusRoom API changes
3. New collection types added
4. Performance issues discovered
5. Unity editor API changes

### Common Issues

**Issue**: Rooms not appearing
- Check: Is game in Play mode?
- Check: Is room field accessible via reflection?
- Check: Is room actually instantiated?

**Issue**: State shows as null
- Check: Has initial state arrived from server?
- Check: Is serializer properly configured?

**Issue**: Values not updating
- Check: Is Auto Refresh enabled?
- Check: Is connection still active?
- Check: Are state changes actually happening?

## Version History

- **v1.0** (2025-11-28)
  - Initial implementation
  - Room discovery with reflection
  - State inspection with recursive rendering
  - Auto-refresh functionality
  - Copy to clipboard feature
  - Support for MapSchema and ArraySchema
  - Static and instance field support

## Credits

Developed as an enhancement to the Colyseus Unity SDK to improve developer experience and debugging capabilities.

