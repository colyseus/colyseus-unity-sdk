using UnityEngine;
using VContainer;

public sealed class GameFactory : IGameFactory
{
    private readonly IObjectResolver _resolver;
    [Inject] private readonly IAssetProviderService _assets;

    private GameFactory(IObjectResolver resolver)
    {
        _resolver = resolver;
    }

    public T CreatePrefab<T>(string assetAddress, Vector2 at) where T : Object
    {
        var prefab = _assets.Instantiate<T>(assetAddress, at);
        _resolver.Inject(prefab);
        
        return prefab;
    }

    public LocalPlayerMovement CreateLocalPlayer(Vector2 at)
        => CreatePrefab<LocalPlayerMovement>(AssetPath.LocalPlayerPath, at);

    public NetworkedPlayerMovement CreateNetworkedPlayer(Vector2 at)
        => CreatePrefab<NetworkedPlayerMovement>(AssetPath.NetworkedPlayerPath, at);
    public LocalPlayerSynchronizationSystem CreateLocalPlayerSynchronizationSystem(Vector2 at)
        => CreatePrefab<LocalPlayerSynchronizationSystem>(AssetPath.LocalPlayerSynchronizationSystemPath, at);

    public NetworkedPlayerSynchronizationSystem CreateNetworkedPlayerSynchronizationSystem(Vector2 at)
        => CreatePrefab<NetworkedPlayerSynchronizationSystem>(AssetPath.NetworkedPlayerSynchronizationSystemPath, at);
}
