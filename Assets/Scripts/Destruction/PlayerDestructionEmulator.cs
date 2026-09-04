using System.Collections.Generic;
using UnityEngine;

namespace Destruction
{
  public sealed class PlayerDestructionEmulator : MonoBehaviour
  {
    [Header("Check")]
    [SerializeField, Min(0f)] private float _interval = 0.5f;

    [Header("Pulse")]
    [SerializeField, Min(0f)] private float _radius = 3f;
    [SerializeField, Min(0f)] private float _magnitude = 5f;

    private readonly HashSet<DestructibleObject> _hit = new();

    private float _timer;

    private void Update()
    {
      _timer += Time.deltaTime;
      if (_timer < _interval)
        return;

      _timer = 0f;
      Check();
    }

    private void Check()
    {
      var origin = transform.position;

      _hit.Clear();
      foreach (var collider in Physics.OverlapSphere(origin, _radius))
      {
        var destructible = collider.GetComponentInParent<DestructibleObject>();
        if (destructible != null)
          _hit.Add(destructible);
      }

      foreach (var destructible in _hit)
        destructible.Impulse(origin, _magnitude);
    }

    private void OnDrawGizmosSelected()
    {
      Gizmos.color = Color.yellow;
      Gizmos.DrawWireSphere(transform.position, _radius);
    }

    private void OnValidate()
    {
      _interval = Mathf.Max(0f, _interval);
      _radius = Mathf.Max(0f, _radius);
      _magnitude = Mathf.Max(0f, _magnitude);
    }
  }
}
