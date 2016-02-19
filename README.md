# colyseus-c# [![Join the chat at https://gitter.im/gamestdio/colyseus](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/gamestdio/colyseus?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

C#/Unity3D integration for [colyseus](https://github.com/gamestdio/colyseus) - a
Minimalistic MMO Game Server.

## Usage

```csharp
using Colyseus

// [...]

Client colyseus = new Colyseus.Client ("ws://localhost:2657");
Room room = this.colyseus.Join ("room_name");
room.OnUpdate += Room_OnUpdate;

void Room_OnUpdate (object sender, RoomUpdateEventArgs e)
{
  if (e.patches == null) {
    // received first state
    // e.state <- do something with e.state
  } else {
    // received patched state
    // e.patches <- do something with e.patches
  }
}
```

## License

MIT
