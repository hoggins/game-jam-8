using App;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Metagame.MainMenu
{
  public class MainMenuUi : UiBase
  {
    [SerializeField] private GameObject _mainMenu;
    [SerializeField] private GameObject _progressionMenu;
    [SerializeField] private Button _playButton;
    [SerializeField] private Button _progressionButton;
    [SerializeField] private Button _quitButton;
    [SerializeField] private Button _progressionBackButton;

    [Inject] private MainMenuService _mainMenuService;

    private void Awake()
    {
      this.AsInjected();
    }

    protected override void OnEnable()
    {
      base.OnEnable();
      _playButton.onClick.AddListener(Play);
      _progressionButton.onClick.AddListener(OpenProgression);
      _quitButton.onClick.AddListener(Quit);
      _progressionBackButton.onClick.AddListener(ShowMainMenu);
      ShowMainMenu();
    }

    protected override void OnDisable()
    {
      _playButton.onClick.RemoveListener(Play);
      _progressionButton.onClick.RemoveListener(OpenProgression);
      _quitButton.onClick.RemoveListener(Quit);
      _progressionBackButton.onClick.RemoveListener(ShowMainMenu);
      base.OnDisable();
    }

    private void Play() =>
      _mainMenuService.Play();

    private void Quit() =>
      _mainMenuService.Quit();

    protected override void OnCancel()
    {
      if (_progressionMenu != null && _progressionMenu.activeSelf)
        ShowMainMenu();
    }

    private void ShowMainMenu()
    {
      SetActive(_mainMenu, true);
      SetActive(_progressionMenu, false);
    }

    private void OpenProgression()
    {
      SetActive(_mainMenu, false);
      SetActive(_progressionMenu, true);
    }

    private static void SetActive(GameObject target, bool isActive)
    {
      if (target != null)
        target.SetActive(isActive);
    }
  }
}
