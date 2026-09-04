using App;
using Metagame.PauseMenu;
using VContainer;

namespace Battle
{
  public class BattleHudUi : UiBase
  {
    [Inject] private PauseMenuService _pauseMenuService;

    private void Awake()
    {
      this.AsInjected();
    }

    protected override void OnCancel()
    {
      if (_pauseMenuService.IsPaused)
        _pauseMenuService.Resume();
      else
        _pauseMenuService.Pause();
    }
  }
}
