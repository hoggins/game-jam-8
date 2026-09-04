using App;
using Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Metagame.MainMenu
{
  public class MainMenuUi : MonoBehaviour
  {
    [SerializeField] private GameObject _mainMenu;
    [SerializeField] private GameObject _progressionMenu;
    [SerializeField] private Button _playButton;
    [SerializeField] private Button _progressionButton;
    [SerializeField] private Button _quitButton;
    [SerializeField] private Button _progressionBackButton;
    [SerializeField] private TMP_Text _ducksKilledValue;
    [SerializeField] private TMP_Text _buildingsDestroyedValue;
    [SerializeField] private TMP_Text _coinsValue;

    [Inject] private MainMenuService _mainMenuService;
    [Inject] private Storage _storage;

    private void Awake()
    {
      this.AsInjected();
    }

    private void OnEnable()
    {
      _playButton.onClick.AddListener(Play);
      _progressionButton.onClick.AddListener(OpenProgression);
      _quitButton.onClick.AddListener(Quit);
      _progressionBackButton.onClick.AddListener(ShowMainMenu);
      ShowMainMenu();
    }

    private void OnDisable()
    {
      _playButton.onClick.RemoveListener(Play);
      _progressionButton.onClick.RemoveListener(OpenProgression);
      _quitButton.onClick.RemoveListener(Quit);
      _progressionBackButton.onClick.RemoveListener(ShowMainMenu);
    }

    private void Play() =>
      _mainMenuService.Play();

    private void Quit() =>
      _mainMenuService.Quit();

    private void ShowMainMenu()
    {
      SetActive(_mainMenu, true);
      SetActive(_progressionMenu, false);
    }

    private void OpenProgression()
    {
      SetText(_ducksKilledValue, _storage.DucksKilled);
      SetText(_buildingsDestroyedValue, _storage.BuildingsDestroyed);
      SetText(_coinsValue, _storage.CurrentCoins);
      SetActive(_mainMenu, false);
      SetActive(_progressionMenu, true);
    }

    private static void SetActive(GameObject target, bool isActive)
    {
      if (target != null)
        target.SetActive(isActive);
    }

    private static void SetText(TMP_Text target, int value)
    {
      if (target != null)
        target.text = value.ToString();
    }
  }
}
