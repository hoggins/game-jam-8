using App;
using Model;
using TMPro;
using UnityEngine;
using VContainer;

namespace Metagame.Stats
{
  public class CoinsCountUi : MonoBehaviour
  {
    [SerializeField] private TMP_Text _coinsCountText;

    [Inject] private CharacterService _characterService;

    private void Awake()
    {
      this.AsInjected();
    }

    private void OnEnable()
    {
      _characterService.CoinsChanged += UpdateCoinsCount;
      UpdateCoinsCount(_characterService.CurrentCoins);
    }

    private void OnDisable()
    {
      _characterService.CoinsChanged -= UpdateCoinsCount;
    }

    private void UpdateCoinsCount(int coinsCount)
    {
      _coinsCountText.text = coinsCount.ToString();
    }
  }
}
