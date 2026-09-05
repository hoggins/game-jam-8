using System;
using System.Collections.Generic;
using App;
using Movement;
using Pooling;
using UnityEngine;
using VContainer;

namespace Destruction
{
  [RequireComponent(typeof(FlowMapNoGoZone))]
  public class DestructibleObject : MonoBehaviour
  {
    private const string FrictionMaterialPath = "Descructable/FirstHouseFriction";
    private const string DestructionFxPath = "Fx/Prefabs/FxBuildingDucksDestruction01";

    [SerializeField, Min(0f)] private float _breakMagnitude = 5f;

    [Inject] private EnvironmentDecayManager _decayManager;
    [Inject] private Pool _pool;

    private FlowMapNoGoZone _noGoZone;
    private BoxCollider _navigationCollider;
    private List<DecayPart> _parts;
    private GameObject _destructionFxPrefab;
    private bool _destroyed;

    public event Action<DestructibleObject> Destroyed;

    private static LayerMask? _partLayer;
    private static PhysicsMaterial _frictionMaterial;

    private void Awake()
    {
      this.AsInjected();

      _partLayer ??= LayerMask.NameToLayer(DestructibleLayers.Parts);
      _frictionMaterial ??= Resources.Load<PhysicsMaterial>(FrictionMaterialPath);
      _destructionFxPrefab = Resources.Load<GameObject>(DestructionFxPath);

      _noGoZone = GetComponent<FlowMapNoGoZone>();
      _navigationCollider = GetComponent<BoxCollider>();
      _parts = new List<DecayPart>(GetComponentsInChildren<DecayPart>());

      // A part only needs a live Rigidbody once it actually breaks off (added in Impulse below);
      // with thousands of houses on a big map, a Rigidbody sitting on every intact part is pure
      // PhysX bookkeeping cost for debris nobody is touching yet. The collider stays too, just
      // disabled, since nothing needs it before the part is airborne.
      foreach (var part in _parts)
        foreach (var collider in part.GetComponents<Collider>())
          collider.enabled = false;
    }

    public void Break(Vector3 origin) =>
      Impulse(origin, _breakMagnitude);

    public bool FallOutRandomPart(Vector3 origin)
    {
      RemoveMissingParts();
      if (_destroyed || _parts.Count == 0)
        return false;

      var index = UnityEngine.Random.Range(0, _parts.Count);
      var part = _parts[index];
      _parts.RemoveAt(index);
      DetachPart(part, origin, _breakMagnitude);
      return true;
    }

    /// <summary>
    /// Knocks one specific part off. <see cref="FallOutRandomPart"/> is the right call when the parts
    /// are interchangeable rubble; this one is for objects whose parts mean something in order — a
    /// bar that has to empty from one end rather than developing holes in the middle.
    /// </summary>
    public bool FallOutPart(DecayPart part, Vector3 origin)
    {
      RemoveMissingParts();
      if (_destroyed || part == null || !_parts.Remove(part))
        return false;

      DetachPart(part, origin, _breakMagnitude);
      return true;
    }

    public void Impulse(Vector3 origin, float magnitude)
    {
      if (_destroyed)
        return;

      _destroyed = true;
      if (_noGoZone != null)
        _noGoZone.enabled = false;

      if (_navigationCollider != null)
        _navigationCollider.enabled = false;

      ApplyGroundDamage();
      SpawnDestructionFx();

      gameObject.layer = _partLayer!.Value;

      for (var i = _parts.Count - 1; i >= 0; i--)
      {
        var part = _parts[i];
        _parts.RemoveAt(i);
        DetachPart(part, origin, magnitude);
      }

      Destroyed?.Invoke(this);

      if (Application.isPlaying)
      {
        if (_decayManager != null)
          _decayManager.MarkForDestruction(this);
        else if (_parts.Count == 0)
          Destroy(gameObject);
      }
    }

    private void DetachPart(DecayPart part, Vector3 origin, float magnitude)
    {
      if (part == null)
        return;

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

      if (Application.isPlaying && _decayManager != null)
        _decayManager.RegisterPart(this, body, part.Settings);
    }

    private void RemoveMissingParts()
    {
      for (var i = _parts.Count - 1; i >= 0; i--)
      {
        if (_parts[i] == null)
          _parts.RemoveAt(i);
      }
    }

    private void SpawnDestructionFx()
    {
      if (!Application.isPlaying || _pool == null || _destructionFxPrefab == null)
        return;

      var rotation = transform.rotation * _destructionFxPrefab.transform.localRotation;
      _pool.Get(_destructionFxPrefab, transform.position, rotation);
    }

    private void ApplyGroundDamage()
    {
      var health = GetComponent<DestructibleHealth>();
      if (health == null || health.ObjectType != DestructibleObjectType.House)
        return;

      var groundDamageMask = GroundDamageMask.Instance;
      if (groundDamageMask == null)
        return;

      var renderers = GetComponentsInChildren<Renderer>(true);
      if (renderers.Length == 0)
        return;

      var bounds = renderers[0].bounds;
      for (var i = 1; i < renderers.Length; i++)
        bounds.Encapsulate(renderers[i].bounds);

      var footprintRadius = Mathf.Sqrt(
        bounds.extents.x * bounds.extents.x
        + bounds.extents.z * bounds.extents.z);
      groundDamageMask.ApplyCircleDamage(bounds.center, footprintRadius, Color.white, 1f, 0.5f);
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
