# ![colyseus.js](https://github.com/gamestdio/colyseus/blob/master/media/header.png?raw=true)

> Multiplayer Game Client for Unity.

[![Join the chat at https://gitter.im/gamestdio/colyseus](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/gamestdio/colyseus?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
[![Donate](https://img.shields.io/badge/Donate-PayPal-green.svg)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=N9C36SSE9ZCTS)

C#/Unity client for [Colyseus](https://github.com/gamestdio/colyseus)
Multiplayer Game Server.

## Installation

Copy `Assets/Colyseus` into your project. See [usage
example](Assets/ColyseusClient.cs).

## Running the demo server

Ensure you have [Node v6+](http://nodejs.org/) installed. Then run these
commands in your commandline:

```
cd Server
npm install
npm start
```

## Usage

```csharp
Client colyseus = new Colyseus.Client ("ws://localhost:2657");

Room room = colyseus.Join ("room_name");
room.OnUpdate += OnUpdate;
```


**Getting the full room state**

```csharp
void OnUpdate (object sender, RoomUpdateEventArgs e)
{
	Debug.Log(e.state);
}
```

**Listening to additions on state**

```csharp
room.state.Listen ("players", "add", OnAddPlayer);
```

```csharp
void OnAddPlayer (string[] path, MessagePackObject value)
{
	Debug.Log ("OnAddPlayer");
	Debug.Log (value);
}
```

**Listening to updates on state**

```csharp
room.state.Listen ("players/:id/:axis", "replace", OnPlayerMove);
```

```csharp
void OnPlayerMove (string[] path, MessagePackObject value)
{
	Debug.Log ("OnPlayerMove");
	Debug.Log ("playerId: " + path[0] + ", axis: " + path[1]);
	Debug.Log (value);
}
```

**Listening to deletions on state**

```csharp
room.state.Listen ("players/:id", "remove", OnPlayerRemoved);
```

```csharp
void OnPlayerRemoved (string[] path, MessagePackObject value)
{
	Debug.Log ("OnPlayerRemoved");
	Debug.Log ("playerId: " + path[0]);
}
```

## Building for Windows Store (Universal 10 / UWP)
Build steps for UWP:
- rename Colyseus/MsgPack/MsgPack.dll to something like MsgPack2.dll.
- rename Colyseus/MsgPack/MsgPackUWP.dll to MsgPack.dll

Otherwise, build process for Windows Store (Universal 10) will fail because the MsgPack-namespace can't be found.
The Unity Player however should work as expected after this, despite the renaming.
## License

MIT
