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

    [Tooltip("Golden duck shown instead of the goal arrow over the first seconds of a battle.")]
    [SerializeField] private CanvasGroup _goldenDuck;

    [Tooltip("Goal arrow, held back until the golden duck has had its turn.")]
    [SerializeField] private CanvasGroup _theGoalArrowPanel;

    [Tooltip("Seconds the golden duck stays up before the goal arrow takes over. Battle time, so pausing does not burn it.")]
    [SerializeField, Min(0f)] private float _goldenDuckDuration = 10f;

    [Inject] private BattleService _battleService;
    [Inject] private Sfx.SfxService _sfxService;

    private InputAction _subscribedToggleAction;
    private bool _enabledToggleAction;
    private bool _progressionInputEnabled = true;

    private CanvasGroup _canvasGroup;
    private Coroutine _transitionCoroutine;
    private bool _pauseShown;
    private bool _progressionShown;
    private CheatsInfoUi _cheatsInfoUi;
    private bool _cheatsInfoShown;
    private bool _battleOverShown;
    private bool _hudVisible = true;
    private Coroutine _goldenDuckCoroutine;

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

      _cheatsInfoUi ??= FindFirstObjectByType<CheatsInfoUi>(FindObjectsInactive.Include);
      if (_cheatsInfoUi != null)
        _cheatsInfoUi.ShownChanged += OnCheatsInfoShownChanged;

      // The popups are siblings that reset themselves on Awake, so the HUD comes up unobscured.
      _pauseShown = _pauseMenuUi != null && _pauseMenuUi.IsPaused;
      _progressionShown = _progressionUi != null && _progressionUi.IsShown;
      _cheatsInfoShown = _cheatsInfoUi != null && _cheatsInfoUi.IsShown;
      ApplyHudVisibility(false);
      ApplyGoldenDuckShown(false);

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

      if (_cheatsInfoUi != null)
        _cheatsInfoUi.ShownChanged -= OnCheatsInfoShownChanged;

      if (_transitionCoroutine != null)
      {
        StopCoroutine(_transitionCoroutine);
        _transitionCoroutine = null;
      }

      StopGoldenDuck();

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

    private void OnCheatsInfoShownChanged(bool isShown)
    {
      _cheatsInfoShown = isShown;
      ApplyHudVisibility(true);
      ApplyAnyScreenOpenState();
    }

    /// Pause, the progression screen, and the cheats screen each drive Time.timeScale on their
    /// own, so whichever one closes first restores it to 1 and unfreezes the world behind the
    /// others. This is the only place that sees all three, so it re-freezes while any is still
    /// up - and hands the same condition to the sfx service, so the music and the queued battle
    /// sounds cannot drift out of step with the freeze.
    private void ApplyAnyScreenOpenState()
    {
      var isAnyScreenOpen = _pauseShown || _progressionShown || _cheatsInfoShown;
      if (isAnyScreenOpen)
        Time.timeScale = 0f;

      _sfxService?.SetBattleScreenOpen(isAnyScreenOpen);
    }

    private void OnBattleOver()
    {
      _battleOverShown = true;
      ApplyHudVisibility(true);
      // A battle that ends inside the duck's window must not hand the arrow back a beat later.
      StopGoldenDuck();
      ApplyGoldenDuckShown(false);
    }

    /// Abandoning tears the battle down without a win or defeat screen, so the latch that
    /// OnBattleOver sets must not survive into whatever the HUD shows next.
    private void OnBattleAbandoned()
    {
      _battleOverShown = false;
      ApplyHudVisibility(true);
      StopGoldenDuck();
      ApplyGoldenDuckShown(false);
    }

    /// The HUD recedes while any popup is up: it fades out, scales up to <see cref="_hiddenScale"/>,
    /// and stops taking clicks so the popup underneath owns the input.
    private void ApplyHudVisibility(bool animate)
    {
      var visible = !_pauseShown && !_progressionShown && !_cheatsInfoShown && !_battleOverShown;
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

      // Pause, the upgrade screen, and the cheats screen all drive timeScale to 0, so this cannot use scaled time.
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
      StartGoldenDuck();
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

    /// The battle opens on the golden duck instead of the goal arrow: the duck fades up, holds for
    /// <see cref="_goldenDuckDuration"/> of battle time, then trades places with the arrow, which
    /// stays for the rest of the battle.
    private void StartGoldenDuck()
    {
      StopGoldenDuck();
      if (_goldenDuck == null && _theGoalArrowPanel == null)
        return;

      if (!isActiveAndEnabled)
      {
        ApplyGoldenDuckShown(false);
        return;
      }

      _goldenDuckCoroutine = StartCoroutine(RunGoldenDuck());
    }

    private void StopGoldenDuck()
    {
      if (_goldenDuckCoroutine == null)
        return;

      StopCoroutine(_goldenDuckCoroutine);
      _goldenDuckCoroutine = null;
    }

    private IEnumerator RunGoldenDuck()
    {
      ApplyGoldenDuckAmount(0f);
      yield return AnimateGoldenDuckSwap(1f);

      // Battle time, so a pause does not eat into the duck's window - unlike the fades below,
      // which are UI and run unscaled so they still play while a popup has frozen the world.
      yield return new WaitForSeconds(_goldenDuckDuration);

      yield return AnimateGoldenDuckSwap(0f);
      _goldenDuckCoroutine = null;
    }

    /// Crossfades the pair towards <paramref name="targetDuckAmount"/> - 1 duck up and arrow down,
    /// 0 the other way around - both in the same loop so neither leads the other.
    private IEnumerator AnimateGoldenDuckSwap(float targetDuckAmount)
    {
      var startDuckAmount = 1f - targetDuckAmount;
      if (_transitionDuration > 0f)
      {
        for (var elapsed = 0f; elapsed < _transitionDuration; elapsed += Time.unscaledDeltaTime)
        {
          var progress = _transitionCurve.Evaluate(Mathf.Clamp01(elapsed / _transitionDuration));
          ApplyGoldenDuckAmount(Mathf.LerpUnclamped(startDuckAmount, targetDuckAmount, progress));
          yield return null;
        }
      }

      ApplyGoldenDuckAmount(targetDuckAmount);
    }

    private void ApplyGoldenDuckShown(bool shown) => ApplyGoldenDuckAmount(shown ? 1f : 0f);

    /// Drives both groups from one value: at 1 the duck sits at alpha 1 and scale 1 while the arrow
    /// is faded out at <see cref="_hiddenScale"/>, and at 0 the roles are swapped.
    private void ApplyGoldenDuckAmount(float duckAmount)
    {
      ApplySwapState(_goldenDuck, duckAmount);
      ApplySwapState(_theGoalArrowPanel, 1f - duckAmount);
    }

    private void ApplySwapState(CanvasGroup group, float shownAmount)
    {
      if (group == null)
        return;

      group.alpha = shownAmount;
      group.transform.localScale = Vector3.one * Mathf.LerpUnclamped(_hiddenScale, 1f, shownAmount);
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
