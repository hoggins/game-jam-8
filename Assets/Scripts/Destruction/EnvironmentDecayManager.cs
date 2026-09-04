using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

namespace Destruction
{
  public class EnvironmentDecayManager : ITickable
  {
    private class DecayingPart
    {
      public Rigidbody Body;
      public PartDecaySettings Settings;
      public float RegisterTime;
      public float GroundY;
      public bool Sinking;
    }

    private readonly EnvironmentDecaySettings _settings;
    private readonly List<DecayingPart> _parts = new();

    public EnvironmentDecayManager(EnvironmentDecaySettings settings)
    {
      _settings = settings;
    }

    public void RegisterPart(Rigidbody body, PartDecaySettings settings)
    {
      _parts.Add(new DecayingPart
      {
        Body = body,
        Settings = settings,
        RegisterTime = Time.time,
      });
    }

    void ITickable.Tick()
    {
      for (var i = _parts.Count - 1; i >= 0; i--)
      {
        var part = _parts[i];

        if (part.Body == null)
        {
          _parts.RemoveAt(i);
          continue;
        }

        if (Time.time - part.RegisterTime < part.Settings.destructionDelay)
          continue;

        if (!part.Sinking)
        {
          part.GroundY = SampleGround(part.Body.position);
          part.Sinking = true;
          part.Body.isKinematic = true;
        }

        part.Body.MovePosition(part.Body.position + Vector3.down * (part.Settings.fallSpeed * Time.deltaTime));

        if (GetTopY(part.Body.transform) > part.GroundY + part.Settings.sinkDepth)
          continue;

        Object.Destroy(part.Body.gameObject);
        _parts.RemoveAt(i);
      }
    }

    private static float GetTopY(Transform target)
    {
      var renderers = target.GetComponentsInChildren<Renderer>();
      if (renderers.Length == 0)
        return target.position.y;

      var bounds = renderers[0].bounds;
      for (var i = 1; i < renderers.Length; i++)
        bounds.Encapsulate(renderers[i].bounds);

      return bounds.max.y;
    }

    private float SampleGround(Vector3 position)
    {
      var origin = position + Vector3.up * _settings.GroundRaycastHeight;
      if (Physics.Raycast(origin, Vector3.down, out var hit, _settings.GroundRaycastDistance, _settings.GroundLayer))
        return hit.point.y;

      return 0f;
    }
  }
}
