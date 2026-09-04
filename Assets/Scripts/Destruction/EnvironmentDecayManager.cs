using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

namespace Destruction
{
  public class EnvironmentDecayManager : ITickable
  {
    private class DecayingPart
    {
      public DestructibleObject Owner;
      public Rigidbody Body;
      public PartDecaySettings Settings;
      public float IdleTimer;
      public float GroundY;
      public bool Sinking;
    }

    private readonly EnvironmentDecaySettings _settings;
    private readonly List<DecayingPart> _parts = new();
    private readonly Dictionary<DestructibleObject, int> _remainingParts = new();

    public EnvironmentDecayManager(EnvironmentDecaySettings settings)
    {
      _settings = settings;
    }

    public void RegisterPart(DestructibleObject owner, Rigidbody body, PartDecaySettings settings)
    {
      _remainingParts.TryGetValue(owner, out var count);
      _remainingParts[owner] = count + 1;

      _parts.Add(new DecayingPart
      {
        Owner = owner,
        Body = body,
        Settings = settings,
      });
    }

    void ITickable.Tick()
    {
      if (!Application.isPlaying)
        return;

      for (var i = _parts.Count - 1; i >= 0; i--)
      {
        var part = _parts[i];

        if (part.Body == null)
        {
          _parts.RemoveAt(i);
          CompletePart(part.Owner);
          continue;
        }

        if (!part.Sinking)
        {
          if (!IsIdle(part.Body))
          {
            part.IdleTimer = 0f;
            continue;
          }

          part.IdleTimer += Time.deltaTime;
          if (part.IdleTimer < _settings.IdleGraceTime)
            continue;

          part.GroundY = SampleGround(part.Body.position);
          part.Sinking = true;
          part.Body.isKinematic = true;
        }

        part.Body.MovePosition(part.Body.position + Vector3.down * (part.Settings.fallSpeed * Time.deltaTime));

        var bounds = GetBounds(part.Body.transform);
        if (!IsUnderground(bounds, part.GroundY, part.Settings.sinkDepth))
          continue;

        Remove(part.Body.gameObject);
        _parts.RemoveAt(i);
        CompletePart(part.Owner);
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

      if (owner != null)
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

    private bool IsIdle(Rigidbody body) =>
      body.linearVelocity.sqrMagnitude <= _settings.IdleLinearSpeedThreshold * _settings.IdleLinearSpeedThreshold
      && body.angularVelocity.sqrMagnitude <= _settings.IdleAngularSpeedThreshold * _settings.IdleAngularSpeedThreshold;

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
