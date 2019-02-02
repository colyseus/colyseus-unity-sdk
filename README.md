<div align="center">
  <a href="https://github.com/colyseus/colyseus">
    <img src="https://github.com/colyseus/colyseus/blob/master/media/header.png?raw=true" />
  </a>
  <br>
  <br>
  <a href="https://npmjs.com/package/colyseus">
    <img src="https://img.shields.io/npm/dm/colyseus.svg?style=for-the-badge&logo=data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAQAAAC1+jfqAAAABGdBTUEAALGPC/xhBQAAACBjSFJNAAB6JgAAgIQAAPoAAACA6AAAdTAAAOpgAAA6mAAAF3CculE8AAAAAmJLR0QAAKqNIzIAAAAJcEhZcwAADsQAAA7EAZUrDhsAAAAHdElNRQfjAgETESWYxR33AAAAtElEQVQoz4WQMQrCQBRE38Z0QoTcwF4Qg1h4BO0sxGOk80iCtViksrIQRRBTewWxMI1mbELYjYu+4rPMDPtn12ChMT3gavb4US5Jym0tcBIta3oDHv4Gwmr7nC4QAxBrCdzM2q6XqUnm9m9r59h7Rc0n2pFv24k4ttGMUXW+sGELTJjSr7QDKuqLS6UKFChVWWuFkZw9Z2AAvAirKT+JTlppIRnd6XgaP4goefI2Shj++OnjB3tBmHYK8z9zAAAAJXRFWHRkYXRlOmNyZWF0ZQAyMDE5LTAyLTAxVDE4OjE3OjM3KzAxOjAwGQQixQAAACV0RVh0ZGF0ZTptb2RpZnkAMjAxOS0wMi0wMVQxODoxNzozNyswMTowMGhZmnkAAAAZdEVYdFNvZnR3YXJlAHd3dy5pbmtzY2FwZS5vcmeb7jwaAAAAAElFTkSuQmCC">
  </a>
  <a href="https://patreon.com/endel" title="Donate to this project using Patreon">
    <img src="https://img.shields.io/badge/endpoint.svg?url=https%3A%2F%2Fshieldsio-patreon.herokuapp.com%2Fendel&style=for-the-badge" alt="Patreon donate button"/>
  </a>
  <a href="https://discuss.colyseus.io" title="Discuss on Forum">
    <img src="https://img.shields.io/badge/discuss-on%20forum-brightgreen.svg?style=for-the-badge&colorB=0069b8&logo=data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAQAAAC1+jfqAAAABGdBTUEAALGPC/xhBQAAACBjSFJNAAB6JgAAgIQAAPoAAACA6AAAdTAAAOpgAAA6mAAAF3CculE8AAAAAmJLR0QAAKqNIzIAAAAJcEhZcwAADsQAAA7EAZUrDhsAAAAHdElNRQfjAgETDROxCNUzAAABB0lEQVQoz4WRvyvEARjGP193CnWRH+dHQmGwKZtFGcSmxHAL400GN95ktIpV2dzlLzDJgsGgGNRdDAzoQueS/PgY3HXHyT3T+/Y87/s89UANBKXBdoZo5J6L4K1K5ZxHfnjnlQUf3bKvkgy57a0r9hS3cXfMO1kWJMza++tj3Ac7/LY343x1NA9cNmYMwnSS/SP8JVFuSJmr44iFqvtmpjhmhBCrOOazCesq6H4P3bPBjFoIBydOk2bUA17I080Es+wSZ51B4DIA2zgjSpYcEe44Js01G0XjRcCU+y4ZMrDeLmfc9EnVd5M/o0VMeu6nJZxWJivLmhyw1WHTvrr2b4+2OFqra+ALwouTMDcqmjMAAAAldEVYdGRhdGU6Y3JlYXRlADIwMTktMDItMDFUMTg6MTM6MTkrMDE6MDAC9f6fAAAAJXRFWHRkYXRlOm1vZGlmeQAyMDE5LTAyLTAxVDE4OjEzOjE5KzAxOjAwc6hGIwAAABl0RVh0U29mdHdhcmUAd3d3Lmlua3NjYXBlLm9yZ5vuPBoAAAAASUVORK5CYII=" alt="Discussion forum" />
  </a>
  <a href="https://discord.gg/RY8rRS7">
    <img src="https://img.shields.io/discord/525739117951320081.svg?style=for-the-badge&colorB=7581dc&logo=discord&logoColor=white">
  </a>
  <h3>
     Multiplayer Game Client for <a href="https://unity3d.com/">Unity</a>. <br /><a href="http://colyseus.io/docs/">View documentation</a>
  </h3>
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
room.OnStateChange += OnStateChange;
```

**Getting the full room state**

```csharp
void OnStateChange (object sender, RoomUpdateEventArgs e)
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
