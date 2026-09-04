using ScenesManagement;
using UnityEngine;

namespace Metagame.MainMenu
{
  [UnityEngine.Scripting.Preserve]
  public class MainMenuService
  {
    private readonly SceneService _sceneService;

    public MainMenuService(SceneService sceneService)
    {
      _sceneService = sceneService;
    }

    public void Play()
    {
      _sceneService.LoadBattleScene();
    }

    public void Quit()
    {
      Application.Quit();
    }
  }
}
