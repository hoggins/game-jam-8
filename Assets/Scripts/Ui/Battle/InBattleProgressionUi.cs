using System.Collections;
using App;
using Telemetry;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Battle
{
  [RequireComponent(typeof(CanvasGroup))]
  public class InBattleProgressionUi : MonoBehaviour
  {
    [SerializeField] private Button _closeButton;
    [SerializeField, Min(0f)] private float _transitionDuration = 0.2f;
    [SerializeField] private AnimationCurve _transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField, Min(0f)] private float _hiddenScale = 1.2f;

    private CanvasGroup _canvasGroup;
    private Coroutine _transitionCoroutine;

    [Inject] private EconomyTelemetryService _telemetry;

    public bool IsShown { get; private set; }

    public void Show() =>
      SetShown(true);

    public void Hide() =>
      SetShown(false);

    public void Toggle() =>
      SetShown(!IsShown);

    private void Awake()
    {
      this.AsInjected();
      _canvasGroup = GetComponent<CanvasGroup>();
      if (_canvasGroup == null)
        _canvasGroup = gameObject.AddComponent<CanvasGroup>();

      _canvasGroup.interactable = false;
      _canvasGroup.blocksRaycasts = false;
      SetTransitionState(0f, Vector3.one * _hiddenScale);
      gameObject.SetActive(false);
    }

    private void OnEnable()
    {
      if (_closeButton != null)
        _closeButton.onClick.AddListener(Hide);
    }

    private void OnDisable()
    {
      if (_closeButton != null)
        _closeButton.onClick.RemoveListener(Hide);

      if (_transitionCoroutine == null)
        return;

      StopCoroutine(_transitionCoroutine);
      _transitionCoroutine = null;
    }

    private void OnDestroy()
    {
      if (IsShown)
        Time.timeScale = 1f;
    }

    private void SetShown(bool isShown)
    {
      if (IsShown == isShown)
        return;

      IsShown = isShown;
      _telemetry?.SetUpgradeUiShown(isShown);
      Time.timeScale = isShown ? 0f : 1f;
      SetVisible(isShown);
    }

    private void SetVisible(bool isVisible)
    {
      if (isVisible && !gameObject.activeSelf)
        gameObject.SetActive(true);

      if (_transitionCoroutine != null)
        StopCoroutine(_transitionCoroutine);

      _canvasGroup.interactable = isVisible;
      _canvasGroup.blocksRaycasts = isVisible;
      _transitionCoroutine = StartCoroutine(AnimateVisibility(isVisible));
    }

    private IEnumerator AnimateVisibility(bool isVisible)
    {
      var startAlpha = _canvasGroup.alpha;
      var targetAlpha = isVisible ? 1f : 0f;
      var startScale = transform.localScale;
      var targetScale = Vector3.one * (isVisible ? 1f : _hiddenScale);

      if (_transitionDuration <= 0f)
      {
        SetTransitionState(targetAlpha, targetScale);
        _transitionCoroutine = null;
        if (!isVisible)
          gameObject.SetActive(false);

        yield break;
      }

      var elapsed = 0f;
      while (elapsed < _transitionDuration)
      {
        elapsed += Time.unscaledDeltaTime;
        var progress = Mathf.Clamp01(elapsed / _transitionDuration);
        var curvedProgress = _transitionCurve.Evaluate(progress);
        SetTransitionState(
          Mathf.LerpUnclamped(startAlpha, targetAlpha, curvedProgress),
          Vector3.LerpUnclamped(startScale, targetScale, curvedProgress));
        yield return null;
      }

      SetTransitionState(targetAlpha, targetScale);
      _transitionCoroutine = null;
      if (!isVisible)
        gameObject.SetActive(false);
    }

    private void SetTransitionState(float alpha, Vector3 scale)
    {
      _canvasGroup.alpha = alpha;
      transform.localScale = scale;
    }
  }
}
