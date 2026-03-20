# Colyseus Room Inspector

Unity Editor tool for inspecting connected Colyseus room states in real-time during Play mode.

## Usage

1. Open **Window > Colyseus > Room Inspector**
2. Enter Play mode and connect to a Colyseus server
3. Active rooms are automatically discovered and displayed

### Toolbar

| Button | Description |
|--------|-------------|
| **Auto Refresh** | Toggle automatic updates (every 0.5s) |
| **Refresh Now** | Manually refresh the display |
| **Copy State JSON** | Copy current state to clipboard |

### Example Output

Given a room with `MapSchema<Player> players` and `float gameTime`:

```
Room: my_room (abc123)
  +-- Connection Info
  |   +-- Room ID: abc123
  |   +-- Session ID: xyz789
  |   +-- Connection: Connected
  |   +-- Source Object: NetworkManager
  +-- Room State
      +-- State Type: MyRoomState
      +-- players (MapSchema) [2 items]
      |   +-- [player1] (Player)
      |   |   +-- x: 10.5
      |   |   +-- y: 20.3
      |   |   +-- name: "Alice"
      |   +-- [player2] (Player)
      |       +-- x: 15.2
      |       +-- y: 18.7
      |       +-- name: "Bob"
      +-- gameTime: 45.2
```

## Supported Types

- Primitives (int, float, string, bool)
- Nested Schema objects
- `MapSchema<T>` and `ArraySchema<T>` collections

## Limitations

- **Play mode only** -- not available in Edit mode
- **Read-only** -- cannot edit state values
- Collections limited to **100 items** displayed
- Nesting limited to **10 levels** deep

## Troubleshooting

**"No active Colyseus rooms found"** -- Ensure you are in Play mode and have connected to a room. The inspector discovers rooms by scanning MonoBehaviour fields via reflection.

**State shows as "null"** -- The room is connected but hasn't received the initial state yet. Wait a moment or check your server-side room.

**Values not updating** -- Check that Auto Refresh is enabled in the toolbar and that the room is still connected.
