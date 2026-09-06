using System.Collections.Generic;
using Destruction;
using UnityEngine;

namespace Map
{
  /// <summary>
  /// The authored spawn-to-Goal route shared by timer respawns and house difficulty selection.
  /// Route progress is measured along the route polyline, not as a radius from the spawn.
  /// </summary>
  public sealed class TimerRoute
  {
    public const int HopCount = 6;

    private readonly Vector3[] _pathPoints;
    private readonly float[] _pathCumulativeLengths;
    private readonly Vector3[] _checkpointPoints;
    private readonly float[] _checkpointProgresses;
    private readonly float[] _checkpointLegLengths;

    private TimerRoute(
      Vector3[] pathPoints, float[] pathCumulativeLengths,
      Vector3[] checkpointPoints, float[] checkpointProgresses, float[] checkpointLegLengths)
    {
      _pathPoints = pathPoints;
      _pathCumulativeLengths = pathCumulativeLengths;
      _checkpointPoints = checkpointPoints;
      _checkpointProgresses = checkpointProgresses;
      _checkpointLegLengths = checkpointLegLengths;
      TotalLength = pathCumulativeLengths[pathCumulativeLengths.Length - 1];
    }

    public Vector3 Origin => _pathPoints[0];
    public Vector3 Goal => _pathPoints[_pathPoints.Length - 1];
    public IReadOnlyList<Vector3> Points => _pathPoints;
    public IReadOnlyList<Vector3> PathPoints => _pathPoints;
    public IReadOnlyList<Vector3> CheckpointPoints => _checkpointPoints;
    public int CheckpointCount => _checkpointPoints.Length;
    public int RespawnCount => Mathf.Max(0, _checkpointPoints.Length - 1);
    public float TotalLength { get; }
    public Vector3 InitialPosition => _checkpointPoints[0];

    /// <summary>
    /// Creates the absolute-distance zig-zag route retained as the fallback algorithm. The first
    /// leg places the initial Timer; the following six legs place successive respawns. Each segment
    /// has the requested world-unit length before runtime placement jitter is applied.
    /// </summary>
    public static bool TryCreate(
      Vector3 origin, Vector3 goal, float lateralDistanceRatio,
      float initialDistance, IReadOnlyList<float> hopDistances, out TimerRoute route)
    {
      route = null;

      var toGoal = goal - origin;
      toGoal.y = 0f;
      var directDistance = toGoal.magnitude;
      if (directDistance < 0.001f || hopDistances == null || hopDistances.Count < HopCount)
        return false;

      var forward = toGoal / directDistance;
      var side = new Vector3(-forward.z, 0f, forward.x);
      var lateralRatio = Mathf.Max(0f, lateralDistanceRatio);
      var points = new Vector3[HopCount + 3];
      points[0] = origin;

      var requestedForwardDistance = Mathf.Max(0f, initialDistance);
      for (var i = 0; i < HopCount; i++)
        requestedForwardDistance += Mathf.Max(0f, hopDistances[i]);

      var forwardPerUnit = 1f / Mathf.Sqrt(1f + lateralRatio * lateralRatio);
      var forwardDistance = requestedForwardDistance * forwardPerUnit;
      var scale = forwardDistance > directDistance
        ? directDistance / forwardDistance
        : 1f;

      var current = origin;
      var legIndex = 0;
      AppendLeg(
        points, 1, ref current, forward, side,
        Mathf.Max(0f, initialDistance) * scale, lateralRatio, legIndex++);
      for (var i = 0; i < HopCount; i++)
      {
        AppendLeg(
          points,
          i + 2,
          ref current,
          forward,
          side,
          Mathf.Max(0f, hopDistances[i]) * scale,
          lateralRatio,
          legIndex++);
      }

      goal.y = origin.y;
      points[points.Length - 1] = goal;
      var cumulativeLengths = BuildCumulativeLengths(points);
      var checkpointPoints = new Vector3[HopCount + 1];
      var checkpointProgresses = new float[checkpointPoints.Length];
      for (var i = 0; i < checkpointPoints.Length; i++)
      {
        checkpointPoints[i] = points[i + 1];
        checkpointProgresses[i] = cumulativeLengths[i + 1];
      }

      var checkpointLegLengths = BuildCheckpointLegLengths(checkpointProgresses);
      route = new TimerRoute(
        points,
        cumulativeLengths,
        checkpointPoints,
        checkpointProgresses,
        checkpointLegLengths);
      return route.TotalLength > 0.001f;
    }

    private static void AppendLeg(
      Vector3[] points, int pointIndex, ref Vector3 current,
      Vector3 forward, Vector3 side, float distance, float lateralRatio, int legIndex)
    {
      var directionScale = 1f / Mathf.Sqrt(1f + lateralRatio * lateralRatio);
      var forwardDistance = distance * directionScale;
      var lateralDistance = distance * lateralRatio * directionScale;
      var lateralSign = (legIndex & 1) == 0 ? -1f : 1f;
      current += forward * forwardDistance + side * (lateralSign * lateralDistance);
      current.y = points[0].y;
      points[pointIndex] = current;
    }

