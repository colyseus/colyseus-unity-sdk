<div align="center">
  <a href="https://colyseus.io/">
    <img src="https://github.com/colyseus/colyseus/blob/master/media/logo.svg?raw=true" width="40%" />
  </a>
  <h3>Colyseus Multiplayer SDK for MonoGame</h3>
</div>

MonoGame integration for [Colyseus](https://colyseus.io/) — provides a `GameComponent` that automatically dispatches WebSocket events on the game loop.

## Installation

```sh
dotnet add package Colyseus.MonoGame
```

This installs the core `Colyseus` SDK as a dependency.

## Setup

Register the `ColyseusGameComponent` in your game's `Initialize()` method:

```csharp
using Colyseus.MonoGame;

protected override void Initialize()
{
    Components.Add(new ColyseusGameComponent(this));
    base.Initialize();
}
```

This single line handles all WebSocket event dispatching on the game thread — no manual polling needed.

## Quick Example

```csharp
using Microsoft.Xna.Framework;
using Colyseus;
using Colyseus.Schema;
using Colyseus.MonoGame;

public class Game1 : Game
{
    Client client;
    Room<MyRoomState> room;

    protected override void Initialize()
    {
        Components.Add(new ColyseusGameComponent(this));
        base.Initialize();
    }

    protected override async void LoadContent()
    {
        client = new Client("ws://localhost:2567");

        room = await client.JoinOrCreate<MyRoomState>("my_room");

        var callbacks = Callbacks.Get(room);

        callbacks.OnAdd(state => state.players, (sessionId, player) => {
            System.Diagnostics.Debug.WriteLine($"Player joined: {sessionId}");
        });

        room.Send("move", new { x = 10f, y = 20f });

        room.OnMessage<string>("chat", (message) => {
            System.Diagnostics.Debug.WriteLine("Chat: " + message);
        });
    }

    protected override void OnExiting(object sender, EventArgs args)
    {
        if (room != null) room.Leave();
        base.OnExiting(sender, args);
    }
}
```

## Documentation

See the full documentation at **https://docs.colyseus.io/getting-started/monogame**

## License

MIT
