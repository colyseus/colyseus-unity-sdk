using System;
using Colyseus.Schema;
using UnityEngine;
using VContainer;

public sealed class LocalPlayerSynchronizationSystem : MonoBehaviour
{
    [Inject] private readonly INetworkService _networkService;
    [Inject] private readonly IGameFactory _gameFactory;

    private Action _onPlayerAddedHandler;
    private Action _onPlayerChangeHandler;
    private Action _onPlayerRemovedHandler;

    private string _localPlayerId => _networkService.LocalPlayerId;

    private void Start()
    {
        _onPlayerAddedHandler = _networkService.SubscribeToPlayerAdded(OnPlayerAdded);
    }

    private void OnPlayerAdded(string playerId, Player player)
    {
        if (playerId != _localPlayerId)
        {
            return;
        }

        var playerPosition = new Vector2(0f, 0f);
        var networkedPlayer = _gameFactory.CreateLocalPlayer(playerPosition);

        // Subscribe to OnChange for the local player
        _onPlayerChangeHandler = player.OnChange(
            () => OnPlayerPropertiesChanged(playerId, player, networkedPlayer));

        // Subscribe to OnRemove for this local player
        _onPlayerRemovedHandler = player.OnRemove(
            () => OnPlayerRemoved(playerId, player, networkedPlayer));

        Debug.Log($"Local Player {playerId} has been spawned at position {playerPosition}");
    }

    /// <summary>
    /// This method is now called when the local player's properties are changed.
    /// </summary>
    /// <param name="playerId"></param>
    /// <param name="player"></param>
    /// <param name="networkedPlayer"></param>
    private void OnPlayerPropertiesChanged(string playerId, Player player, LocalPlayerMovement networkedPlayer)
    {
        //Debug.Log($"Local Player {playerId} changed: x={player.x}, y={player.y}");
    }

    private void OnPlayerRemoved(string playerId, Player player, LocalPlayerMovement networkedPlayer)
    {
        if (networkedPlayer != null)
        {
            _onPlayerAddedHandler?.Invoke();
            _onPlayerChangeHandler?.Invoke();
            _onPlayerRemovedHandler?.Invoke();

            Debug.Log($"Local Player {playerId} has left the Game!");
            Destroy(networkedPlayer.gameObject);   
        }
    }

    private void OnDestroy()
    {
        _onPlayerAddedHandler?.Invoke();
        _onPlayerChangeHandler?.Invoke();
        _onPlayerRemovedHandler?.Invoke();
    }
}