using App;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Metagame.PauseMenu
{
  public class PauseMenuUi : MonoBehaviour
  {
    [SerializeField] private GameObject _root;
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _mainMenuButton;

    [Inject] private PauseMenuService _pauseMenuService;

    private void Awake()
    {
      this.AsInjected();
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
      if (_root != null)
        _root.SetActive(isVisible);
    }
  }
}
