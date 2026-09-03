using UnityEngine;

namespace App
{
  internal static class AppBootstrap
  {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Main()
    {
      Application.quitting += OnQuitting;
      _ = AppScope.Instance;
    }

    private static void OnQuitting()
    {
      AppScope.Shutdown();
    }
  }
}
