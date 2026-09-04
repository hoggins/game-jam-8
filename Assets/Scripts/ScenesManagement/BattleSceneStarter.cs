using App;
using Model;
using UnityEngine;
using VContainer;

namespace ScenesManagement
{
  /// Starts a battle when the scene this lives in becomes active, so BattleScene can be
  /// entered directly. Loading through SceneService also starts one; StartBattle ignores
  /// the second call.
  [DisallowMultipleComponent]
  public sealed class BattleSceneStarter : MonoBehaviour
  {
    [Inject] private BattleService _battleService;

    private void Awake() =>
      this.AsInjected();

    private void Start()
    {
      if (!_battleService.IsBattleActive)
        _battleService.StartBattle();
    }
  }
}
