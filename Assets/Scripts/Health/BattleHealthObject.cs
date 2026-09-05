using Destruction;
using UnityEngine;

namespace Health
{
  /// <summary>
  /// The root of the in-world health bar. It carries the HUD camera and nothing else of substance —
  /// the bar's behaviour lives on <see cref="BattleHealthBar"/>, on the destructible group below.
  ///
  /// This exists for the same reason its opposite numbers on the timer and the arrow do: the decay
  /// manager retires the destructible group once its parts have decayed, but nothing owns the
  /// composite root, so without this the world would keep a husk holding a live camera and a render
  /// texture for every battle played.
  /// </summary>
  [DisallowMultipleComponent]
  public sealed class BattleHealthObject : MonoBehaviour
  {
    [Tooltip("The destructible group holding the bar's pixels. Found in children when unset.")]
    [SerializeField] private DestructibleObject _body;

    private GameObject _hudCameraObject;
    private bool _isDead;

    private void Awake()
    {
      WarnIfRootIsDestructible();

      if (_body == null)
        _body = GetComponentInChildren<DestructibleObject>(true);

      var camera = GetComponentInChildren<Camera>(true);
      _hudCameraObject = camera != null ? camera.gameObject : null;
    }

    private void OnEnable()
    {
      if (_body != null)
        _body.Destroyed += OnBodyDestroyed;
    }

    private void OnDisable()
    {
      if (_body != null)
        _body.Destroyed -= OnBodyDestroyed;
    }

    private void Update()
    {
      if (!_isDead)
        return;

      if (!HasDestructibleChildren())
        Destroy(gameObject);
    }

    private void OnBodyDestroyed(DestructibleObject destroyed)
    {
      if (_isDead)
        return;

      _isDead = true;

      // One camera and one render texture per live element, and they are not free: shut it down as
      // soon as there is nothing left to show rather than waiting for the debris to decay.
      if (_hudCameraObject != null)
        _hudCameraObject.SetActive(false);
    }

    private bool HasDestructibleChildren()
    {
      for (var i = 0; i < transform.childCount; i++)
        if (transform.GetChild(i).GetComponent<DestructibleObject>() != null)
          return true;

      return false;
    }

    private void OnValidate() => WarnIfRootIsDestructible();

    /// <summary>
    /// The bar is one destructible group hanging off this root, and this root carries the HUD camera.
    /// A <see cref="DestructibleObject"/> here instead would shatter all 24 pixels in a single hit
    /// without <see cref="BattleHealthBar"/> ever seeing them go — the player would lose the bar
    /// without losing the health it stands for. Tools > Destruction > Destructible Object Setup adds
    /// exactly that, so say so out loud instead of just misbehaving.
    /// </summary>
    private void WarnIfRootIsDestructible()
    {
      if (GetComponent<DestructibleObject>() == null && GetComponent<DestructibleHealth>() == null)
        return;

      Debug.LogError(
        $"{name}: the health bar root carries a DestructibleObject/DestructibleHealth, which breaks "
        + "all 24 pixels in one hit without charging the player for them. Remove them from the "
        + "prefab root, or rebuild it with Tools > Destruction > Rebuild Battle Health Prefab.",
        this);
    }
  }
}
