using App;
using Metagame.PauseMenu;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Battle
{
  public class BattleHudUi : UiBase
  {
    [SerializeField] private PauseMenuUi _pauseMenuUi;
    [SerializeField] private InBattleProgressionUi _progressionUi;
    [SerializeField] private InputActionReference _toggleProgressionAction;

    private InputAction _subscribedToggleAction;
    private bool _enabledToggleAction;

    protected override void OnEnable()
    {
      base.OnEnable();

      if (_toggleProgressionAction == null)
        return;

      _subscribedToggleAction = _toggleProgressionAction.action;
      _subscribedToggleAction.performed += ToggleProgressionPerformed;

      _enabledToggleAction = !_subscribedToggleAction.enabled;
      if (_enabledToggleAction)
        _subscribedToggleAction.Enable();
    }

    protected override void OnDisable()
    {
      if (_subscribedToggleAction != null)
      {
        _subscribedToggleAction.performed -= ToggleProgressionPerformed;
        if (_enabledToggleAction)
          _subscribedToggleAction.Disable();

        _subscribedToggleAction = null;
        _enabledToggleAction = false;
      }

      base.OnDisable();
    }

    protected override void OnCancel()
    {
      if (_pauseMenuUi != null)
        _pauseMenuUi.TogglePause();
    }

    private void ToggleProgressionPerformed(InputAction.CallbackContext context)
    {
      if (_progressionUi != null)
        _progressionUi.Toggle();
    }
  }
}
