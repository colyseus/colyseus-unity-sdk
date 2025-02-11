using UnityEngine;

public interface IGameFactory
{
    public T CreatePrefab<T>(string assetAddress, Vector2 at) where T : Object;
    public LocalPlayerMovement CreateLocalPlayer(Vector2 at);
    public NetworkedPlayerMovement CreateNetworkedPlayer(Vector2 at);
    public LocalPlayerSynchronizationSystem CreateLocalPlayerSynchronizationSystem(Vector2 at);
    public NetworkedPlayerSynchronizationSystem CreateNetworkedPlayerSynchronizationSystem(Vector2 at);
}