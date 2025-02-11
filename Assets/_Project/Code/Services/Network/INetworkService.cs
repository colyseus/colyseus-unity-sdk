using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Colyseus.Schema;
using UnityEngine;

public interface INetworkService : IService, IDisposable
{
    public string GameName { get; set; }
    public string HostName { get; set; }
    public string Port { get; set; }
    public string Protocol { get; set; }
    public string HostAddress { get; }
    public bool IsInitialized { get; }
    public string LocalPlayerId { get; }
    public Dictionary<string, NetworkedPlayerMovement> Players { get; set; }
    public Player GetPlayerById(string playerId);
    public NetworkedPlayerMovement GetPlayerObjectById(string playerId);
    public void Initialize();
    public UniTask JoinOrCreateGame();
    public Action SubscribeToPlayerAdded(Action<string, Player> handler);
    public Action SubscribeToPlayerRemoved(Action<string, Player> handler);
    public Action SubscribeToPlayerChanged(Action<string, Player> handler);
    public UniTask Send(object obj);
    public UniTask SendPosition(Vector2 position);
    public UniTask Leave(bool consented = true);
}