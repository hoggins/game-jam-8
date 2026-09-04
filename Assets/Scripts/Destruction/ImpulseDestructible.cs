using System;
using App;
using UnityEngine;
using VContainer;

namespace Destruction
{
  public class ImpulseDestructible : MonoBehaviour
  {
    private const string FrictionMaterialPath = "Descructable/FirstHouseFriction";

    [SerializeField] private PartDecaySettings decaySettings = new();

    [Inject] private EnvironmentDecayManager _decayManager;

    private Rigidbody[] _bodies;
    private bool _destroyed;

    public event Action<ImpulseDestructible> Destroyed;

    private void Awake()
    {
      this.AsInjected();

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

      foreach (var body in _bodies)
      {
        body.isKinematic = false;

        var hitPoint = ClosestPoint(body, origin);
        var direction = (hitPoint - origin).normalized;
        body.AddForceAtPosition(direction * magnitude, hitPoint, ForceMode.Impulse);

        _decayManager.RegisterPart(body, decaySettings);
      }

      Destroyed?.Invoke(this);
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
