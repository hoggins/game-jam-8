using System.Collections;
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

    [Tooltip("Seconds the HUD takes to recede behind a popup, or come back.")]
    [SerializeField, Min(0f)] private float _transitionDuration = 0.2f;

    [Tooltip("Eases the recede over its duration. Sampled from 0..1.")]
    [SerializeField] private AnimationCurve _transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Scale the HUD reaches once it has faded out behind a popup.")]
    [SerializeField, Min(0f)] private float _hiddenScale = 1.2f;

    [Inject] private BattleService _battleService;
    [Inject] private Sfx.SfxService _sfxService;

    private InputAction _subscribedToggleAction;
    private bool _enabledToggleAction;
    private bool _progressionInputEnabled = true;

    private CanvasGroup _canvasGroup;
    private Coroutine _transitionCoroutine;
    private bool _pauseShown;
    private bool _progressionShown;
    private bool _battleOverShown;
    private bool _hudVisible = true;

    private void Awake()
    {
      this.AsInjected();

      _canvasGroup = GetComponent<CanvasGroup>();
      if (_canvasGroup == null)
        _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    protected override void OnEnable()
    {
      base.OnEnable();

      if (_battleService != null)
      {
        _battleService.BattleStarted += OnBattleStarted;
        _battleService.BattleWinStarted += OnBattleWinStarted;
        _battleService.BattleWon += OnBattleOver;
        _battleService.BattleDefeated += OnBattleOver;
        _battleService.BattleAbandoned += OnBattleAbandoned;
      }

      if (_pauseMenuUi != null)
        _pauseMenuUi.ShownChanged += OnPauseShownChanged;

      if (_progressionUi != null)
        _progressionUi.ShownChanged += OnProgressionShownChanged;

      // The popups are siblings that reset themselves on Awake, so the HUD comes up unobscured.
      _pauseShown = _pauseMenuUi != null && _pauseMenuUi.IsPaused;
      _progressionShown = _progressionUi != null && _progressionUi.IsShown;
      ApplyHudVisibility(false);

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
        _battleService.BattleWon -= OnBattleOver;
        _battleService.BattleDefeated -= OnBattleOver;
        _battleService.BattleAbandoned -= OnBattleAbandoned;
      }

      if (_pauseMenuUi != null)
        _pauseMenuUi.ShownChanged -= OnPauseShownChanged;

      if (_progressionUi != null)
        _progressionUi.ShownChanged -= OnProgressionShownChanged;

      if (_transitionCoroutine != null)
      {
        StopCoroutine(_transitionCoroutine);
        _transitionCoroutine = null;
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

    private void OnPauseShownChanged(bool isShown)
    {
      _pauseShown = isShown;
      ApplyHudVisibility(true);
      ApplyAnyScreenOpenState();
    }

    private void OnProgressionShownChanged(bool isShown)
    {
      _progressionShown = isShown;
      ApplyHudVisibility(true);
      ApplyAnyScreenOpenState();
    }

    /// Pause and the progression screen each drive Time.timeScale on their own, so whichever one
    /// closes first restores it to 1 and unfreezes the world behind the other. This is the only
    /// place that sees both, so it re-freezes while either is still up - and holds the battle
    /// music on the same condition, so the track cannot drift out of step with the freeze.
    private void ApplyAnyScreenOpenState()
    {
      var isAnyScreenOpen = _pauseShown || _progressionShown;
      if (isAnyScreenOpen)
        Time.timeScale = 0f;

      _sfxService?.SetMusicPaused(isAnyScreenOpen);
    }

    private void OnBattleOver()
    {
      _battleOverShown = true;
      ApplyHudVisibility(true);
    }

    /// Abandoning tears the battle down without a win or defeat screen, so the latch that
    /// OnBattleOver sets must not survive into whatever the HUD shows next.
    private void OnBattleAbandoned()
    {
      _battleOverShown = false;
      ApplyHudVisibility(true);
    }

    /// The HUD recedes while any popup is up: it fades out, scales up to <see cref="_hiddenScale"/>,
    /// and stops taking clicks so the popup underneath owns the input.
    private void ApplyHudVisibility(bool animate)
    {
      var visible = !_pauseShown && !_progressionShown && !_battleOverShown;
      if (_hudVisible == visible && animate)
        return;

      _hudVisible = visible;

      // Going away, the HUD stops taking clicks at once so the popup owns input immediately.
      // Coming back, input waits for AnimateVisibility to finish, otherwise an invisible HUD
      // swallows clicks meant for the popup that is still fading out.
      if (!visible)
        SetInteractive(false);

      if (_transitionCoroutine != null)
      {
        StopCoroutine(_transitionCoroutine);
        _transitionCoroutine = null;
      }

      if (!animate || _transitionDuration <= 0f || !isActiveAndEnabled)
      {
        SetTransitionState(visible ? 1f : 0f, Vector3.one * (visible ? 1f : _hiddenScale));
        SetInteractive(visible);
        return;
      }

      _transitionCoroutine = StartCoroutine(AnimateVisibility(visible));
    }

    private IEnumerator AnimateVisibility(bool visible)
    {
      var startAlpha = _canvasGroup.alpha;
      var targetAlpha = visible ? 1f : 0f;
      var startScale = transform.localScale;
      var targetScale = Vector3.one * (visible ? 1f : _hiddenScale);

      // Pause and the upgrade screen both drive timeScale to 0, so this cannot use scaled time.
      for (var elapsed = 0f; elapsed < _transitionDuration; elapsed += Time.unscaledDeltaTime)
      {
        var progress = _transitionCurve.Evaluate(Mathf.Clamp01(elapsed / _transitionDuration));
        SetTransitionState(
          Mathf.LerpUnclamped(startAlpha, targetAlpha, progress),
          Vector3.LerpUnclamped(startScale, targetScale, progress));
        yield return null;
      }

      SetTransitionState(targetAlpha, targetScale);
      _transitionCoroutine = null;
      if (visible)
        SetInteractive(true);
    }

    private void SetTransitionState(float alpha, Vector3 scale)
    {
      _canvasGroup.alpha = alpha;
      transform.localScale = scale;
    }

    private void SetInteractive(bool interactive)
    {
      _canvasGroup.interactable = interactive;
      _canvasGroup.blocksRaycasts = interactive;
    }

    private void OnBattleStarted()
    {
      _battleOverShown = false;
      ApplyHudVisibility(true);
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