    /// <summary>
    /// Builds a route using the scene's current Goal. Runtime map fill and the editor's level
    /// preview pass the same route origin and endpoint when available.
    /// </summary>
    public static bool TryCreateForBattle(
      Vector3 origin, float lateralDistanceRatio, SpecialSpawnSettings spawnSettings, int seed,
      out TimerRoute route)
    {
      var goal = TheGoal.Current;
      if (goal == null)
        goal = Object.FindFirstObjectByType<TheGoal>();

      if (goal == null || goal.IsDestroyed)
      {
        route = null;
        return false;
      }

      var random = new System.Random(seed);
      var initialDistance = PickDistance(
        spawnSettings, SpecialHouses.Timer, -1, random, 30f);
      var hopDistances = new float[HopCount];
      for (var i = 0; i < HopCount; i++)
        hopDistances[i] = PickDistance(
          spawnSettings, SpecialHouses.Timer, i, random, i == 0 ? 30f : 60f);

      return TryCreate(
        origin,
        goal.transform.position,
        lateralDistanceRatio,
        initialDistance,
        hopDistances,
        out route);
    }

    private static float PickDistance(
      SpecialSpawnSettings spawnSettings, SpecialHouses type, int respawnIndex,
      System.Random random, float fallback)
    {
      var minDistance = 0f;
      var maxDistance = 0f;
      var configured = respawnIndex < 0
        ? spawnSettings != null && spawnSettings.TryGetInitialDistance(type, out minDistance, out maxDistance)
        : spawnSettings != null && spawnSettings.TryGetRespawnDistance(type, respawnIndex, out minDistance, out maxDistance);
      if (!configured)
        return fallback;

      return Mathf.Lerp(minDistance, maxDistance, (float)random.NextDouble());
    }

    private static float[] BuildCumulativeLengths(IReadOnlyList<Vector3> points)
    {
      var cumulativeLengths = new float[points.Count];
      for (var i = 1; i < points.Count; i++)
      {
        var segment = points[i] - points[i - 1];
        segment.y = 0f;
        cumulativeLengths[i] = cumulativeLengths[i - 1] + segment.magnitude;
      }

      return cumulativeLengths;
    }

    private static float[] BuildCheckpointLegLengths(float[] checkpointProgresses)
    {
      var legLengths = new float[checkpointProgresses.Length];
      if (legLengths.Length == 0)
        return legLengths;

      legLengths[0] = checkpointProgresses[0];
      for (var i = 1; i < legLengths.Length; i++)
        legLengths[i] = checkpointProgresses[i] - checkpointProgresses[i - 1];

      return legLengths;
    }

    public bool TryGetInitial(out Vector3 position, out float routeProgress)
    {
      if (_checkpointPoints.Length == 0)
      {
        position = default;
        routeProgress = 0f;
        return false;
      }

      position = _checkpointPoints[0];
      routeProgress = _checkpointProgresses[0];
      return routeProgress > 0f;
    }

    public bool TryGetHop(int hopIndex, out Vector3 position, out float routeProgress)
    {
      if (hopIndex < 0 || hopIndex >= RespawnCount)
      {
        position = default;
        routeProgress = 0f;
        return false;
      }

      position = _checkpointPoints[hopIndex + 1];
      routeProgress = _checkpointProgresses[hopIndex + 1];
      return true;
    }

    public float GetSegmentLength(int segmentIndex)
    {
      if (segmentIndex < 0 || segmentIndex >= _checkpointLegLengths.Length)
        return 0f;

      return _checkpointLegLengths[segmentIndex];
    }

    public float GetCheckpointProgress(int checkpointIndex)
    {
      if (checkpointIndex < 0 || checkpointIndex >= _checkpointProgresses.Length)
        return 0f;

      return _checkpointProgresses[checkpointIndex];
    }

    public float GetPathProgress(int pathPointIndex)
    {
      if (pathPointIndex < 0 || pathPointIndex >= _pathCumulativeLengths.Length)
        return 0f;

      return _pathCumulativeLengths[pathPointIndex];
    }

    public float NormalizeProgress(float routeProgress) =>
      TotalLength > 0f ? Mathf.Clamp01(routeProgress / TotalLength) : 0f;

    /// <summary>
    /// Projects a world-space point onto the closest segment of the route polyline and returns its
    /// normalized distance along that polyline. Points outside the route corridor inherit the stage
    /// of their nearest route segment.
    /// </summary>
    public float ProjectNormalizedProgress(Vector3 worldPoint)
    {
      var closestDistance = float.PositiveInfinity;
      var closestProgress = 0f;

      for (var i = 1; i < _pathPoints.Length; i++)
      {
        var start = _pathPoints[i - 1];
        var end = _pathPoints[i];
        var segment = end - start;
        segment.y = 0f;
        var segmentLengthSquared = segment.sqrMagnitude;
        if (segmentLengthSquared < 0.000001f)
          continue;

        var relative = worldPoint - start;
        relative.y = 0f;
        var segmentProgress = Mathf.Clamp01(Vector3.Dot(relative, segment) / segmentLengthSquared);
        var closestPoint = start + segment * segmentProgress;
        var distance = worldPoint - closestPoint;
        distance.y = 0f;
        var squaredDistance = distance.sqrMagnitude;
        if (squaredDistance >= closestDistance)
          continue;

        closestDistance = squaredDistance;
        closestProgress = (_pathCumulativeLengths[i - 1] + Mathf.Sqrt(segmentLengthSquared) * segmentProgress)
          / TotalLength;
      }

      return Mathf.Clamp01(closestProgress);
    }
  }
}
