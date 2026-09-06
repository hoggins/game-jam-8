using Destruction;
using Movement;
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
    private BoxCollider _barCollider;
    private FlowMapNoGoZone _barNoGoZone;
    private DecayPart[] _parts;
    private Renderer[][] _partRenderers;
    private bool _isDead;

    private void Awake()
    {
      WarnIfRootIsDestructible();

      if (_body == null)
        _body = GetComponentInChildren<DestructibleObject>(true);

      var camera = GetComponentInChildren<Camera>(true);
      _hudCameraObject = camera != null ? camera.gameObject : null;

      if (_body == null)
        return;

      _barCollider = _body.GetComponent<BoxCollider>();
      _barNoGoZone = _body.GetComponent<FlowMapNoGoZone>();
      _parts = _body.GetComponentsInChildren<DecayPart>(true);
      _partRenderers = new Renderer[_parts.Length][];
      for (var i = 0; i < _parts.Length; i++)
        _partRenderers[i] = _parts[i] != null
          ? _parts[i].GetComponentsInChildren<Renderer>(true)
          : System.Array.Empty<Renderer>();
    }

    private void OnEnable()
    {
      if (_body != null)
        _body.Destroyed += OnBodyDestroyed;

      UpdateBarCollider();
    }

    private void OnDisable()
    {
      if (_body != null)
        _body.Destroyed -= OnBodyDestroyed;
    }

    private void Update()
    {
      UpdateBarCollider();

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

    private void UpdateBarCollider()
    {
      if (_barCollider == null || _parts == null)
        return;

      if (!TryGetStandingPartsBounds(out var bounds))
      {
        if (_barCollider.enabled && _barCollider.size != Vector3.zero)
        {
          _barCollider.size = Vector3.zero;
          _barNoGoZone?.RefreshCache();
        }

        return;
      }

      if (_barCollider.center == bounds.center && _barCollider.size == bounds.size)
        return;

      _barCollider.center = bounds.center;
      _barCollider.size = bounds.size;
      _barNoGoZone?.RefreshCache();
    }

    private bool TryGetStandingPartsBounds(out Bounds bounds)
    {
      var hasBounds = false;
      bounds = default;

      for (var i = 0; i < _parts.Length; i++)
      {
        var part = _parts[i];
        if (part == null || !part.gameObject.activeInHierarchy)
          continue;

        var rigidbody = part.GetComponent<Rigidbody>();
        if (rigidbody != null && !rigidbody.isKinematic)
          continue;

        foreach (var renderer in _partRenderers[i])
        {
          if (renderer == null || !renderer.enabled)
            continue;

          EncapsulateWorldBounds(renderer.bounds, ref bounds, ref hasBounds);
        }
      }

      return hasBounds;
    }

    private void EncapsulateWorldBounds(Bounds worldBounds, ref Bounds localBounds, ref bool hasBounds)
    {
      var extents = worldBounds.extents;
      for (var x = -1; x <= 1; x += 2)
      for (var y = -1; y <= 1; y += 2)
      for (var z = -1; z <= 1; z += 2)
      {
        var worldPoint = worldBounds.center + Vector3.Scale(extents, new Vector3(x, y, z));
        var localPoint = _body.transform.InverseTransformPoint(worldPoint);
        if (!hasBounds)
        {
          localBounds = new Bounds(localPoint, Vector3.zero);
          hasBounds = true;
        }
        else
        {
          localBounds.Encapsulate(localPoint);
        }
      }
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
