using System.Collections.Generic;
using Destruction;
using UnityEngine;

namespace Map
{
  /// <summary>
  /// The authored spawn-to-Goal route shared by timer respawns and house difficulty selection.
  /// Route progress is measured along the zig-zag polyline, not as a radius from the spawn.
  /// </summary>
  public sealed class TimerRoute
  {
    public const int HopCount = 6;

    private static readonly float[] HopForwardProgress = { 0.10f, 0.20f, 0.40f, 0.60f, 0.80f, 1.00f };

    private readonly Vector3[] _points;
    private readonly float[] _cumulativeLengths;

    private TimerRoute(Vector3[] points, float[] cumulativeLengths)
    {
      _points = points;
      _cumulativeLengths = cumulativeLengths;
      TotalLength = cumulativeLengths[cumulativeLengths.Length - 1];
    }

    public Vector3 Origin => _points[0];
    public Vector3 Goal => _points[_points.Length - 1];
    public IReadOnlyList<Vector3> Points => _points;
    public float TotalLength { get; }

    /// <summary>
    /// Creates the six-hop route used by the current timer schedule. The first two forward legs
    /// cover 20% of the direct route and the next two cover the next 40%, matching the T1/T2/T3
    /// checkpoints in the pacing proposal. The last hop reaches the Goal's forward coordinate with
    /// its lateral offset intact, keeping the timer clear of the Goal; the final polyline point is
    /// the Goal itself.
    /// </summary>
    public static bool TryCreate(Vector3 origin, Vector3 goal, float lateralDistanceRatio, out TimerRoute route)
    {
      route = null;

      var toGoal = goal - origin;
      toGoal.y = 0f;
      var directDistance = toGoal.magnitude;
      if (directDistance < 0.001f)
        return false;

      var forward = toGoal / directDistance;
      var side = new Vector3(-forward.z, 0f, forward.x);
      var lateralRatio = Mathf.Max(0f, lateralDistanceRatio);
      var points = new Vector3[HopCount + 2];
      points[0] = origin;

      var previousForwardProgress = 0f;
      for (var i = 0; i < HopCount; i++)
      {
        var forwardProgress = HopForwardProgress[i];
        var legProgress = forwardProgress - previousForwardProgress;
        var lateralDistance = directDistance * legProgress * lateralRatio;
        var lateralSign = (i & 1) == 0 ? -1f : 1f;
        var point = origin
          + forward * (directDistance * forwardProgress)
          + side * (lateralSign * lateralDistance);
        point.y = origin.y;
        points[i + 1] = point;
        previousForwardProgress = forwardProgress;
      }

      goal.y = origin.y;
      points[points.Length - 1] = goal;

      var cumulativeLengths = new float[points.Length];
      for (var i = 1; i < points.Length; i++)
      {
        var segment = points[i] - points[i - 1];
        segment.y = 0f;
        cumulativeLengths[i] = cumulativeLengths[i - 1] + segment.magnitude;
      }

      if (cumulativeLengths[cumulativeLengths.Length - 1] < 0.001f)
        return false;

      route = new TimerRoute(points, cumulativeLengths);
      return true;
    }

    /// <summary>
    /// Builds a route using the scene's current Goal. This is shared by runtime map fill and the
    /// editor's level preview, so both paths use the same route origin and endpoint when available.
    /// </summary>
    public static bool TryCreateForBattle(Vector3 origin, float lateralDistanceRatio, out TimerRoute route)
    {
      var goal = TheGoal.Current;
      if (goal == null)
        goal = Object.FindFirstObjectByType<TheGoal>();

      if (goal == null || goal.IsDestroyed)
      {
        route = null;
        return false;
      }

      return TryCreate(origin, goal.transform.position, lateralDistanceRatio, out route);
    }

    public bool TryGetHop(int hopIndex, out Vector3 position, out float routeProgress)
    {
      if (hopIndex < 0 || hopIndex >= HopCount)
      {
        position = default;
        routeProgress = 0f;
        return false;
      }

      position = _points[hopIndex + 1];
      routeProgress = _cumulativeLengths[hopIndex + 1];
      return true;
    }

    public float NormalizeProgress(float routeProgress) =>
      TotalLength > 0f ? Mathf.Clamp01(routeProgress / TotalLength) : 0f;

    /// <summary>
    /// Projects a world-space point onto the closest segment of the route polyline and returns its
    /// normalized distance along that polyline. Points outside the route corridor naturally inherit
    /// the stage of their nearest route segment.
    /// </summary>
    public float ProjectNormalizedProgress(Vector3 worldPoint)
    {
      var closestDistance = float.PositiveInfinity;
      var closestProgress = 0f;

      for (var i = 1; i < _points.Length; i++)
      {
        var start = _points[i - 1];
        var end = _points[i];
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
        closestProgress = (_cumulativeLengths[i - 1] + Mathf.Sqrt(segmentLengthSquared) * segmentProgress)
          / TotalLength;
      }

      return Mathf.Clamp01(closestProgress);
    }
  }
}
