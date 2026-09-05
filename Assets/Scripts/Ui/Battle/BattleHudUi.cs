using App;
using Metagame.PauseMenu;
using Model;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace Battle
{
  public class BattleHudUi : UiBase
  {
    [SerializeField] private PauseMenuUi _pauseMenuUi;
    [SerializeField] private InBattleProgressionUi _progressionUi;
    [SerializeField] private InputActionReference _toggleProgressionAction;

    [Inject] private BattleService _battleService;

    private InputAction _subscribedToggleAction;
    private bool _enabledToggleAction;
    private bool _progressionInputEnabled = true;

    private void Awake() => this.AsInjected();

    protected override void OnEnable()
    {
      base.OnEnable();

      if (_battleService != null)
        _battleService.BattleStarted += OnBattleStarted;

      if (_toggleProgressionAction == null)
        return;

      _subscribedToggleAction = _toggleProgressionAction.action;
      _subscribedToggleAction.performed += ToggleProgressionPerformed;

      ApplyProgressionInputState();
    }

    protected override void OnDisable()
    {
      if (_battleService != null)
        _battleService.BattleStarted -= OnBattleStarted;

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

    /// Enables or disables the in-battle progression shortcut. The Upgrade house owns when this is
    /// available: while it stands, F opens the progression screen; after it is destroyed the
    /// shortcut is disabled for the rest of the current battle.
    public void SetProgressionInputEnabled(bool enabled)
    {
      _progressionInputEnabled = enabled;
      ApplyProgressionInputState();
    }

    /// Cancel closes the progression screen when it is open, and otherwise toggles pause.
    /// Both screens drive Time.timeScale, so only one of them may react to a single press.
    protected override void OnCancel()
    {
      if (_progressionUi != null && _progressionUi.IsShown)
      {
        _progressionUi.Hide();
        return;
      }

      if (_pauseMenuUi != null)
        _pauseMenuUi.TogglePause();
    }

    private void ToggleProgressionPerformed(InputAction.CallbackContext context)
    {
      if (_progressionUi == null)
        return;

      if (_pauseMenuUi != null && _pauseMenuUi.IsPaused)
        return;

      _progressionUi.Toggle();
    }

    private void OnBattleStarted()
    {
      _progressionInputEnabled = true;
      ApplyProgressionInputState();
    }

    private void ApplyProgressionInputState()
    {
      if (_subscribedToggleAction == null)
        return;

      if (!_progressionInputEnabled)
      {
        _subscribedToggleAction.Disable();
        return;
      }

      if (!_subscribedToggleAction.enabled)
      {
        _subscribedToggleAction.Enable();
        _enabledToggleAction = true;
      }
    }
  }
}
