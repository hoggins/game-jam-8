using App;
using Model;
using TMPro;
using UnityEngine;
using VContainer;

namespace Metagame.Stats
{
  public enum StatisticType
  {
    DucksKilled,
    BuildingsDestroyed,
  }

  public class StatisticsItemUi : MonoBehaviour
  {
    [SerializeField] private StatisticType _statisticType;
    [SerializeField] private TMP_Text _valueText;

    [Inject] private Storage _storage;

    private void Awake()
    {
      this.AsInjected();
    }

    private void OnEnable()
    {
      _storage.StatisticsChanged += Refresh;
      Refresh();
    }

    private void OnDisable()
    {
      _storage.StatisticsChanged -= Refresh;
    }

    private void Refresh() =>
      _valueText.text = GetValue().ToString();

    private int GetValue() =>
      _statisticType switch
      {
        StatisticType.DucksKilled => _storage.DucksKilled,
        StatisticType.BuildingsDestroyed => _storage.BuildingsDestroyed,
        _ => 0,
      };
  }
}
