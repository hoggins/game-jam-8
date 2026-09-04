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

    [Inject] private EnvironmentDecayManager _decayManager;

    private FlowMapNoGoZone _noGoZone;
    private Rigidbody[] _bodies;
    private bool _destroyed;

    public event Action<DestructibleObject> Destroyed;

    private void Awake()
    {
      this.AsInjected();

      _noGoZone = GetComponent<FlowMapNoGoZone>();
      _bodies = GetComponentsInChildren<Rigidbody>();

      var friction = Resources.Load<PhysicsMaterial>(FrictionMaterialPath);
      foreach (var body in _bodies)
      {
        body.isKinematic = true;
        foreach (var collider in body.GetComponents<Collider>())
          collider.material = friction;
      }
    }

    public void Impulse(Vector3 origin, float magnitude)
    {
      if (_destroyed)
        return;

      _destroyed = true;
      if (_noGoZone != null)
        _noGoZone.enabled = false;

      foreach (var body in _bodies)
      {
        body.isKinematic = false;

        var hitPoint = ClosestPoint(body, origin);
        var direction = (hitPoint - origin).normalized;
        body.AddForceAtPosition(direction * magnitude, hitPoint, ForceMode.Impulse);

        if (Application.isPlaying)
        {
          var decayPart = body.GetComponent<DecayPart>();
          var settings = decayPart != null ? decayPart.Settings : new PartDecaySettings();
          _decayManager.RegisterPart(this, body, settings);
        }
      }

      Destroyed?.Invoke(this);

      if (_bodies.Length == 0 && Application.isPlaying)
        Destroy(gameObject);
    }

    private static Vector3 ClosestPoint(Rigidbody body, Vector3 origin)
    {
      var colliders = body.GetComponents<Collider>();
      if (colliders.Length == 0)
        return body.position;

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
  }
}
