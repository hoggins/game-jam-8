using Battle;
using Destruction;
using UnityEngine;

namespace Upgrade
{
  /// <summary>
  /// Composite root for the in-world upgrade house. The House01 child is the destructible body and
  /// the camera child mirrors it into the HUD. The camera never moves: unlike the compass arrow,
  /// this object has no changing state to communicate.
  /// </summary>
  [DisallowMultipleComponent]
  public sealed class BattleUpgradeObject : MonoBehaviour
  {
    [Tooltip("The destructible House01 child. Found in children when unset.")]
    [SerializeField] private DestructibleObject _body;

    [Tooltip("The static camera rendering the house into the HUD. Found in children when unset.")]
    [SerializeField] private Camera _hudCamera;

    private bool _isDead;

    private void Awake()
    {
      if (_body == null)
        _body = GetComponentInChildren<DestructibleObject>(true);

      if (_hudCamera == null)
        _hudCamera = GetComponentInChildren<Camera>(true);
    }

    private void OnEnable()
    {
      if (_body != null)
        _body.Destroyed += OnBodyDestroyed;

      // BattleHudUi enables F for a new battle. Do not enable it here: timer respawns can create a
      // replacement Upgrade during the same battle after the original one was destroyed.
    }

    private void OnDisable()
    {
      if (_body != null)
        _body.Destroyed -= OnBodyDestroyed;
    }

    private void Update()
    {
      if (_isDead && !HasDestructibleChildren())
        Destroy(gameObject);
    }

    private void OnBodyDestroyed(DestructibleObject destroyed)
    {
      if (_isDead)
        return;

      _isDead = true;

      // The body remains in the scene while its debris decays, but there is nothing left for the
      // HUD camera to show. Release its render texture immediately rather than keeping a dead HUD
      // entry alive for the duration of the decay.
      if (_hudCamera != null)
        _hudCamera.gameObject.SetActive(false);

      SetProgressionInputEnabled(false);
    }

    private void SetProgressionInputEnabled(bool enabled)
    {
      var hud = FindFirstObjectByType<BattleHudUi>(FindObjectsInactive.Include);
      if (hud != null)
        hud.SetProgressionInputEnabled(enabled);
    }

    private bool HasDestructibleChildren()
    {
      for (var i = 0; i < transform.childCount; i++)
        if (transform.GetChild(i).GetComponent<DestructibleObject>() != null)
          return true;

      return false;
    }
  }
}
