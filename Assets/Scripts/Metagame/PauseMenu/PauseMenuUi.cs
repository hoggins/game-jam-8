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

    [Inject] private PauseMenuService _pauseMenuService;
    private CanvasGroup _canvasGroup;

    private void Awake()
    {
      this.AsInjected();
      _canvasGroup = GetComponent<CanvasGroup>();
      if (_canvasGroup == null)
        _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
      _pauseMenuService.PauseChanged += SetVisible;
      _resumeButton.onClick.AddListener(Resume);
      _mainMenuButton.onClick.AddListener(ToMainMenu);
      SetVisible(_pauseMenuService.IsPaused);
    }

    private void OnDisable()
    {
      _pauseMenuService.PauseChanged -= SetVisible;
      _resumeButton.onClick.RemoveListener(Resume);
      _mainMenuButton.onClick.RemoveListener(ToMainMenu);
    }

    private void Resume() =>
      _pauseMenuService.Resume();

    private void ToMainMenu() =>
      _pauseMenuService.ToMainMenu();

    private void SetVisible(bool isVisible)
    {
      _canvasGroup.alpha = isVisible ? 1f : 0f;
      _canvasGroup.interactable = isVisible;
      _canvasGroup.blocksRaycasts = isVisible;
    }
  }
}
