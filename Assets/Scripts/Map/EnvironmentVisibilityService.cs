using UnityEngine;
using VContainer.Unity;

namespace Map
{
  // Frustum culling still has to test every active renderer against the camera every frame, so
  // with thousands of houses spawned across a big map at once, most of them sitting far outside
  // the camera's view are still paying that per-object cost. Disabling their renderers outright
  // removes them from consideration entirely instead of just making the test cheaper.
  public sealed class EnvironmentVisibilityService : ITickable
  {
    private const string PlayerTag = "Player";

    private readonly MapEnvironmentSpawner _spawner;
    private readonly EnvironmentVisibilitySettings _settings;

    private Transform _player;

    public EnvironmentVisibilityService(MapEnvironmentSpawner spawner, EnvironmentVisibilitySettings settings)
    {
      _spawner = spawner;
      _settings = settings;
    }

    void ITickable.Tick()
    {
      if (_player == null)
        _player = GameObject.FindGameObjectWithTag(PlayerTag)?.transform;

      if (_player == null)
        return;

      var playerPosition = _player.position;
      var visibleRadiusSq = _settings.VisibleRadius * _settings.VisibleRadius;
      var hiddenRadiusSq = _settings.HiddenRadius * _settings.HiddenRadius;

      foreach (var placement in _spawner.SpawnedObjects)
      {
        var destructible = placement.Destructible;
        if (destructible == null)
          continue;

        var offset = destructible.transform.position - playerPosition;
        offset.y = 0f;
        var distanceSq = offset.sqrMagnitude;

        // Hysteresis band between the two radii keeps an object sitting near the boundary from
        // flipping visibility every single tick.
        if (distanceSq <= visibleRadiusSq)
          destructible.SetVisible(true);
        else if (distanceSq > hiddenRadiusSq)
          destructible.SetVisible(false);
      }
    }
  }
}
