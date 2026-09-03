using App.TmpTest;
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
      builder.Register<ScopeLogger>(Lifetime.Singleton)
        .AsSelf()
        .AsImplementedInterfaces();
    }
  }
}