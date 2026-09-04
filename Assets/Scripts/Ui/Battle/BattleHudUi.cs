using App;
using Metagame.PauseMenu;
using UnityEngine;

namespace Battle
{
  public class BattleHudUi : UiBase
  {
    [SerializeField] private PauseMenuUi _pauseMenuUi;

    protected override void OnCancel()
    {
      if (_pauseMenuUi != null)
        _pauseMenuUi.TogglePause();
    }
  }
}
