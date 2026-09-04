using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace App
{
  internal static class AppBootstrap
  {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Main()
    {
      Application.quitting += OnQuitting;
      SceneManager.sceneLoaded += OnSceneLoaded;
      _ = AppScope.Instance;

      ReplaceLegacyUiInputModules();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
      ReplaceLegacyUiInputModules();
    }

    private static void ReplaceLegacyUiInputModules()
    {
      StandaloneInputModule[] legacyModules = Object.FindObjectsByType<StandaloneInputModule>(
        FindObjectsInactive.Include,
        FindObjectsSortMode.None);

      foreach (StandaloneInputModule legacyModule in legacyModules)
      {
        legacyModule.enabled = false;

        if (legacyModule.GetComponent<InputSystemUIInputModule>() == null)
          legacyModule.gameObject.AddComponent<InputSystemUIInputModule>();

        Object.Destroy(legacyModule);
      }
    }

    private static void OnQuitting()
    {
      SceneManager.sceneLoaded -= OnSceneLoaded;
      AppScope.Shutdown();
    }
  }
}
