using System;
using System.Collections.Generic;
using Colyseus.Schema;
using UnityEngine;
using VContainer;

public sealed class NetworkedPlayerSynchronizationSystem : MonoBehaviour
{
    [Inject] private readonly INetworkService _networkService;
    [Inject] private readonly IGameFactory _gameFactory;

    private Action _onPlayerAddedHandler;
    private Dictionary<string, Action> _playerChangeHandlers = new Dictionary<string, Action>();
    private Dictionary<string, Action> _playerRemoveHandlers = new Dictionary<string, Action>();

    private string _localPlayerId => _networkService.LocalPlayerId;

    private void Start()
    {
        _onPlayerAddedHandler = _networkService.SubscribeToPlayerAdded(OnPlayerAdded);
    }

    // It would be good practice to use object pooling.
    private void OnPlayerAdded(string playerId, Player player)
    {
        if (playerId == _localPlayerId)
        {
            return;
        }

        var playerPosition = new Vector2(0f, 0f);
        var networkedPlayer = _gameFactory.CreateNetworkedPlayer(playerPosition);

        // Subscribe to OnChange for the specific player
        _playerChangeHandlers[playerId] = player.OnChange(
            () => OnPlayerPropertiesChanged(playerId, player, networkedPlayer));

        // Subscribe to OnRemove for this specific player
        _playerRemoveHandlers[playerId] = player.OnRemove(
            () => OnPlayerRemoved(playerId, player, networkedPlayer));

        _networkService.Players[playerId] = networkedPlayer;

        Debug.Log($"Player {playerId} has been spawned at position {playerPosition}");
    }

    /// <summary>
    /// This method is now called when a networked player's properties are changed.
    /// </summary>
    /// <param name="playerId"></param>
    /// <param name="player"></param>
    /// <param name="networkedPlayer"></param>
    private void OnPlayerPropertiesChanged(string playerId, Player player, NetworkedPlayerMovement networkedPlayer)
    {
        Debug.Log($"Player {playerId} has been changed: x={player.x}, y={player.y}");
        
        if (networkedPlayer != null)
        {
            networkedPlayer.SetTargetPosition(new Vector2(player.x, player.y));
        }
    }

    private void OnPlayerRemoved(string playerId, Player player, NetworkedPlayerMovement networkedPlayer)
    {
        if (networkedPlayer != null)
        {
            if (_playerChangeHandlers.TryGetValue(playerId, out var changeHandler))
            {
                changeHandler?.Invoke();
                _playerChangeHandlers.Remove(playerId);
            }

            if (_playerRemoveHandlers.TryGetValue(playerId, out var removeHandler))
            {
                removeHandler?.Invoke();
                _playerRemoveHandlers.Remove(playerId);
            }
            
            _networkService.Players.Remove(playerId);

            Debug.Log($"Player {playerId} has left the Game!");
            Destroy(networkedPlayer.gameObject);
        }
    }
    
    private void OnDestroy()
    {
        _onPlayerAddedHandler?.Invoke();

        foreach (var handler in _playerChangeHandlers.Values)
        {
            handler?.Invoke();
        }
        _playerChangeHandlers.Clear();

        foreach (var handler in _playerRemoveHandlers.Values)
        {
            handler?.Invoke();
        }
        _playerRemoveHandlers.Clear();
        _networkService.Players.Clear();
    }
}