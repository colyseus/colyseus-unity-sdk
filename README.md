<div align="center">
  <a href="https://github.com/gamestdio/colyseus">
    <img src="https://github.com/gamestdio/colyseus/blob/master/media/header.png?raw=true" />
  </a>
  <br>
  <br>
	<a href="https://npmjs.com/package/colyseus">
		<img src="https://img.shields.io/npm/dm/colyseus.svg">
	</a>
	<a href="https://github.com/gamestdio/colyseus#sponsors">
		<img src="https://opencollective.com/colyseus/sponsors/badge.svg">
	</a>
	<a href="https://github.com/gamestdio/colyseus#backers">
		<img src="https://opencollective.com/colyseus/backers/badge.svg">
	</a>
	<a href="https://gitter.im/gamestdio/colyseus">
		<img src="https://badges.gitter.im/gamestdio/colyseus.svg">
	</a>
  <h3>
     Multiplayer Game Client for Unity / C#
  <h3>
</div>

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
	Debug.Log(e.isFirstState); // is this the very first room update from the server?
}
```

**Listening to add/remove on state**

```csharp
room.Listen ("players/:id", OnPlayerChange);
```

```csharp
void OnPlayerChange (DataChange change)
{
	Debug.Log (change.path["id"]);
	Debug.Log (change.operation);
	Debug.Log (change.value);
}
```

**Listening to updates on state**

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

## Building for Windows Store (Universal 10 / UWP)
Build steps for UWP:
- rename Colyseus/MsgPack/MsgPack.dll to something like MsgPack2.dll.
- rename Colyseus/MsgPack/MsgPackUWP.dll to MsgPack.dll

Otherwise, build process for Windows Store (Universal 10) will fail because the MsgPack-namespace can't be found.
The Unity Player however should work as expected after this, despite the renaming.
## License

MIT
