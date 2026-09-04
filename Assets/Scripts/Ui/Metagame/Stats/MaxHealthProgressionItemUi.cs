using App;
using Balance;
using Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Metagame.Stats
{
  public class MaxHealthProgressionItemUi : MonoBehaviour
  {
    [Inject] private readonly CharacterService _characterService;
    [SerializeField] private Button _upgradeButton;
    [SerializeField] private TMP_Text _valueText;
    [SerializeField] private TMP_Text _priceText;

    private void Awake()
    {
      this.AsInjected();
    }

    private void OnEnable()
    {
      _upgradeButton.onClick.AddListener(UpgradeClick);
      _characterService.ProgressionChanged += Refresh;
      Refresh();
    }

    private void OnDisable()
    {
      _upgradeButton.onClick.RemoveListener(UpgradeClick);
      _characterService.ProgressionChanged -= Refresh;
    }

    private void UpgradeClick()
    {
      _characterService.UpgradeMaxHealth();
    }

    private void Refresh()
    {
      _valueText.text = _characterService.MaxHealth.ToString();
      _priceText.text = ProgressionBalance.MaxHealthUpgradeCost.ToString();
      _upgradeButton.interactable = _characterService.CanUpgradeMaxHealth;
    }
  }
}
