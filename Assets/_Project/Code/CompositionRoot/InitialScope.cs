using VContainer;
using VContainer.Unity; 
// VContainer is a Dependency Injection Framework for the Unity Game Engine.

public class InitialScope : LifetimeScope
{
    private static bool _started = false;

    private class EntryPoint : IStartable
    {
        public void Start()
        {
            LifetimeScope lifetimeScope = Find<InitialScope>();

            /*
            For a producation ready project, a good practice would be to use a game state machine 
            and the following game states:

            BootstrapState, 
            LoadProgressState, 
            MainMenuState, 
            LoadLevelSinglePlayerModeState, 
            LoadLevelMultiplayerModeState, 
            GameLoopState, 
            EndGameState
            */

            IUIFactory uIFactory = lifetimeScope.Container.Resolve<IUIFactory>();
            uIFactory.CreateEventSystem();
            uIFactory.CreateMainMenuView();
        }
    }

    /// <summary>
    /// VContainer will manage the lifecycle of the services and ensure that only one instance of each class exists.
    /// </summary>
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<SceneManagementService>(Lifetime.Singleton).As<ISceneManagementService>();
        builder.Register<AssetProviderService>(Lifetime.Singleton).As<IAssetProviderService>();

        builder.Register<UIFactory>(Lifetime.Singleton).As<IUIFactory>();
        builder.Register<GameFactory>(Lifetime.Singleton).As<IGameFactory>();

        builder.Register<ColyseusService>(Lifetime.Singleton).As<INetworkService>();

        if (!_started)
        {
            builder.RegisterEntryPoint<EntryPoint>();
            _started = true;
        }
    }
}