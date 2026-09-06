using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Battle
{
  [DisallowMultipleComponent]
  public sealed class CheatsInfoUi : MonoBehaviour
  {
    private const string CheatsInfo =
      "<b>CHEATS</b>\n"
      // + "<b>F2</b>  Toggle cheats info\n"
      + "<b>F3</b>  Reset all player progression\n"
      + "<b>F4</b>  Destroy battle timer "
      + "<b>F5</b>  Teleport to goal "
      + "<b>F6</b>  Add coins\n"
      // + "<b>F8</b>  Teleport to configured position\n"
      // + "<b>F9</b>  Teleport to origin\n"
      + "<b>F10</b>  Player invincibility "
      + "<b>F11</b>  Infinite timer";

    [SerializeField] private TMP_Text _infoText;

    private CanvasGroup _canvasGroup;
    private Button _mainMenuButton;
    private bool _isOpen;

    public event Action<bool> ShownChanged;

    public bool IsShown => _isOpen;

    private void Awake()
    {
      EnsureCanvasGroup();

      _mainMenuButton = FindMainMenuButton();
      ApplyVisibility(false);
    }

    private void OnEnable()
    {
      if (_infoText != null)
        _infoText.text = CheatsInfo;

      _mainMenuButton?.onClick.AddListener(Close);
      ApplyVisibility(_isOpen);
    }

    private void OnDisable()
    {
      _mainMenuButton?.onClick.RemoveListener(Close);

      if (!_isOpen)
        return;

      _isOpen = false;
      Time.timeScale = 1f;
      ShownChanged?.Invoke(false);
    }

    private void OnDestroy()
    {
      if (_isOpen)
        Time.timeScale = 1f;
    }

    public void Toggle()
    {
      if (!_isOpen && !gameObject.activeSelf)
        gameObject.SetActive(true);

      SetVisible(!_isOpen);
    }

    private void Close() => SetVisible(false);

    private void SetVisible(bool isVisible)
    {
      EnsureCanvasGroup();

      if (_isOpen == isVisible)
      {
        ApplyVisibility(isVisible);
        return;
      }

      _isOpen = isVisible;
      Time.timeScale = isVisible ? 0f : 1f;
      ApplyVisibility(isVisible);
      ShownChanged?.Invoke(isVisible);
    }

    private void ApplyVisibility(bool isVisible)
    {
      _canvasGroup.alpha = isVisible ? 1f : 0f;
      _canvasGroup.interactable = isVisible;
      _canvasGroup.blocksRaycasts = isVisible;
    }

    private void EnsureCanvasGroup()
    {
      if (_canvasGroup != null)
        return;

      _canvasGroup = GetComponent<CanvasGroup>();
      if (_canvasGroup == null)
        _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private Button FindMainMenuButton()
    {
      var buttons = GetComponentsInChildren<Button>(true);
      for (var i = 0; i < buttons.Length; i++)
      {
        if (buttons[i].name == "MainMenuButton")
          return buttons[i];
      }

      return buttons.Length == 1 ? buttons[0] : null;
    }
  }
}
