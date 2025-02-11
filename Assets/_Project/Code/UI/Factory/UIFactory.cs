using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using UnityEngine.Pool;
using UnityEngine.UI;

public sealed class UIFactory : IUIFactory
{
    private readonly IObjectResolver _resolver;
    [Inject] private readonly IAssetProviderService _assets;

    private UIRoot _uiRoot;

    private readonly Dictionary<string, ObjectPool<GameObject>> _objectPools = new Dictionary<string, ObjectPool<GameObject>>();

    public UIFactory(IObjectResolver resolver)
    {
        _resolver = resolver;
    }

    public UIRoot CreateUIRoot(bool again = false)
    {
        if (_uiRoot == null || again == true)
        {
            _uiRoot = _assets.Instantiate<UIRoot>(AssetPath.UIRootPath);
            _resolver.InjectGameObject(_uiRoot.gameObject);
        }

        return _uiRoot;
    }

    private ObjectPool<GameObject> GetOrCreatePool(string assetPath, int defaultCapacity = 10, int maxSize = 20)
    {
        if (!_objectPools.TryGetValue(assetPath, out var pool))
        {
            pool = new ObjectPool<GameObject>(
                createFunc: () => _assets.Instantiate<GameObject>(assetPath),
                actionOnGet: obj => obj.SetActive(true),
                actionOnRelease: obj => obj.SetActive(false),
                actionOnDestroy: Object.Destroy,
                collectionCheck: false,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize
            );
            _objectPools[assetPath] = pool;
        }
        return pool;
    }

    public T CreateUIElement<T>(
        string assetPath,
        bool isActive = true,
        Transform container = null,
        bool useObjectPool = false,
        int defaultCapacity = 10,
        int maxSize = 20) where T : Component
    {
        if (_uiRoot == null)
        {
            CreateUIRoot();
        }

        T prefab;

        if (useObjectPool)
        {
            prefab = GetOrCreatePool(assetPath, defaultCapacity, maxSize).Get().GetComponent<T>();
        }
        else
        {
            prefab = _assets.Instantiate<T>(assetPath);
        }

        if (container != null)
        {
            prefab.transform.SetParent(container, true);
        }
        else
        {
            prefab.transform.SetParent(_uiRoot.Content, true);
        }

        if (prefab.TryGetComponent<CanvasScaler>(out var canvasScaler))
        {
            Object.Destroy(canvasScaler);
        }

        _resolver.InjectGameObject(prefab.gameObject);
        prefab.gameObject.SetActive(isActive);

        return prefab;
    }

    public void ReleaseUIElement(GameObject element, string assetPath)
    {
        if (_objectPools.TryGetValue(assetPath, out var pool))
        {
            pool.Release(element);
        }
        else
        {
            Object.Destroy(element);
        }
    }

    public Component CreateEventSystem()
        => CreateUIElement<Component>(AssetPath.EventSystemPath);

    public MainMenuView CreateMainMenuView()
        => CreateUIElement<MainMenuView>(AssetPath.MainMenuViewPath);

    public GameplayView CreateGameplayView()
        => CreateUIElement<GameplayView>(AssetPath.GameplayViewPath);
}