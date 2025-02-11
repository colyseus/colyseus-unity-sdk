using UnityEngine;
using VContainer;

public class GameplayView : MonoBehaviour
{
    private readonly string _mainMenuScene = "Menu";
    [Inject] private readonly INetworkService _networkService;
    [Inject] private readonly ISceneManagementService _sceneManagementService;
    [Inject] private readonly IUIFactory _uIFactory;
    
    private void EnterMainMenuScene()
    {
        _uIFactory.CreateUIRoot(true);
        _uIFactory.CreateEventSystem();
        _uIFactory.CreateMainMenuView();
    }

    public async void BackToMenu()
    {
        await _networkService.Leave(true);
        await _sceneManagementService.LoadAsync(_mainMenuScene, onLoaded: EnterMainMenuScene);
    }

    public async void Exit()
    {
        await _networkService.Leave(true);

        Quit();
    }

    private void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif 
    }
}
