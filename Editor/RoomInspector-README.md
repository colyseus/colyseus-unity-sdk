# Colyseus Room Inspector

The Colyseus Room Inspector is a Unity Editor tool that allows you to inspect and monitor your connected Colyseus room states in real-time while your game is running in Play mode.

## Features

- **Real-time State Inspection**: View all room state properties and their current values
- **Auto-refresh**: Automatically updates the display every 0.5 seconds
- **Multiple Room Support**: Displays all active Colyseus rooms in your scene
- **Schema Visualization**: Recursively displays nested Schema objects, MapSchema, and ArraySchema
- **Connection Information**: Shows Room ID, Session ID, and connection status
- **Collapsible Sections**: Organize information with foldout groups for better readability

## How to Use

### Opening the Inspector

1. Start Unity Editor
2. Go to **Window → Colyseus → Room Inspector**
3. The Room Inspector window will open

### Using the Inspector

1. **Enter Play Mode**: The inspector only works when your game is running
2. **Connect to a Room**: Make sure your game connects to a Colyseus server
3. **View State**: The inspector will automatically detect and display any active rooms

### Interface Elements

#### Toolbar
- **Auto Refresh Toggle**: Enable/disable automatic updates (enabled by default)
- **Refresh Now Button**: Manually refresh the display
- **Copy State JSON Button**: Copy the current room state to clipboard as formatted text

#### Room Information
Each connected room displays:
- **Room Name and ID**: Identifies the room
- **Connection Info Section**:
  - Room ID
  - Session ID
  - Room Name
  - Connection status
  - Source GameObject

- **Room State Section**:
  - State type name
  - All state properties with current values
  - Nested Schema objects (expandable)
  - MapSchema collections with item counts
  - ArraySchema collections with item counts

### Supported State Types

The inspector can display:
- **Primitive Types**: numbers, strings, booleans
- **Schema Objects**: Nested state objects
- **MapSchema**: Dictionary-like collections
- **ArraySchema**: Array-like collections

### Performance Considerations

- Collections (MapSchema/ArraySchema) are limited to displaying 100 items
- State inspection depth is limited to 10 levels to prevent infinite recursion
- The auto-refresh interval is set to 0.5 seconds to balance responsiveness and performance

## Example

Given this room state:

```csharp
public class MyRoomState : Schema
{
    [Type(0, "map", typeof(MapSchema<Player>))]
    public MapSchema<Player> players = new MapSchema<Player>();
    
    [Type(1, "number")]
    public float gameTime = 0f;
}

public class Player : Schema
{
    [Type(0, "number")]
    public float x = 0f;
    
    [Type(1, "number")]
    public float y = 0f;
    
    [Type(2, "string")]
    public string name = "";
}
```

The Room Inspector will display:
```
Room: my_room (abc123)
  ├─ Connection Info
  │   ├─ Room ID: abc123
  │   ├─ Session ID: xyz789
  │   ├─ Connection: Connected
  │   └─ Source Object: NetworkManager
  └─ Room State
      ├─ State Type: MyRoomState
      ├─ players (MapSchema) [2 items]
      │   ├─ [player1] (Player)
      │   │   ├─ x: 10.5
      │   │   ├─ y: 20.3
      │   │   └─ name: "Alice"
      │   └─ [player2] (Player)
      │       ├─ x: 15.2
      │       ├─ y: 18.7
      │       └─ name: "Bob"
      └─ gameTime: 45.2
```

## Troubleshooting

### "No active Colyseus rooms found"

This message appears when:
- Not in Play mode
- No MonoBehaviour has a ColyseusRoom field
- The room hasn't connected yet

**Solution**: Make sure you're in Play mode and your game has successfully connected to a Colyseus server.

### State shows as "null"

This happens when:
- The room is connected but hasn't received the initial state yet
- There's an issue with state synchronization

**Solution**: Wait a moment for the initial state to arrive, or check your server-side room implementation.

### Values not updating

- Check that "Auto Refresh" is enabled in the toolbar
- Try clicking "Refresh Now" to manually update
- Verify your room is still connected

## Technical Details

The Room Inspector works by:
1. Finding all MonoBehaviour components in the active scene
2. Using reflection to discover fields of type `ColyseusRoom<T>`
3. Extracting room properties and state using reflection
4. Recursively traversing Schema objects to display their structure
5. Updating the display periodically via `EditorApplication.update`

## Requirements

- Unity Editor (any version supporting the Colyseus SDK)
- Colyseus Unity SDK
- Active scene in Play mode
- Connected Colyseus room

## Notes

- The inspector is read-only and does not allow editing values
- Values are displayed as selectable text fields for easy copying
- The tool is designed for debugging and development purposes

