<div align="center">
  <a href="https://colyseus.io/">
    <img src="https://github.com/colyseus/colyseus/blob/master/media/logo.svg?raw=true" width="40%" />
  </a>
  <h3>Colyseus Multiplayer SDK for C#/.NET</h3>
</div>

Engine-agnostic core SDK for [Colyseus](https://colyseus.io/) — includes Schema serialization, matchmaking, room management, and state synchronization.

Works with Unity, Godot (C#), MonoGame, and any .NET project.

## Installation

```sh
dotnet add package Colyseus
```

## Threading

When the client is created on a thread with a `SynchronizationContext` (Unity, Godot C#), Colyseus posts WebSocket callbacks back to that context automatically.

For engines without one, either register an external dispatcher through `ColyseusContext.RegisterWebSocketForDispatch`, or call `room.Connection.DispatchMessageQueue()` from your update loop.

## Quick Example

```csharp
using Colyseus;
using Colyseus.Schema;

var client = new Client("ws://localhost:2567");

var room = await client.JoinOrCreate<MyRoomState>("my_room");
Console.WriteLine("Joined room: " + room.Id);

var callbacks = Callbacks.Get(room);

callbacks.Listen(state => state.currentTurn, (currentValue, previousValue) => {
    Console.WriteLine($"Turn changed: {previousValue} -> {currentValue}");
});

callbacks.OnAdd(state => state.players, (sessionId, player) => {
    Console.WriteLine($"Player joined: {sessionId}");
});

room.Send("move", new { x = 10f, y = 20f });

room.OnMessage<string>("chat", (message) => {
    Console.WriteLine("Chat: " + message);
});
```

## Documentation

See the full documentation at **https://docs.colyseus.io/getting-started/unity**

## License

MIT
