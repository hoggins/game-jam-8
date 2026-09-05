using App;
using Model;
using UnityEngine;
using VContainer;

namespace Movement
{
  /// <summary>
  /// Grows the player a little with every max-health upgrade, so a tankier character visibly
  /// occupies more of the field. Scales the whole root, which carries the collider and the weapon
  /// children with it; the flocking radius on <see cref="IMovementController"/> is a plain number
  /// and does not follow.
  /// </summary>
  [DisallowMultipleComponent]
  public sealed class PlayerScale : MonoBehaviour
  {
    [Inject] private CharacterService _characterService;

    private Vector3 _authoredScale;

    private void Awake()
    {
      _authoredScale = transform.localScale;
      this.AsInjected();
    }

    private void OnEnable()
    {
      _characterService.ProgressionChanged += Refresh;
      Refresh();
    }

    private void OnDisable()
    {
      _characterService.ProgressionChanged -= Refresh;
    }

    private void Refresh()
    {
      // Level 1 is the authored size; the growth is bounded by the stat's own level cap, so no
      // separate scale clamp is needed here.
      transform.localScale = _authoredScale * _characterService.CharacterScaleFactor;
    }
  }
}
