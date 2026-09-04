using System;
using ScenesManagement;
using UnityEngine;
using VContainer;

namespace Metagame.PauseMenu
{
  [UnityEngine.Scripting.Preserve]
  public class PauseMenuService
  {
    [Inject] private readonly SceneService _sceneService;

    public event Action<bool> PauseChanged;

    public bool IsPaused { get; private set; }

    public void Pause() =>
      SetPaused(true);

    public void Resume() =>
      SetPaused(false);

    public void ToMainMenu()
    {
      Resume();
      _sceneService.LoadMainMenuScene();
    }

    private void SetPaused(bool isPaused)
    {
      if (IsPaused == isPaused)
        return;

      IsPaused = isPaused;
      Time.timeScale = isPaused ? 0f : 1f;
      PauseChanged?.Invoke(isPaused);
    }
  }
}
