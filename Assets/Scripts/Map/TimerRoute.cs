using System;
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
    public const int HopCount = 9;
    private const int MaxGenerationAttempts = 8;
    private const int LateralNormalizationSamples = 1024;

    private readonly Vector3[] _pathPoints;
    private readonly float[] _pathCumulativeLengths;
    private readonly Vector3[] _checkpointPoints;
    private readonly float[] _checkpointProgresses;
    private readonly float[] _checkpointLegLengths;
    private readonly float[] _checkpointTurnAngles;
    private readonly float[] _checkpointLateralOffsets;

    private TimerRoute(
      Vector3[] pathPoints, float[] pathCumulativeLengths,
      Vector3[] checkpointPoints, float[] checkpointProgresses, float[] checkpointLegLengths,
      float directDistance, float scale, float lateralAmplitude, float oscillations,
      int routeSeed, int generationAttempts, bool mirrored,
      float harmonicWeight, float phase, float secondHarmonicPhase,
      float peakLateralExcursion,
      float[] checkpointTurnAngles, float[] checkpointLateralOffsets)
    {
      _pathPoints = pathPoints;
      _pathCumulativeLengths = pathCumulativeLengths;
      _checkpointPoints = checkpointPoints;
      _checkpointProgresses = checkpointProgresses;
      _checkpointLegLengths = checkpointLegLengths;
      _checkpointTurnAngles = checkpointTurnAngles;
      _checkpointLateralOffsets = checkpointLateralOffsets;
      DirectDistance = directDistance;
      Scale = scale;
      LateralAmplitude = lateralAmplitude;
      Oscillations = oscillations;
      RouteSeed = routeSeed;
      GenerationAttempts = generationAttempts;
      Mirrored = mirrored;
      HarmonicWeight = harmonicWeight;
      Phase = phase;
      SecondHarmonicPhase = secondHarmonicPhase;
      PeakLateralExcursion = peakLateralExcursion;
      TotalLength = pathCumulativeLengths[pathCumulativeLengths.Length - 1];

      var turnTotal = 0f;
      var turnMaximum = 0f;
      for (var i = 0; i < _checkpointTurnAngles.Length; i++)
      {
        turnTotal += _checkpointTurnAngles[i];
        turnMaximum = Mathf.Max(turnMaximum, _checkpointTurnAngles[i]);
      }

      MaxTurnAngleDeg = turnMaximum;
      MeanTurnAngleDeg = _checkpointTurnAngles.Length > 0
        ? turnTotal / _checkpointTurnAngles.Length
        : 0f;
      FinalSegmentTurnAngleDeg = _checkpointTurnAngles.Length > 0
        ? _checkpointTurnAngles[_checkpointTurnAngles.Length - 1]
        : 0f;
    }

    public Vector3 Origin => _pathPoints[0];
    public Vector3 Goal => _pathPoints[_pathPoints.Length - 1];
    public IReadOnlyList<Vector3> Points => _pathPoints;
    public IReadOnlyList<Vector3> PathPoints => _pathPoints;
    public IReadOnlyList<Vector3> CheckpointPoints => _checkpointPoints;
    public int CheckpointCount => _checkpointPoints.Length;
    public int RespawnCount => Mathf.Max(0, _checkpointPoints.Length - 1);
    public float TotalLength { get; }
    public float DirectDistance { get; }
    public float Scale { get; }
    public float LateralAmplitude { get; }
    public float Oscillations { get; }
    public int RouteSeed { get; }
    public int GenerationAttempts { get; }
    public bool Mirrored { get; }
    public float HarmonicWeight { get; }
    public float Phase { get; }
    public float SecondHarmonicPhase { get; }
    public float PeakLateralExcursion { get; }
    public float MaxTurnAngleDeg { get; }
    public float MeanTurnAngleDeg { get; }
    public float FinalSegmentTurnAngleDeg { get; }
    public Vector3 InitialPosition => _checkpointPoints[0];

    /// <summary>
    /// Creates the route with the first leg placing the initial Timer and the following nine legs
    /// placing successive respawns.
    /// </summary>
    public static bool TryCreate(
      Vector3 origin, Vector3 goal, float lateralAmplitude,
      float initialDistance, IReadOnlyList<float> hopDistances, out TimerRoute route)
    {
      return TryCreate(
        origin, goal, lateralAmplitude, 0.9f, 1.5f,
        initialDistance, hopDistances, out route);
    }

    public static bool TryCreate(
      Vector3 origin, Vector3 goal, float lateralAmplitude,
      float forwardFraction, float oscillations,
      float initialDistance, IReadOnlyList<float> hopDistances, out TimerRoute route)
    {
      return TryCreate(
        origin, goal, lateralAmplitude, forwardFraction, oscillations,
        initialDistance, hopDistances, false, 0f, 0f, 0f,
        0, 1, out route);
    }

    private static bool TryCreate(
      Vector3 origin, Vector3 goal, float lateralAmplitude,
      float forwardFraction, float oscillations,
      float initialDistance, IReadOnlyList<float> hopDistances,
      bool mirrored, float harmonicWeight, float phase, float secondHarmonicPhase,
      int routeSeed, int generationAttempts, out TimerRoute route)
    {
      route = null;

      var toGoal = goal - origin;
      toGoal.y = 0f;
      var directDistance = toGoal.magnitude;
      if (directDistance < 0.001f || hopDistances == null || hopDistances.Count < HopCount)
        return false;

      var forward = toGoal / directDistance;
      var side = new Vector3(-forward.z, 0f, forward.x);
      if (mirrored)
        side = -side;
      var points = new Vector3[HopCount + 3];
      points[0] = origin;

      var requestedForwardDistance = Mathf.Max(0f, initialDistance);
      for (var i = 0; i < HopCount; i++)
        requestedForwardDistance += Mathf.Max(0f, hopDistances[i]);

      var forwardBudget = directDistance * Mathf.Clamp01(forwardFraction);
      var scale = requestedForwardDistance > 0f
        ? forwardBudget / requestedForwardDistance
        : 0f;
      var scaledForwardDistance = requestedForwardDistance * scale;
      var amplitude = Mathf.Max(0f, lateralAmplitude);
      var normalization = CalculateLateralNormalization(
        oscillations, harmonicWeight, phase, secondHarmonicPhase);

      var current = origin;
      var cumulativeForwardDistance = 0f;
      AppendLeg(
        points, 1, ref current, forward, side,
        Mathf.Max(0f, initialDistance) * scale,
        ref cumulativeForwardDistance,
        scaledForwardDistance, amplitude, oscillations,
        harmonicWeight, phase, secondHarmonicPhase, normalization);
      for (var i = 0; i < HopCount; i++)
      {
        AppendLeg(
          points,
          i + 2,
          ref current,
          forward,
          side,
          Mathf.Max(0f, hopDistances[i]) * scale,
          ref cumulativeForwardDistance,
          scaledForwardDistance,
          amplitude,
          oscillations,
          harmonicWeight,
          phase,
          secondHarmonicPhase,
          normalization);
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
      var checkpointTurnAngles = BuildCheckpointTurnAngles(points);
      var checkpointLateralOffsets = BuildCheckpointLateralOffsets(
        checkpointPoints, origin, side);
      route = new TimerRoute(
        points,
        cumulativeLengths,
        checkpointPoints,
        checkpointProgresses,
        checkpointLegLengths,
        directDistance,
        scale,
        amplitude,
        oscillations,
        routeSeed,
        generationAttempts,
        mirrored,
        harmonicWeight,
        phase,
        secondHarmonicPhase,
        amplitude,
        checkpointTurnAngles,
        checkpointLateralOffsets);
      return route.TotalLength > 0.001f;
    }

    private static void AppendLeg(
      Vector3[] points, int pointIndex, ref Vector3 current,
      Vector3 forward, Vector3 side, float distance,
      ref float cumulativeForwardDistance, float totalForwardDistance,
      float amplitude, float oscillations,
      float harmonicWeight, float phase, float secondHarmonicPhase,
      float normalization)
    {
      cumulativeForwardDistance += distance;
      var normalizedProgress = totalForwardDistance > 0f
        ? cumulativeForwardDistance / totalForwardDistance
        : (float)pointIndex / (HopCount + 1);
      var rawOffset = EvaluateLateralSeries(
        normalizedProgress, oscillations, harmonicWeight, phase, secondHarmonicPhase);
      var originOffset = EvaluateLateralSeries(
        0f, oscillations, harmonicWeight, phase, secondHarmonicPhase);
      var lateralOffset = amplitude * (rawOffset - originOffset) / normalization;
      current = points[0]
        + forward * cumulativeForwardDistance
        + side * lateralOffset;
      current.y = points[0].y;
      points[pointIndex] = current;
    }

    /// <summary>
    /// Builds a route using the scene's current Goal. Runtime map fill and the editor's level
    /// preview pass the same route origin and endpoint when available.
    /// </summary>
    public static bool TryCreateForBattle(
      Vector3 origin, float lateralAmplitude, SpecialSpawnSettings spawnSettings, int seed,
      out TimerRoute route)
    {
      return TryCreateForBattle(
        origin, lateralAmplitude, 0.9f, 1.5f, 45f, 2,
        spawnSettings, seed, out route, out _);
    }

    public static bool TryCreateForBattle(
      Vector3 origin, float lateralAmplitude, float forwardFraction, float oscillations,
      SpecialSpawnSettings spawnSettings, int seed, out TimerRoute route)
    {
      return TryCreateForBattle(
        origin, lateralAmplitude, forwardFraction, oscillations, 45f, 2,
        spawnSettings, seed, out route, out _);
    }

    public static bool TryCreateForBattle(
      Vector3 origin, float lateralAmplitude, float forwardFraction, float oscillations,
      float maxTurnAngle, int minCheckpointsPerTier,
      SpecialSpawnSettings spawnSettings, int seed,
      out TimerRoute route, out int generationAttempts)
    {
      route = null;
      generationAttempts = 0;
      var goal = TheGoal.Current;
      if (goal == null)
        goal = UnityEngine.Object.FindFirstObjectByType<TheGoal>();

      if (goal == null || goal.IsDestroyed)
      {
        return false;
      }

      var random = new System.Random(seed);
      var candidateHopDistances = new float[HopCount];
      for (var attempt = 1; attempt <= MaxGenerationAttempts; attempt++)
      {
        var initialDistance = PickDistance(
          spawnSettings, SpecialHouses.Timer, -1, random, 30f);
        for (var i = 0; i < HopCount; i++)
          candidateHopDistances[i] = PickDistance(
            spawnSettings, SpecialHouses.Timer, i, random, i == 0 ? 30f : 60f);

        var mirrored = random.Next(0, 2) == 0;
        var harmonicWeight = (float)random.NextDouble() * 0.4f;
        var phase = (float)random.NextDouble() * 2f * Mathf.PI;
        var secondHarmonicPhase = (float)random.NextDouble() * 2f * Mathf.PI;
        if (!TryCreate(
          origin,
          goal.transform.position,
          lateralAmplitude,
          forwardFraction,
          oscillations,
          initialDistance,
          candidateHopDistances,
          mirrored,
          harmonicWeight,
          phase,
          secondHarmonicPhase,
          seed,
          attempt,
          out var candidate))
          continue;

        if (!MeetsGenerationConstraints(candidate, maxTurnAngle, minCheckpointsPerTier))
          continue;

        route = candidate;
        generationAttempts = attempt;
        return true;
      }

      var fallbackLegs = new float[HopCount];
      for (var i = 0; i < fallbackLegs.Length; i++)
        fallbackLegs[i] = 1f;

      TryCreate(
        origin,
        goal.transform.position,
        lateralAmplitude,
        forwardFraction,
        oscillations,
        1f,
        fallbackLegs,
        false,
        0f,
        0f,
        0f,
        seed,
        MaxGenerationAttempts,
        out route);
      generationAttempts = MaxGenerationAttempts;
      return route != null;
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

    public static int CreateSeed() => Guid.NewGuid().GetHashCode();

    private static float EvaluateLateralSeries(
      float normalizedProgress, float oscillations, float harmonicWeight,
      float phase, float secondHarmonicPhase)
    {
      var firstHarmonic = 2f * Mathf.PI * oscillations * normalizedProgress + phase;
      var secondHarmonic = 4f * Mathf.PI * oscillations * normalizedProgress + secondHarmonicPhase;
      return Mathf.Sin(firstHarmonic) + harmonicWeight * Mathf.Sin(secondHarmonic);
    }

    private static float CalculateLateralNormalization(
      float oscillations, float harmonicWeight, float phase, float secondHarmonicPhase)
    {
      var originOffset = EvaluateLateralSeries(
        0f, oscillations, harmonicWeight, phase, secondHarmonicPhase);
      var maximum = 0f;
      for (var i = 0; i <= LateralNormalizationSamples; i++)
      {
        var normalizedProgress = i / (float)LateralNormalizationSamples;
        var centeredOffset = EvaluateLateralSeries(
          normalizedProgress, oscillations, harmonicWeight, phase, secondHarmonicPhase)
          - originOffset;
        maximum = Mathf.Max(maximum, Mathf.Abs(centeredOffset));
      }

      return Mathf.Max(0.000001f, maximum);
    }

    private static bool MeetsGenerationConstraints(
      TimerRoute route, float maxTurnAngle, int minCheckpointsPerTier)
    {
      if (route.MaxTurnAngleDeg > Mathf.Max(0f, maxTurnAngle))
        return false;

      var tierCounts = new int[3];
      for (var i = 0; i < route.CheckpointCount; i++)
      {
        var normalizedProgress = route.NormalizeProgress(route.GetCheckpointProgress(i));
        var tier = normalizedProgress < HouseSet.RouteT1End
          ? 0
          : normalizedProgress < HouseSet.RouteT2End ? 1 : 2;
        tierCounts[tier]++;
      }

      var minimum = Mathf.Max(0, minCheckpointsPerTier);
      return tierCounts[0] >= minimum
        && tierCounts[1] >= minimum
        && tierCounts[2] >= minimum;
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

    private static float[] BuildCheckpointTurnAngles(IReadOnlyList<Vector3> points)
    {
      var turnAngles = new float[HopCount + 1];
      for (var i = 0; i < turnAngles.Length; i++)
        turnAngles[i] = CalculateTurnAngle(points[i], points[i + 1], points[i + 2]);

      return turnAngles;
    }

    private static float[] BuildCheckpointLateralOffsets(
      IReadOnlyList<Vector3> checkpointPoints, Vector3 origin, Vector3 side)
    {
      var lateralOffsets = new float[checkpointPoints.Count];
      for (var i = 0; i < lateralOffsets.Length; i++)
        lateralOffsets[i] = Vector3.Dot(checkpointPoints[i] - origin, side);

      return lateralOffsets;
    }

    private static float CalculateTurnAngle(Vector3 previous, Vector3 current, Vector3 next)
    {
      var incoming = current - previous;
      var outgoing = next - current;
      incoming.y = 0f;
      outgoing.y = 0f;
      if (incoming.sqrMagnitude < 0.000001f || outgoing.sqrMagnitude < 0.000001f)
        return 0f;

      return Vector3.Angle(incoming, outgoing);
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

    public float GetCheckpointTurnAngle(int checkpointIndex)
    {
      if (checkpointIndex < 0 || checkpointIndex >= _checkpointTurnAngles.Length)
        return 0f;

      return _checkpointTurnAngles[checkpointIndex];
    }

    public float GetCheckpointLateralOffset(int checkpointIndex)
    {
      if (checkpointIndex < 0 || checkpointIndex >= _checkpointLateralOffsets.Length)
        return 0f;

      return _checkpointLateralOffsets[checkpointIndex];
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
