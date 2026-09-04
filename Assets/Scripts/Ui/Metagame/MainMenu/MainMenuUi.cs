using App;
using ScenesManagement;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Metagame.MainMenu
{
  public class MainMenuUi : UiBase
  {
    private static bool _openProgressionRequested;

    /// Asks the next MainMenuUi to show the progression screen instead of the main menu.
    /// Call it before loading MainMenuScene - it is consumed once the screen is enabled.
    public static void RequestProgression() =>
      _openProgressionRequested = true;

    [SerializeField] private GameObject _mainMenu;
    [SerializeField] private GameObject _progressionMenu;
    [SerializeField] private Button _playButton;
    [SerializeField] private Button _progressionButton;
    [SerializeField] private Button _quitButton;
    [SerializeField] private Button _progressionBackButton;
    [SerializeField] private Button _progressionPlayButton;

    [Inject] private SceneService _sceneService;

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
      if (_progressionPlayButton != null)
        _progressionPlayButton.onClick.AddListener(Play);

      if (ConsumeOpenProgressionRequest())
        OpenProgression();
      else
        ShowMainMenu();
    }

    protected override void OnDisable()
    {
      _playButton.onClick.RemoveListener(Play);
      _progressionButton.onClick.RemoveListener(OpenProgression);
      _quitButton.onClick.RemoveListener(Quit);
      _progressionBackButton.onClick.RemoveListener(ShowMainMenu);
      if (_progressionPlayButton != null)
        _progressionPlayButton.onClick.RemoveListener(Play);
      base.OnDisable();
    }

    protected override void OnCancel()
    {
      if (_progressionMenu != null && _progressionMenu.activeSelf)
        ShowMainMenu();
    }

    private void Play() =>
      _sceneService.LoadBattleScene();

    private static void Quit() =>
      Application.Quit();

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

    private static bool ConsumeOpenProgressionRequest()
    {
      if (!_openProgressionRequested)
        return false;

      _openProgressionRequested = false;
      return true;
    }

    private static void SetActive(GameObject target, bool isActive)
    {
      if (target != null)
        target.SetActive(isActive);
    }
  }
}
