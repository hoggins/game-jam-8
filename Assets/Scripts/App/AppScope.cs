using Metagame.MainMenu;
using Metagame.PauseMenu;
using Model;
using ScenesManagement;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App
{
  public class AppScope : LifetimeScope
  {
    public static AppScope Instance => GetInitialized();
    private static AppScope _instance;

    private static AppScope GetInitialized()
    {
      if (_instance != null)
        return _instance;

      _instance = new GameObject(nameof(AppScope)).AddComponent<AppScope>();
      DontDestroyOnLoad(_instance.gameObject);
      return _instance;
    }

    public static void Shutdown()
    {
      if (_instance == null)
        return;

      _instance.Dispose();
      Destroy(_instance.gameObject);
      _instance = null;
    }

    protected override void Configure(IContainerBuilder builder)
    {
      base.Configure(builder);

      builder.Register<SceneService>(Lifetime.Singleton)
        .AsSelf()
        .AsImplementedInterfaces();

      builder.Register<Storage>(Lifetime.Singleton)
        .AsSelf()
        .AsImplementedInterfaces();

      builder.Register<CharacterService>(Lifetime.Singleton).AsSelf();
      builder.Register<MainMenuService>(Lifetime.Singleton).AsSelf();
      builder.Register<PauseMenuService>(Lifetime.Singleton).AsSelf();
      builder.Register<Pooling.Pool>(Lifetime.Singleton)
        .AsSelf()
        .AsImplementedInterfaces();

      var movementSettings = Resources.Load<Movement.MovementSettings>("MovementSettings");
      if (movementSettings == null)
        throw new System.InvalidOperationException("MovementSettings asset was not found in Resources.");

      builder.RegisterInstance(movementSettings);
      builder.RegisterEntryPoint<Movement.MovementUpdater>(Lifetime.Singleton)
        .AsSelf();
    }
  }
}
