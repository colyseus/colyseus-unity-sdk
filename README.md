<div align="center">
  <a href="https://github.com/gamestdio/colyseus">
    <img src="https://github.com/gamestdio/colyseus/blob/master/media/header.png?raw=true" />
  </a>
  <br>
  <br>
  <a href="https://npmjs.com/package/colyseus">
    <img src="https://img.shields.io/npm/dm/colyseus.svg">
  </a>
  <a href="https://patreon.com/endel" title="Donate to this project using Patreon">
    <img src="https://img.shields.io/badge/patreon-donate-yellow.svg" alt="Patreon donate button" />
  </a>
  <a href="http://discuss.colyseus.io" title="Discuss on Forum">
    <img src="https://img.shields.io/badge/discuss-on%20forum-brightgreen.svg?style=flat&colorB=b400ff" alt="Discussion forum" />
  </a>
  <a href="https://gitter.im/gamestdio/colyseus">
    <img src="https://badges.gitter.im/gamestdio/colyseus.svg">
  </a>
  <h3>
     Multiplayer Game Client for Unity / C#
  <h3>
</div>

## Installation

Copy `Assets/Plugins` into your Unity project.

## Running the demo server

Ensure you have [Node v6+](http://nodejs.org/) installed. Then run these
commands in your commandline:

```
cd Server
npm install
npm start
```

## Usage

You'll need to start a coroutine for each WebSocket connection with the server.
See [usage example](Assets/ColyseusClient.cs) for more details.

```csharp
Client colyseus = new Colyseus.Client ("ws://localhost:2657");

Room room = colyseus.Join ("room_name");
room.OnUpdate += OnUpdate;
```

**Getting the full room state**

```csharp
void OnUpdate (object sender, RoomUpdateEventArgs e)
{
	if (e.isFirstState) {
		// First setup of your client state
		Debug.Log(e.state);
	} else {
		// Further updates on your client state
		Debug.Log(e.state);
	}
}
```

**Listening to add/remove on a specific key on the room state**

```csharp
room.Listen ("players/:id", OnPlayerChange);
```

```csharp
void OnPlayerChange (DataChange change)
{
	Debug.Log (change.path["id"]);
	Debug.Log (change.operation); // "add" or "remove"
	Debug.Log (change.value); // the player object
}
```

**Listening to specific data changes in the state**

```csharp
room.Listen ("players/:id/:axis", OnPlayerMove);
```

```csharp
void OnPlayerMove (DataChange change)
{
	Debug.Log ("OnPlayerMove");
	Debug.Log ("playerId: " + change.path["id"] + ", axis: " + change.path["axis"]);
	Debug.Log (change.value);
}
```

## License

MIT
