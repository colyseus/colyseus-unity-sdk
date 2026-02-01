using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Colyseus;

public class WeatherMessage {
  public string weather;
}

public class ColyseusNetworkManager : MonoBehaviour
{
  protected Client client;
  protected Room<MyRoomState> room;

  protected Room<NoState> lobbyRoom;

  protected Room<NoState> queueRoom;
  // Start is called before the first frame update
  void Start()
  {
    client = new Client("ws://localhost:2567");

    //
    // join "my_room" and bind callbacks
    // ------------------------------------------------------------
    // this is the main game room demonstration, with messages and state callbacks
    //
    joinMyRoom();

    //
    // join "lobby" room and bind callbacks
    // ------------------------------------------------------------
    // when creating or destroying "my_room" rooms, the "lobby" room will receive these events
    //
    joinLobbyRoom();

    //
    // join "queue" room and bind callbacks
    // ------------------------------------------------------------
    // the "queue" room requires 4 clients to join the game room
    // you can open the playground at http://localhost:2567, and join the "queue" room 3 times to test it
    //
    joinQueueRoom();
  }

  // Update is called once per frame
  void Update()
  {

  }

  async void OnDestroy()
  {
    // Close all room connections when the MonoBehaviour is destroyed
    // (e.g., when stopping play mode in the Editor)
    if (room != null) await room.Leave();
    if (lobbyRoom != null) await lobbyRoom.Leave();
    if (queueRoom != null) await queueRoom.Leave();
  }

  protected async void joinMyRoom()
  {
    var options = new Dictionary<string, object>();
    room = await client.JoinOrCreate<MyRoomState>("my_room", options);

    // Allow to reconnect immediately
    room.Reconnection.MinUptime = 0;

    room.OnLeave += (int code) => {
      Debug.Log($"[MyRoom] Left room with code: {code}");
    };

    //
    // messages from server
    //
    room.OnMessage("weather", (WeatherMessage message) =>
    {
      Debug.Log($"[MyRoom] Weather changed: {message.weather}");
    });

    //
    // state callbacks
    //
    var callbacks = Colyseus.Schema.Callbacks.Get(room);

    callbacks.OnAdd(state => state.players, (key, player) =>
    {
      Debug.Log($"[MyRoom] Player {key} joined the room");

      callbacks.OnChange(player, () =>
      {
        Debug.Log($"[MyRoom] Player {key} changed!");
      });
    });

    callbacks.OnRemove(state => state.players, (key, player) =>
    {
      Debug.Log($"[MyRoom] Player {key} left the room");
    });
  }

  protected async void joinLobbyRoom()
  {
    lobbyRoom = await client.JoinOrCreate("lobby");

    lobbyRoom.OnMessage("rooms", (RoomAvailable[] message) =>
    {
      Debug.Log($"[Lobby] Rooms: {message}");
    });

    lobbyRoom.OnMessage("+", (object[] message) =>
    {
      string roomId = (string)message[0];
      var roomData = (IDictionary<string, object>)message[1];
      Debug.Log($"[Lobby] Add Room: {roomId} - {roomData["name"]} ({roomData["clients"]}/{roomData["maxClients"]})");
    });

    lobbyRoom.OnMessage("-", (string message) =>
    {
      Debug.Log($"[Lobby] Remove Room: {message}");
    });

  }

  protected async void joinQueueRoom()
  {
    queueRoom = await client.JoinOrCreate("queue");

    queueRoom.OnMessage("clients", (int clients) =>
    {
      Debug.Log($"[Queue] Clients: {clients}");
    });

    queueRoom.OnMessage("seat", async (SeatReservation seat) => {
      Debug.Log($"[Queue] Seat: {seat.roomId} - {seat.sessionId} - {seat.publicAddress} - {seat.processId} - {seat.reconnectionToken} - {seat.devMode} - {seat.protocol}");

      var room = await client.ConsumeSeatReservation<MyRoomState>(seat);

      Debug.Log($"[Queue] Joined game room: {room.RoomId}");

      room.OnLeave += (int code) => {
        Debug.Log($"[Queue] Left game room: {code}");
      };

      // confirm successfully joined the room! / leave the queue room
      _ = queueRoom.Send("confirm");

      // delay 1 second
      _ = Task.Delay(500);

      //
      // queue demo is over, leave the room
      // (you should actually bind the callbacks for the room and not leave it here)
      //
      _ = room.Leave();
    });

    queueRoom.OnLeave += (int code) => {
      Debug.Log($"[Queue] Left queue room: {code}");
    };

  }
}
