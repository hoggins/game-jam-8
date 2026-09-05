using Balance;
using CustomCamera;
using Destruction;
using Map;
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

      var battleBalance = Resources.Load<BattleBalanceConfig>("BattleBalanceConfig");
      if (battleBalance == null)
        throw new System.InvalidOperationException("BattleBalanceConfig asset was not found in Resources.");

      builder.RegisterInstance(battleBalance);

      var progressionBalance = Resources.Load<ProgressionBalanceConfig>("ProgressionBalanceConfig");
      if (progressionBalance == null)
        throw new System.InvalidOperationException("ProgressionBalanceConfig asset was not found in Resources.");

      builder.RegisterInstance(progressionBalance);

      builder.Register<SceneService>(Lifetime.Singleton)
        .AsSelf()
        .AsImplementedInterfaces();

      builder.Register<Storage>(Lifetime.Singleton)
        .AsSelf()
        .AsImplementedInterfaces();

      builder.Register<BattleService>(Lifetime.Singleton)
        .AsSelf()
        .AsImplementedInterfaces();
      builder.Register<CharacterService>(Lifetime.Singleton)
        .AsSelf()
        .AsImplementedInterfaces();
      builder.Register<ArrowService>(Lifetime.Singleton)
        .AsSelf()
        .AsImplementedInterfaces();
      builder.Register<Pooling.Pool>(Lifetime.Singleton)
        .AsSelf()
        .AsImplementedInterfaces();
      builder.Register<SceneHud.SceneHudService>(Lifetime.Singleton)
        .AsSelf()
        .AsImplementedInterfaces();
      builder.Register<CameraDistanceController>(Lifetime.Singleton)
        .AsSelf()
        .AsImplementedInterfaces();
      builder.Register<Sfx.UiSfxService>(Lifetime.Singleton)
        .AsSelf()
        .AsImplementedInterfaces();

      var movementSettings = Resources.Load<Movement.MovementSettings>("MovementSettings");
      if (movementSettings == null)
        throw new System.InvalidOperationException("MovementSettings asset was not found in Resources.");

      builder.RegisterInstance(movementSettings);
      builder.RegisterEntryPoint<Movement.MovementUpdater>(Lifetime.Singleton)
        .AsSelf();

      RegisterMap(builder);
    }

    private static void RegisterMap(IContainerBuilder builder)
    {
      var decaySettings = Resources.Load<EnvironmentDecaySettings>("EnvironmentDecaySettings");
      if (decaySettings == null)
        throw new System.InvalidOperationException("EnvironmentDecaySettings asset was not found in Resources.");

      builder.RegisterInstance(decaySettings);
      builder.RegisterEntryPoint<EnvironmentDecayManager>(Lifetime.Singleton)
        .AsSelf();

      var visibilitySettings = Resources.Load<Map.EnvironmentVisibilitySettings>("EnvironmentVisibilitySettings");
      if (visibilitySettings == null)
        throw new System.InvalidOperationException("EnvironmentVisibilitySettings asset was not found in Resources.");

      builder.RegisterInstance(visibilitySettings);
      builder.Register<MapEnvironmentSpawner>(Lifetime.Singleton)
        .AsSelf()
        .AsImplementedInterfaces();

      var specialSpawnSettings = Resources.Load<Map.SpecialSpawnSettings>("SpecialSpawnSettings");
      if (specialSpawnSettings == null)
        throw new System.InvalidOperationException("SpecialSpawnSettings asset was not found in Resources.");

      builder.RegisterInstance(specialSpawnSettings);
      builder.RegisterEntryPoint<Timer.TimerRespawnService>(Lifetime.Singleton).AsSelf();
    }
  }
}
