using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;
using VContainer.Unity;

namespace Destruction
{
  public class EnvironmentDecayManager : ITickable
  {
    private static readonly ProfilerMarker TickMarker =
      new("EnvironmentDecayManager.Tick");

    private class DecayingPart
    {
      public DestructibleObject Owner;
      public Rigidbody Body;
      public float DecayStartTimer;
      public float GroundY;
      public bool Sinking;
    }

    private readonly EnvironmentDecaySettings _settings;
    private readonly List<DecayingPart> _parts = new();
    private readonly Dictionary<DestructibleObject, int> _remainingParts = new();
    private readonly HashSet<DestructibleObject> _ownersPendingDestruction = new();

    public EnvironmentDecayManager(EnvironmentDecaySettings settings)
    {
      _settings = settings;
    }

    public void RegisterPart(DestructibleObject owner, Rigidbody body)
    {
      _remainingParts.TryGetValue(owner, out var count);
      _remainingParts[owner] = count + 1;

      _parts.Add(new DecayingPart
      {
        Owner = owner,
        Body = body,
        DecayStartTimer = 0f,
      });
    }

    public void MarkForDestruction(DestructibleObject owner)
    {
      if (owner == null)
        return;

      _ownersPendingDestruction.Add(owner);
      if (_remainingParts.ContainsKey(owner))
        return;

      _ownersPendingDestruction.Remove(owner);
      Remove(owner.gameObject);
    }

    void ITickable.Tick()
    {
      if (!Application.isPlaying)
        return;

      TickMarker.Begin();
      try
      {
        for (var i = _parts.Count - 1; i >= 0; i--)
        {
          var part = _parts[i];

          if (part.Body == null)
          {
            _parts.RemoveAt(i);
            CompletePart(part.Owner);
            continue;
          }

          part.DecayStartTimer += Time.deltaTime;

          if (!part.Sinking)
          {
            if (part.DecayStartTimer < _settings.DecayStartDelay)
              continue;

            part.GroundY = SampleGround(part.Body.position);
            part.Sinking = true;

            // Ignore physics from here on: other debris should pass through as this part sinks, not rest on top of it.
            foreach (var partCollider in part.Body.GetComponentsInChildren<Collider>())
              partCollider.enabled = false;
          }

          var bounds = GetBounds(part.Body.transform);
          if (!IsUnderground(bounds, part.GroundY, _settings.SinkDepth))
            continue;

          Remove(part.Body.gameObject);
          _parts.RemoveAt(i);
          CompletePart(part.Owner);
        }
      }
      finally
      {
        TickMarker.End();
      }
    }

    private void CompletePart(DestructibleObject owner)
    {
      if (owner == null || !_remainingParts.TryGetValue(owner, out var count))
        return;

      count--;
      if (count > 0)
      {
        _remainingParts[owner] = count;
        return;
      }

      _remainingParts.Remove(owner);

      if (_ownersPendingDestruction.Remove(owner))
        Remove(owner.gameObject);
    }

    private static void Remove(GameObject target)
    {
      try
      {
        UnityEngine.Object.Destroy(target);
      }
      catch (InvalidOperationException)
      {
        // Part is a still-connected Prefab instance child (e.g. testing inside Prefab Mode); Destroy is disallowed there.
        target.SetActive(false);
      }
    }

    private static Bounds GetBounds(Transform target)
    {
      var renderers = target.GetComponentsInChildren<Renderer>();
      if (renderers.Length == 0)
        return new Bounds(target.position, Vector3.zero);

      var bounds = renderers[0].bounds;
      for (var i = 1; i < renderers.Length; i++)
        bounds.Encapsulate(renderers[i].bounds);

      return bounds;
    }

    // Explicitly requires the part's whole AABB to be below the ground surface (offset by sinkDepth)
    // before it is disabled, not just its topmost point crossing some threshold.
    private static bool IsUnderground(Bounds bounds, float groundY, float sinkDepth) =>
      bounds.max.y <= groundY - sinkDepth;

    private float SampleGround(Vector3 position)
    {
      var origin = position + Vector3.up * _settings.GroundRaycastHeight;
      if (Physics.Raycast(origin, Vector3.down, out var hit, _settings.GroundRaycastDistance, _settings.GroundLayer))
        return hit.point.y;

      return 0f;
    }
  }
}
