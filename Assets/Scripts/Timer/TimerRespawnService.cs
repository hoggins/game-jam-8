using System;
using Arrow;
using Destruction;
using Map;
using Model;
using UnityEngine;
using Unity.Profiling;
using UnityEngine.Scripting;
using VContainer.Unity;

namespace Timer
{
  /// <summary>
  /// Keeps the battle timer alive: once <see cref="BattleService.DestroyTimer"/> fires (every
  /// digit smashed), spawns a fresh <see cref="BattleTimerObject"/> on a goalward zig-zag route,
  /// clearing any houses in its way, and resumes the countdown from the default duration. When the
  /// new timer is far enough from the player, the other live specials form a fence between them
  /// with the health bar in the centre; otherwise they use the near-player rule.
  /// </summary>
  [Preserve]
  public sealed class TimerRespawnService : IInitializable, IDisposable
  {
    private static readonly ProfilerMarker RespawnMarker = new("TimerRespawnService.Respawn");
    private static readonly ProfilerMarker SpawnTimerMarker = new("TimerRespawnService.SpawnTimer");
    private static readonly ProfilerMarker ArrangeFenceMarker = new("TimerRespawnService.ArrangeFence");
    private static readonly ProfilerMarker OtherSpecialsMarker = new("TimerRespawnService.OtherSpecials");

    // The route uses the timer's existing respawn distance range as its forward travel range. The
    // small placement jitter keeps the result from looking like a mathematically exact polyline.
    private const float ZigZagLateralDistance = 14f;
    private const float ZigZagPlacementJitter = 4f;

    private readonly BattleService _battleService;
    private readonly MapEnvironmentSpawner _spawner;
    private readonly SpecialSpawnSettings _spawnSettings;

    private int _respawnCount;
    private Vector3 _lastTimerPosition;
    private bool _hasLastTimerPosition;

    public TimerRespawnService(BattleService battleService, MapEnvironmentSpawner spawner, SpecialSpawnSettings spawnSettings)
    {
      _battleService = battleService;
      _spawner = spawner;
      _spawnSettings = spawnSettings;
    }

    void IInitializable.Initialize()
    {
      _battleService.TimerDestroyed += OnTimerDestroyed;
      _battleService.BattleStarted += OnBattleStarted;
    }

    void IDisposable.Dispose()
    {
      _battleService.TimerDestroyed -= OnTimerDestroyed;
      _battleService.BattleStarted -= OnBattleStarted;
    }

    private void OnBattleStarted()
    {
      _respawnCount = 0;
      _lastTimerPosition = default;
      _hasLastTimerPosition = false;
    }

    private void OnTimerDestroyed()
    {
      RespawnMarker.Begin();
      try
      {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
          return;

        if (_spawnSettings == null || !_spawnSettings.TryGetRespawnDistance(SpecialHouses.Timer, _respawnCount, out var minDistance, out var maxDistance))
          return;

        GameObject timerInstance = null;
        SpawnTimerMarker.Begin();
        try
        {
          if (!TrySpawnTimer(player.transform, minDistance, maxDistance, out timerInstance))
            return;
        }
        finally
        {
          SpawnTimerMarker.End();
        }

        _lastTimerPosition = timerInstance.transform.position;
        _hasLastTimerPosition = true;
        _respawnCount++;
        _battleService.RespawnTimer();

        var arrangedFence = false;
        ArrangeFenceMarker.Begin();
        try
        {
          arrangedFence = _spawner.TryArrangeSpecialFence(
            timerInstance.transform.position, player.transform.position, player.transform);
        }
        finally
        {
          ArrangeFenceMarker.End();
        }

        if (!arrangedFence)
          RespawnOtherSpecials(player.transform, timerInstance.transform.position);

        // All special placement/movement operations are now complete. Refresh once so the newly
        // spawned timer and any moved specials are visible to the flow map as a single change.
        _spawner.RefreshNoGoZones();
      }
      finally
      {
        RespawnMarker.End();
      }
    }

    private bool TrySpawnTimer(Transform player, float minDistance, float maxDistance, out GameObject timerInstance)
    {
      timerInstance = null;

      if (TryGetGoalPosition(out var goalPosition)
          && TryGetRouteOrigin(player.position, out var routeOrigin)
          && TryGetZigZagAnchor(routeOrigin, goalPosition, minDistance, maxDistance, _respawnCount, out var anchor))
      {
        // TrySpawnSpecial still validates the candidate against live specials and TheGoal. A small
        // jitter around the directed anchor gives it a few nearby options without losing the route.
        if (_spawner.TrySpawnSpecial(
          SpecialHouses.Timer, anchor, player, 0f, ZigZagPlacementJitter, out timerInstance))
          return true;
      }

      // A missing goal, a short route, or a crowded candidate should not strand the battle. Keep the
      // original player-centered behavior as the safe fallback.
      return _spawner.TrySpawnSpecial(
        SpecialHouses.Timer, player.position, player, minDistance, maxDistance, out timerInstance);
    }

