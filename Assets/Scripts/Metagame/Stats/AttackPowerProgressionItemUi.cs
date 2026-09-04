using App;
using Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Metagame.Stats
{
  public class AttackPowerProgressionItemUi : MonoBehaviour
  {
    [Inject] private readonly CharacterService _characterService;
    [SerializeField] private Button _upgradeButton;
    [SerializeField] private TMP_Text _valueText;

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

    private void UpgradeClick() =>
      _characterService.UpgradeAttackPower();

    private void Refresh()
    {
      _valueText.text = _characterService.AttackPower.ToString();
      _upgradeButton.interactable = _characterService.CanUpgradeAttackPower;
    }
  }
}
