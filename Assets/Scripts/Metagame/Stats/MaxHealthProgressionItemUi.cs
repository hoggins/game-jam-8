using App;
using Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Metagame.Stats
{
  public class MaxHealthProgressionItemUi : MonoBehaviour
  {
    [Inject] private readonly PlayerService _playerService;
    [SerializeField] private Button _upgradeButton;
    [SerializeField] private TMP_Text _valueText;

    private void Awake()
    {
      this.AsInjected();
    }

    private void OnEnable()
    {
      _upgradeButton.onClick.AddListener(UpgradeClick);
      _playerService.ProgressionChanged += Refresh;
      Refresh();
    }

    private void OnDisable()
    {
      _upgradeButton.onClick.RemoveListener(UpgradeClick);
      _playerService.ProgressionChanged -= Refresh;
    }

    private void UpgradeClick()
    {
      _playerService.UpgradeMaxHealth();
    }

    private void Refresh()
    {
      _valueText.text = _playerService.MaxHealth.ToString();
      _upgradeButton.interactable = _playerService.CanUpgradeMaxHealth;
    }
  }
}
