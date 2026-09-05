using System;
using App;
using Movement;
using UnityEngine;
using VContainer;

namespace Destruction
{
  [RequireComponent(typeof(FlowMapNoGoZone))]
  public class DestructibleObject : MonoBehaviour
  {
    private const string FrictionMaterialPath = "Descructable/FirstHouseFriction";

    [SerializeField, Min(0f)] private float _breakMagnitude = 5f;

    [Inject] private EnvironmentDecayManager _decayManager;

    private FlowMapNoGoZone _noGoZone;
    private BoxCollider _navigationCollider;
    private DecayPart[] _parts;
    private Renderer[] _renderers;
    private bool _destroyed;
    private bool _visible = true;

    public event Action<DestructibleObject> Destroyed;

    private static LayerMask? _partLayer;
    private static PhysicsMaterial _frictionMaterial;

    private void Awake()
    {
      this.AsInjected();

      _partLayer ??= LayerMask.NameToLayer(DestructibleLayers.Parts);
      _frictionMaterial ??= Resources.Load<PhysicsMaterial>(FrictionMaterialPath);

      _noGoZone = GetComponent<FlowMapNoGoZone>();
      _navigationCollider = GetComponent<BoxCollider>();
      _parts = GetComponentsInChildren<DecayPart>();
      _renderers = GetComponentsInChildren<Renderer>(true);

      // A part only needs a live Rigidbody once it actually breaks off (added in Impulse below);
      // with thousands of houses on a big map, a Rigidbody sitting on every intact part is pure
      // PhysX bookkeeping cost for debris nobody is touching yet. The collider stays too, just
      // disabled, since nothing needs it before the part is airborne.
      foreach (var part in _parts)
        foreach (var collider in part.GetComponents<Collider>())
          collider.enabled = false;
    }

    // Driven by EnvironmentVisibilityService: a house far from the player still needs its
    // no-go zone/collider/health live for pathfinding and damage, but nobody is looking at it, so
    // its renderers can sit disabled — one less object for the camera's per-frame culling pass to
    // consider, which matters a lot when thousands of houses are spawned at once.
    public void SetVisible(bool visible)
    {
      if (_visible == visible)
        return;

      _visible = visible;
      foreach (var renderer in _renderers)
        if (renderer != null)
          renderer.enabled = visible;
    }

    public void Break(Vector3 origin) =>
      Impulse(origin, _breakMagnitude);

    public void Impulse(Vector3 origin, float magnitude)
    {
      if (_destroyed)
        return;

      _destroyed = true;
      if (_noGoZone != null)
        _noGoZone.enabled = false;

      if (_navigationCollider != null)
        _navigationCollider.enabled = false;

      gameObject.layer = _partLayer!.Value;

      foreach (var part in _parts)
      {
        var colliders = part.GetComponents<Collider>();
        foreach (var collider in colliders)
        {
          collider.enabled = true;
          collider.material = _frictionMaterial;
        }

        var body = part.GetComponent<Rigidbody>();
        if (body == null)
          body = part.gameObject.AddComponent<Rigidbody>();
        body.isKinematic = false;

        var hitPoint = ClosestPoint(colliders, origin);
        var direction = (hitPoint - origin).normalized;
        body.AddForceAtPosition(direction * magnitude, hitPoint, ForceMode.Impulse);

        if (Application.isPlaying)
          _decayManager.RegisterPart(this, body, part.Settings);
      }

      Destroyed?.Invoke(this);

      if (_parts.Length == 0 && Application.isPlaying)
        Destroy(gameObject);
    }

    private static Vector3 ClosestPoint(Collider[] colliders, Vector3 origin)
    {
      if (colliders.Length == 0)
        return origin;

      var closest = colliders[0].ClosestPoint(origin);
      var closestDistance = Vector3.SqrMagnitude(closest - origin);

      for (var i = 1; i < colliders.Length; i++)
      {
        var point = colliders[i].ClosestPoint(origin);
        var distance = Vector3.SqrMagnitude(point - origin);
        if (distance < closestDistance)
        {
          closest = point;
          closestDistance = distance;
        }
      }

      return closest;
    }

    private void OnValidate() =>
      _breakMagnitude = Mathf.Max(0f, _breakMagnitude);
  }
}
