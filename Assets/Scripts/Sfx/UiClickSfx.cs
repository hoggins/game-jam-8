using App;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Sfx
{
  /// Plays the shared UI click sound for the Button on this GameObject.
  [RequireComponent(typeof(Button))]
  public class UiClickSfx : MonoBehaviour
  {
    [Inject] private SfxService _sfxService;

    private Button _button;

    private void Awake()
    {
      this.AsInjected();
      _button = GetComponent<Button>();
    }

    private void OnEnable()
    {
      _button.onClick.AddListener(PlayClick);
    }

    private void OnDisable()
    {
      _button.onClick.RemoveListener(PlayClick);
    }

    private void PlayClick()
    {
      _sfxService.PlayButtonClick();
    }
  }
}
