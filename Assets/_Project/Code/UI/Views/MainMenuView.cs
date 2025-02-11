using UnityEngine;
using VContainer;

public class MainMenuView : MonoBehaviour
{
    private readonly string _gameScene = "Game";
    [Inject] private readonly INetworkService _networkService;
    [Inject] private readonly ISceneManagementService _sceneManagementService;
    [Inject] private readonly IUIFactory _uIFactory;
    [Inject] private readonly IGameFactory _gameFactory;

    public string GameName
    {
        get => _networkService.GameName;
        set => _networkService.GameName = value;
    }

    public string HostName
    {
        get => _networkService.HostName;
        set => _networkService.HostName = value;
    }

    public string Port
    {
        get => _networkService.Port;
        set => _networkService.Port = value;
    }

    public string Protocol
    {
        get => _networkService.Protocol;
        set => _networkService.Protocol = value;
    }

    public string HostAddress => $"{Protocol}://{HostName}:{Port}";

    private async void EnterGameScene()
    {
        _networkService.Initialize();

        if (_networkService.IsInitialized == false)
        {
            Debug.LogWarning("Failed to connect to the server!");
            return;
        }

        await _networkService.JoinOrCreateGame();

        _uIFactory.CreateUIRoot(true);
        _uIFactory.CreateEventSystem();
        _uIFactory.CreateGameplayView();

        _gameFactory.CreateLocalPlayerSynchronizationSystem(Vector2.zero);
        _gameFactory.CreateNetworkedPlayerSynchronizationSystem(Vector2.zero);
    }

    public async void Play()
    {
        await _sceneManagementService.LoadAsync(_gameScene, onLoaded: EnterGameScene);
    }
}
