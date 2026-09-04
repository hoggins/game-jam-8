using System.Collections;
using App;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Metagame.PauseMenu
{
  [RequireComponent(typeof(CanvasGroup))]
  public class PauseMenuUi : MonoBehaviour
  {
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _mainMenuButton;
    [SerializeField, Min(0f)] private float _transitionDuration = 0.2f;
    [SerializeField] private AnimationCurve _transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField, Min(0f)] private float _hiddenScale = 0.9f;

    [Inject] private PauseMenuService _pauseMenuService;
    private CanvasGroup _canvasGroup;
    private Coroutine _transitionCoroutine;

    private void Awake()
    {
      this.AsInjected();
      _pauseMenuService.PauseChanged += SetVisible;

      _canvasGroup = GetComponent<CanvasGroup>();
      if (_canvasGroup == null)
        _canvasGroup = gameObject.AddComponent<CanvasGroup>();

      var isPaused = _pauseMenuService.IsPaused;
      _canvasGroup.interactable = isPaused;
      _canvasGroup.blocksRaycasts = isPaused;
      SetTransitionState(isPaused ? 1f : 0f, Vector3.one * (isPaused ? 1f : _hiddenScale));

      if (!isPaused)
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
      _resumeButton.onClick.AddListener(Resume);
      _mainMenuButton.onClick.AddListener(ToMainMenu);
    }

    private void OnDisable()
    {
      _resumeButton.onClick.RemoveListener(Resume);
      _mainMenuButton.onClick.RemoveListener(ToMainMenu);

      if (_transitionCoroutine == null)
        return;

      StopCoroutine(_transitionCoroutine);
      _transitionCoroutine = null;
    }

    private void OnDestroy()
    {
      _pauseMenuService.PauseChanged -= SetVisible;
    }

    private void Resume() =>
      _pauseMenuService.Resume();

    private void ToMainMenu() =>
      _pauseMenuService.ToMainMenu();

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
