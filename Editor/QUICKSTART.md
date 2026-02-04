# Colyseus Room Inspector - Quick Start Guide

## 🚀 Getting Started in 3 Steps

### Step 1: Open the Inspector
In Unity Editor, go to:
```
Window → Colyseus → Room Inspector
```

### Step 2: Enter Play Mode
Press the Play button in Unity. The inspector window will remain open.

### Step 3: Connect to Your Room
Once your game connects to a Colyseus server, the room will automatically appear in the inspector!

---

## 📊 What You'll See

```
┌─────────────────────────────────────────┐
│  ☑ Auto Refresh   [Refresh Now]  [Copy]│
├─────────────────────────────────────────┤
│                                          │
│  ▼ Room: my_room (abc123)               │
│    ▼ Connection Info                    │
│      • Room ID: abc123                  │
│      • Session ID: xyz789               │
│      • Connection: Connected            │
│      • Source Object: NetworkManager    │
│                                          │
│    ▼ Room State                         │
│      • State Type: MyRoomState          │
│      ▼ players (MapSchema) [2 items]    │
│        ▼ [player1] (Player)             │
│          • x: 10.5                      │
│          • y: 20.3                      │
│        ▼ [player2] (Player)             │
│          • x: 15.2                      │
│          • y: 18.7                      │
│                                          │
└─────────────────────────────────────────┘
```

---

## 🎯 Common Use Cases

### Debug State Synchronization
Watch values update in real-time as players interact with your game.

### Copy State for Bug Reports
Click "Copy State JSON" to grab a snapshot of the current state, perfect for bug reports.

### Monitor Multiple Rooms
If your game uses multiple room connections, they'll all appear in the inspector.

### Inspect Collections
Easily browse MapSchema and ArraySchema collections with item counts and expandable entries.

---

## ⚙️ Toolbar Buttons

| Button | Function |
|--------|----------|
| **Auto Refresh** | Toggle automatic updates (every 0.5s) |
| **Refresh Now** | Manually update the display |
| **Copy State JSON** | Copy current state to clipboard |

---

## 💡 Pro Tips

1. **Dock the Window**: Drag the inspector to dock it alongside your Game view
2. **Selective Expansion**: Collapse sections you don't need to reduce clutter
3. **Copy Values**: Click any value to select it, then Ctrl+C to copy
4. **Monitor Changes**: Watch for values changing to identify sync issues
5. **Multiple Sessions**: Test with multiple clients and see all sessions

---

## 🔍 Supported Schema Types

- ✅ Primitives (int, float, string, bool)
- ✅ Nested Schema objects
- ✅ MapSchema<T> (dictionaries)
- ✅ ArraySchema<T> (lists)
- ✅ Up to 10 levels of nesting
- ✅ Up to 100 items per collection

---

## ❌ Troubleshooting

### "No active Colyseus rooms found"

**Possible causes:**
- Not in Play mode → Press Play
- Room not connected yet → Wait for connection
- Room field is inaccessible → Make sure your MonoBehaviour has a ColyseusRoom field

### State shows as "null"

**Possible causes:**
- Initial state hasn't arrived → Wait a moment
- Connection issue → Check server logs
- Serializer problem → Verify Schema generation

### Values not updating

**Possible causes:**
- Auto Refresh is off → Enable it in toolbar
- Foldout is collapsed → Expand to see updates
- Connection dropped → Check connection status

---

## 📚 Learn More

- [Full Documentation](RoomInspector-README.md)
- [Implementation Details](IMPLEMENTATION_NOTES.md)
- [Colyseus Docs](https://docs.colyseus.io/)

---

## 🎮 Example

Try it with the included example:

1. Start the Colyseus server:
   ```bash
   cd Server
   npm install
   npm start
   ```

2. Open the Example scene in Unity

3. Open the Room Inspector (Window → Colyseus → Room Inspector)

4. Press Play

5. Watch the room state appear and update!

---

**Happy Debugging! 🐛🔧**

