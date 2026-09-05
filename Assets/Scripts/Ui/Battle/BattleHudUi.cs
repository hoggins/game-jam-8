using App;
using Metagame.PauseMenu;
using Model;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;

namespace Battle
{
  public class BattleHudUi : UiBase
  {
    [SerializeField] private PauseMenuUi _pauseMenuUi;
    [SerializeField] private InBattleProgressionUi _progressionUi;
    [SerializeField] private Button _upgradeButton;
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
      {
        _battleService.BattleStarted += OnBattleStarted;
        _battleService.BattleWinStarted += OnBattleWinStarted;
      }

      if (_upgradeButton != null)
      {
        _upgradeButton.onClick.AddListener(ToggleProgression);
        _upgradeButton.interactable = _progressionInputEnabled;
      }

      if (_toggleProgressionAction == null)
        return;

      _subscribedToggleAction = _toggleProgressionAction.action;
      _subscribedToggleAction.performed += ToggleProgressionPerformed;

      ApplyProgressionInputState();
    }

    protected override void OnDisable()
    {
      if (_battleService != null)
      {
        _battleService.BattleStarted -= OnBattleStarted;
        _battleService.BattleWinStarted -= OnBattleWinStarted;
      }

      if (_upgradeButton != null)
        _upgradeButton.onClick.RemoveListener(ToggleProgression);

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
      if (_upgradeButton != null)
        _upgradeButton.interactable = enabled;

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

      if (_battleService != null && _battleService.IsWinning)
        return;

      if (_pauseMenuUi != null)
        _pauseMenuUi.TogglePause();
    }

    private void ToggleProgressionPerformed(InputAction.CallbackContext context)
    {
      ToggleProgression();
    }

    private void ToggleProgression()
    {
      if (!_progressionInputEnabled
          || _progressionUi == null
          || (_battleService != null && _battleService.IsWinning))
        return;

      if (_pauseMenuUi != null && _pauseMenuUi.IsPaused)
        return;

      _progressionUi.Toggle();
    }

    private void OnBattleStarted()
    {
      _progressionInputEnabled = true;
      if (_upgradeButton != null)
        _upgradeButton.interactable = true;

      ApplyProgressionInputState();
    }

    private void OnBattleWinStarted()
    {
      _progressionInputEnabled = false;
      if (_upgradeButton != null)
        _upgradeButton.interactable = false;

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
