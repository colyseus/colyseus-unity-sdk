using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Colyseus.Schema;
using UnityEngine;

public abstract class AbstractNetworkService : INetworkService
{
    private string _gameName = null;
    private string _hostname = null;
    private string _port = null;
    private bool _secureProtocol = false;

    public string GameName
    {
        get => string.IsNullOrEmpty(_gameName) ? "MyRoom" : _gameName;
        set => _gameName = value;
    }

    public string HostName
    {
        get => string.IsNullOrEmpty(_hostname) ? "localhost" : _hostname;
        set => _hostname = value;
    }

    public string Port
    {
        get => string.IsNullOrEmpty(_port) ? "2567" : _port;
        set => _port = value;
    }

    public string Protocol
    {
        get => _secureProtocol ? "wss" : "ws";
        set => _secureProtocol = !_secureProtocol;
    }

    public string HostAddress => $"{Protocol}://{HostName}:{Port}";
    public bool IsInitialized { get; protected set; } = false;
    public abstract string LocalPlayerId { get; }
    public abstract Dictionary<string, NetworkedPlayerMovement> Players { get; set; }
    public abstract Player GetPlayerById(string playerId);
    public abstract NetworkedPlayerMovement GetPlayerObjectById(string playerId);
    public abstract void Initialize();
    public abstract UniTask JoinOrCreateGame();
    public virtual UniTask Send(object obj) => default;
    public abstract Action SubscribeToPlayerAdded(Action<string, Player> handler);
    public abstract Action SubscribeToPlayerRemoved(Action<string, Player> handler);
    public abstract Action SubscribeToPlayerChanged(Action<string, Player> handler);
    public abstract UniTask SendPosition(Vector2 position);
    public abstract UniTask Leave(bool consented = true);
    public abstract void Dispose();
}