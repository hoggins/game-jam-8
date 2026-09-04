using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using VContainer.Unity;

namespace ScenesManagement
{
  [Preserve]
  public class SceneService : IInitializable, ITickable, IDisposable
  {
    private const string BattleSceneName = "BattleScene";
    private const string MainMenuSceneName = "MainMenuScene";
    private const string LoadingUiResourcePath = "Prefabs/SceneLoadingUi";

    private SceneLoadingUi _sceneLoadingUi;
    private AsyncOperation _loadingOperation;
    private bool _isLoading;

    public void LoadBattleScene()
    {
      LoadScene(BattleSceneName);
    }

    public void LoadMainMenuScene()
    {
      LoadScene(MainMenuSceneName);
    }

    public void LoadScene(string sceneName)
    {
      if (string.IsNullOrWhiteSpace(sceneName))
        throw new ArgumentException("Scene name cannot be empty.", nameof(sceneName));

      if (_isLoading)
        return;

      if (!Application.CanStreamedLevelBeLoaded(sceneName))
        throw new ArgumentException($"Scene '{sceneName}' is not included in the build settings.", nameof(sceneName));

      _sceneLoadingUi?.SetProgress(0f);
      _sceneLoadingUi?.EnableLoading(true);

      _loadingOperation = SceneManager.LoadSceneAsync(sceneName);
      if (_loadingOperation == null)
      {
        _sceneLoadingUi?.EnableLoading(false);
        throw new InvalidOperationException($"Unity could not start loading scene '{sceneName}'.");
      }

      _isLoading = true;
    }

    void IInitializable.Initialize()
    {
      SceneLoadingUi prefab = Resources.Load<SceneLoadingUi>(LoadingUiResourcePath);
      if (prefab == null)
      {
        Debug.LogWarning(
          $"Loading UI was not found at Assets/Resources/{LoadingUiResourcePath}.prefab. " +
          "Scenes will still load without a progress overlay.");
        return;
      }

      _sceneLoadingUi = UnityEngine.Object.Instantiate(prefab);
      _sceneLoadingUi.name = nameof(SceneLoadingUi);
      UnityEngine.Object.DontDestroyOnLoad(_sceneLoadingUi.gameObject);
      _sceneLoadingUi.EnableLoading(false);
    }

    void ITickable.Tick()
    {
      if (!_isLoading || _loadingOperation == null)
        return;

      float progress = Mathf.Clamp01(_loadingOperation.progress / 0.9f);
      _sceneLoadingUi?.SetProgress(progress);

      if (!_loadingOperation.isDone)
        return;

      _sceneLoadingUi?.SetProgress(1f);
      _sceneLoadingUi?.EnableLoading(false);
      _loadingOperation = null;
      _isLoading = false;
    }

    void IDisposable.Dispose()
    {
      if (_sceneLoadingUi != null)
        UnityEngine.Object.Destroy(_sceneLoadingUi.gameObject);

      _sceneLoadingUi = null;
      _loadingOperation = null;
      _isLoading = false;
    }
  }
}
