using System.Collections;
using App;
using Metagame.MainMenu;
using Model;
using ScenesManagement;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Battle
{
  public sealed class BattleWinUi : MonoBehaviour
  {
    [SerializeField] private GameObject _content;
    [SerializeField] private Button _continueButton;
    [SerializeField, Min(0f)] private float _transitionDuration = 0.2f;
    [SerializeField] private AnimationCurve _transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField, Min(0f)] private float _hiddenScale = 0.9f;

    [Inject] private BattleService _battleService;
    [Inject] private SceneService _sceneService;

    private CanvasGroup _canvasGroup;
    private Coroutine _transitionCoroutine;

    private void Awake()
    {
      this.AsInjected();
      _battleService.BattleWon += Show;
      _continueButton.onClick.AddListener(Continue);

      if (_content != null)
      {
        _canvasGroup = _content.GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
          _canvasGroup = _content.AddComponent<CanvasGroup>();
      }

      SetTransitionState(0f, Vector3.one * _hiddenScale);
      SetInteractable(false);

      if (_content != null)
        _content.SetActive(false);
    }

    private void OnDestroy()
    {
      _battleService.BattleWon -= Show;
      _continueButton.onClick.RemoveListener(Continue);
    }

    private void Show() => SetVisible(true);

    private void Continue()
    {
      _continueButton.interactable = false;
      MainMenuUi.RequestProgression();
      _sceneService.LoadMainMenuScene();
    }

    private void SetVisible(bool isVisible)
    {
      if (isVisible && _content != null && !_content.activeSelf)
        _content.SetActive(true);

      SetInteractable(isVisible);

      if (_transitionCoroutine != null)
      {
        StopCoroutine(_transitionCoroutine);
        _transitionCoroutine = null;
      }

      var targetScale = Vector3.one * (isVisible ? 1f : _hiddenScale);
      if (_content == null || _transitionDuration <= 0f || !gameObject.activeInHierarchy)
      {
        SetTransitionState(isVisible ? 1f : 0f, targetScale);
        if (!isVisible && _content != null)
          _content.SetActive(false);

        return;
      }

      _transitionCoroutine = StartCoroutine(AnimateVisibility(isVisible, targetScale));
    }

    private IEnumerator AnimateVisibility(bool isVisible, Vector3 targetScale)
    {
      var startAlpha = _canvasGroup != null ? _canvasGroup.alpha : 0f;
      var targetAlpha = isVisible ? 1f : 0f;
      var startScale = _content.transform.localScale;

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
        _content.SetActive(false);
    }

    private void SetInteractable(bool isInteractable)
    {
      _continueButton.interactable = isInteractable;

      if (_canvasGroup == null)
        return;

      _canvasGroup.interactable = isInteractable;
      _canvasGroup.blocksRaycasts = isInteractable;
    }

    private void SetTransitionState(float alpha, Vector3 scale)
    {
      if (_canvasGroup != null)
        _canvasGroup.alpha = alpha;

      if (_content != null)
        _content.transform.localScale = scale;
    }
  }
}