    private bool TryGetRouteOrigin(Vector3 playerPosition, out Vector3 routeOrigin)
    {
      if (_hasLastTimerPosition)
      {
        routeOrigin = _lastTimerPosition;
        return true;
      }

      // The destroyed timer root remains alive as a decay husk during this callback. Reusing its
      // position makes the first route leg start where the initial timer actually stood instead of
      // jumping to an arbitrary point around the player.
      var timers = UnityEngine.Object.FindObjectsByType<BattleTimerObject>(FindObjectsSortMode.None);
      foreach (var timer in timers)
        if (timer != null && timer.IsDead)
        {
          routeOrigin = timer.transform.position;
          return true;
        }

      routeOrigin = playerPosition;
      return true;
    }

    private static bool TryGetGoalPosition(out Vector3 goalPosition)
    {
      var goal = TheGoal.Current;
      if (goal == null)
        goal = UnityEngine.Object.FindFirstObjectByType<TheGoal>();

      if (goal == null || goal.IsDestroyed)
      {
        goalPosition = default;
        return false;
      }

      goalPosition = goal.transform.position;
      return true;
    }

    private static bool TryGetZigZagAnchor(
      Vector3 routeOrigin, Vector3 goalPosition, float minDistance, float maxDistance, int respawnIndex,
      out Vector3 anchor)
    {
      var toGoal = goalPosition - routeOrigin;
      toGoal.y = 0f;
      var remainingDistance = toGoal.magnitude;
      if (remainingDistance < 0.001f)
      {
        anchor = default;
        return false;
      }

      var forward = toGoal / remainingDistance;
      var forwardDistance = Mathf.Min(UnityEngine.Random.Range(minDistance, maxDistance), remainingDistance * 0.8f);
      if (forwardDistance < 0.001f)
      {
        anchor = default;
        return false;
      }

      // Limit the lateral leg so the next point is still closer to the goal than the current one,
      // especially when the timer is on its final leg.
      var maxCloserLateralDistance = Mathf.Sqrt(
        Mathf.Max(0f, 2f * remainingDistance * forwardDistance - forwardDistance * forwardDistance));
      var lateralDistance = Mathf.Min(ZigZagLateralDistance, maxCloserLateralDistance * 0.8f);
      var side = new Vector3(-forward.z, 0f, forward.x);
      if ((respawnIndex & 1) != 0)
        side = -side;

      anchor = routeOrigin + forward * forwardDistance + side * lateralDistance;
      anchor.y = routeOrigin.y;
      return true;
    }

    private void RespawnOtherSpecials(Transform player, Vector3 timerPosition)
    {
      OtherSpecialsMarker.Begin();
      try
      {
        var houseSet = _spawner.CurrentHouseSet;
        if (houseSet == null)
          return;

        _spawner.GetOtherSpecialPlacement(timerPosition, player.position, out var anchor, out var minDistance, out var maxDistance);

        foreach (var special in houseSet.Specials)
        {
          if (special.type == SpecialHouses.Timer || !special.enabled || special.prefab == null)
            continue;

          if (special.type == SpecialHouses.Arrow)
          {
            MoveArrow(SpecialHouses.Arrow, BattleArrowObject.Current, anchor, player, minDistance, maxDistance);
            continue;
          }

          if (special.type == SpecialHouses.GoalArrow)
          {
            MoveArrow(SpecialHouses.GoalArrow, BattleArrowObject.GoalCurrent, anchor, player, minDistance, maxDistance);
            continue;
          }

          // The health bar is the player's own health made physical, so the fallback path never
          // recreates or relocates it. The fence path moves the existing live bar without resetting
          // its current pixels and makes it the central fence asset.
          if (special.type == SpecialHouses.Health)
            continue;

          // A special respawns alongside every Timer death; if one is already standing from an earlier
          // respawn, relocate it instead of spawning a duplicate on top of the world.
          if (_spawner.TryGetCurrentSpecial(special.type, out var existing))
            _spawner.TryMoveSpecial(special.type, existing, anchor, player, minDistance, maxDistance);
          else
            _spawner.TrySpawnSpecial(special.type, anchor, player, minDistance, maxDistance, out _);
        }
      }
      finally
      {
        OtherSpecialsMarker.End();
      }
    }

    /// <summary>
    /// Compass arrows are the specials that are never duplicated. There is one timer arrow and one
    /// goal arrow per battle: while either stands, a new timer moves it somewhere new along the
    /// route rather than handing the player a second one, so the navigation aid has to be found
    /// again; once smashed it is gone for the rest of the battle.
    ///
    /// <see cref="BattleArrowObject.Current"/> is null in exactly that second case, which is why
    /// nothing here spawns a replacement.
    /// </summary>
    private void MoveArrow(
      SpecialHouses type, BattleArrowObject arrow, Vector3 anchor, Transform player,
      float minDistance, float maxDistance)
    {
      if (arrow == null)
        return;

      _spawner.TryMoveSpecial(type, arrow.gameObject, anchor, player, minDistance, maxDistance);
    }
  }
}
