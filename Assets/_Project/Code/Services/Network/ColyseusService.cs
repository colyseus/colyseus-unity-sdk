using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Colyseus;
using Colyseus.Schema;
using NativeWebSocket;
using UnityEngine;

public sealed partial class ColyseusService : AbstractNetworkService
{
    private ColyseusClient _client = null;
    private ColyseusRoom<MyRoomState> _room = null;

    public ColyseusClient Client
    {
        get
        {
            // Initialize Colyseus client, if the client has not been initiated yet or input values from the Menu have been changed.
            if (_client == null || !_client.Settings.WebRequestEndpoint.Contains(HostAddress))
            {
                Initialize();
            }
            return _client;
        }
    }

    public ColyseusRoom<MyRoomState> Room
    {
        get
        {
            if (_room == null)
            {
                Debug.LogError("Room hasn't been initialized yet!");
            }
            return _room;
        }
    }

    public override string LocalPlayerId => Room.SessionId;
    public override Dictionary<string, NetworkedPlayerMovement> Players { get; set; } = new Dictionary<string, NetworkedPlayerMovement>();

    public override Player GetPlayerById(string playerId)
    {
        if (Room?.State?.players == null)
        {
            Debug.LogError("Room or Room.State.players is not initialized!");
            return null;
        }

        if (Room.State.players.TryGetValue(playerId, out var player))
        {
            return player;
        }

        Debug.LogError($"Player with ID {playerId} not found!");
        return null;
    }

    public override NetworkedPlayerMovement GetPlayerObjectById(string playerId)
    {
        if (Room?.State?.players == null)
        {
            Debug.LogError("Room or Room.State.players is not initialized!");
            return null;
        }

        if (Players.TryGetValue(playerId, out var networkedPlayer))
        {
            return networkedPlayer;
        }

        Debug.LogError($"Player object with ID {playerId} not found!");
        return null;
    }

    public override void Initialize()
    {
        _client = new ColyseusClient(HostAddress);
        IsInitialized = true;
        Debug.Log("Colyseus client has been initialized!");
    }

    public override async UniTask JoinOrCreateGame()
    {
        // Will create a new game room if there is no available game rooms in the server.
        _room = await Client.JoinOrCreate<MyRoomState>(GameName);

        SubscribeToRoomEvents();
    }

    private void SubscribeToRoomEvents()
    {
        _room.OnStateChange += OnStateChangeHandler;
        _room.OnLeave += OnLeaveRoom;

        // Assigning listener for incoming messages
        _room.OnMessage<string>("welcomeMessage", message =>
        {
            Debug.Log(message);
        });
    }

    private void OnStateChangeHandler(MyRoomState state, bool isFirstState)
    {
    }

    private void OnLeaveRoom(int code)
    {
        WebSocketCloseCode closeCode = WebSocketExtensions.ParseCloseCodeEnum(code);
        Debug.Log(string.Format("ROOM: ON LEAVE =- Reason: {0} ({1})", closeCode, code));
    }

    public override Action SubscribeToPlayerAdded(Action<string, Player> handler)
    {
        return Room.State.players.OnAdd((key, player) => 
        {
            if (key == LocalPlayerId)
            {
                Debug.Log($"Local Player {key} has joined the Game!");
            }
            else
            {
                Debug.Log($"Player {key} has joined the Game!");
            }
            
            handler(key, player);
        });
    }

    public override Action SubscribeToPlayerRemoved(Action<string, Player> handler)
    {
        return Room.State.players.OnRemove((key, player) =>
        {
            handler(key, player);
        });
    }

    public override Action SubscribeToPlayerChanged(Action<string, Player> handler)
    {
        return Room.State.players.OnChange((key, player) =>
        {
            handler(key, player);
        });
    }

    public override async UniTask Send(object obj)
    {
        await Room.Send("message", new { obj });
    }

    public override async UniTask SendPosition(Vector2 position)
    {
        await Room.Send("position", position.AsPack());
    }

    public override async UniTask Leave(bool consented = true)
    {
        await Room.Leave(true);
    }

    public override async void Dispose()
    {
        if (_room != null)
        {
            _room.OnStateChange -= OnStateChangeHandler;
            _room.OnLeave -= OnLeaveRoom;

            await _room.Leave();
        }
    }
}