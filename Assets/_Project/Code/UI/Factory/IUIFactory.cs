using UnityEngine;

public interface IUIFactory : IService
{
    public UIRoot CreateUIRoot(bool again = false);
    public T CreateUIElement<T>(string assetPath, bool isActive = true, Transform container = null,
        bool useObjectPool = false, int defaultCapacity = 10, int maxSize = 20) where T : Component;
    public Component CreateEventSystem();
    public MainMenuView CreateMainMenuView();
    public GameplayView CreateGameplayView();
}