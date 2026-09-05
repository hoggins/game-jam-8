using System;
using Destruction;
using UnityEngine;

namespace Movement
{
  [DisallowMultipleComponent]
  [RequireComponent(typeof(Collider))]
  public sealed class FlowMapNoGoZone : MonoBehaviour
  {
    private readonly Vector2[] _boxPoints = new Vector2[8];
    private readonly Vector2[] _boxFootprint = new Vector2[16];

    private int _boxFootprintCount;
    private bool _initialized;
    private Bounds _worldBounds;

    internal event Action<FlowMapNoGoZone, bool> ActiveChanged;
    internal event Action<FlowMapNoGoZone> Destroyed;

    internal Collider Collider { get; private set; }
    internal bool IgnoreMobs { get; private set; }
    internal Bounds WorldBounds => _worldBounds;

    private void Awake() =>
      Initialize();

    private void OnEnable()
    {
      RefreshCache();
      ActiveChanged?.Invoke(this, true);
    }

    private void OnDisable() =>
      ActiveChanged?.Invoke(this, false);

    internal void Initialize()
    {
      if (_initialized)
        return;

      if (Collider == null)
        Collider = GetComponent<Collider>();

      // Sidewalk props are destructible, but mobs are allowed to walk through them. Keep this
      // classification on the no-go zone so both the flow map and wall collision use the same rule.
      var health = GetComponent<DestructibleHealth>();
      IgnoreMobs = CompareTag("Prop")
                   || (health != null && health.ObjectType == DestructibleObjectType.Prop);
      _initialized = true;
    }

    internal void RefreshCache()
    {
      Initialize();
      CacheBoxFootprint();
      _worldBounds = Collider != null ? Collider.bounds : default;
    }

    internal bool OverlapsCircle(Vector2 center, float radius) =>
      TryGetCirclePushOut(center, radius, out _);

    internal bool TryGetCirclePushOut(Vector2 center, float radius, out Vector2 push)
    {
      Initialize();
      if (Collider == null)
      {
        push = Vector2.zero;
        return false;
      }

      if (_boxFootprintCount < 3)
        return TryGetBoundsPushOut(center, radius, out push);

      var inside = true;
      var closestPoint = Vector2.zero;
      var closestDistanceSq = float.PositiveInfinity;

      for (var i = 0; i < _boxFootprintCount; i++)
      {
        var start = _boxFootprint[i];
        var end = _boxFootprint[(i + 1) % _boxFootprintCount];
        if (Cross(start, end, center) < 0f)
          inside = false;

        var point = ClosestPointOnSegment(center, start, end);
        var distanceSq = (center - point).sqrMagnitude;
        if (distanceSq < closestDistanceSq)
        {
          closestDistanceSq = distanceSq;
          closestPoint = point;
        }
      }

      var distance = Mathf.Sqrt(closestDistanceSq);
      if (!inside && distance >= radius)
      {
        push = Vector2.zero;
        return false;
      }

      var direction = inside ? closestPoint - center : center - closestPoint;
      if (direction.sqrMagnitude <= 0.000001f)
        direction = Vector2.right;

      var pushDistance = inside ? radius + distance : radius - distance;
      push = direction.normalized * pushDistance;
      return true;
    }

    private bool TryGetBoundsPushOut(Vector2 center, float radius, out Vector2 push)
    {
      var bounds = Collider.bounds;
      var min = new Vector2(bounds.min.x, bounds.min.z);
      var max = new Vector2(bounds.max.x, bounds.max.z);
      var closest = new Vector2(
        Mathf.Clamp(center.x, min.x, max.x),
        Mathf.Clamp(center.y, min.y, max.y));
      var inside = center.x >= min.x && center.x <= max.x
                   && center.y >= min.y && center.y <= max.y;

      if (inside)
      {
        var left = center.x - min.x;
        var right = max.x - center.x;
        var bottom = center.y - min.y;
        var top = max.y - center.y;
        var distance = Mathf.Min(left, right, bottom, top);

        if (distance == left)
          push = Vector2.left * (radius + left);
        else if (distance == right)
          push = Vector2.right * (radius + right);
        else if (distance == bottom)
          push = Vector2.down * (radius + bottom);
        else
          push = Vector2.up * (radius + top);

        return true;
      }

      var away = center - closest;
      var outsideDistance = away.magnitude;
      if (outsideDistance >= radius)
      {
        push = Vector2.zero;
        return false;
      }

      push = away.normalized * (radius - outsideDistance);
      return true;
    }

    private Vector2 ClosestPointOnSegment(Vector2 point, Vector2 start, Vector2 end)
    {
      var segment = end - start;
      var lengthSq = segment.sqrMagnitude;
      if (lengthSq <= 0.000001f)
        return start;

      var t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSq);
      return start + segment * t;
    }

    private void CacheBoxFootprint()
    {
      _boxFootprintCount = 0;
      if (Collider is not BoxCollider boxCollider)
        return;

      var center = boxCollider.center;
      var extents = boxCollider.size * 0.5f;
      var pointCount = 0;

      for (var x = -1; x <= 1; x += 2)
      for (var y = -1; y <= 1; y += 2)
      for (var z = -1; z <= 1; z += 2)
      {
        var localPoint = center + Vector3.Scale(extents, new Vector3(x, y, z));
        var worldPoint = transform.TransformPoint(localPoint);
        _boxPoints[pointCount++] = new Vector2(worldPoint.x, worldPoint.z);
      }

      SortAndRemoveDuplicatePoints(ref pointCount);
      BuildConvexHull(pointCount);
    }

    private void SortAndRemoveDuplicatePoints(ref int pointCount)
    {
      for (var i = 1; i < pointCount; i++)
      {
        var point = _boxPoints[i];
        var insertionIndex = i;
        while (insertionIndex > 0 && ComesBefore(point, _boxPoints[insertionIndex - 1]))
        {
          _boxPoints[insertionIndex] = _boxPoints[insertionIndex - 1];
          insertionIndex--;
        }

        _boxPoints[insertionIndex] = point;
      }

      var uniqueCount = 0;
      for (var i = 0; i < pointCount; i++)
      {
        if (uniqueCount > 0
            && (_boxPoints[i] - _boxPoints[uniqueCount - 1]).sqrMagnitude < 0.000001f)
          continue;

        _boxPoints[uniqueCount++] = _boxPoints[i];
      }

      pointCount = uniqueCount;
    }

    private void BuildConvexHull(int pointCount)
    {
      if (pointCount < 3)
        return;

      var hullCount = 0;
      for (var i = 0; i < pointCount; i++)
      {
        while (hullCount >= 2
               && Cross(_boxFootprint[hullCount - 2], _boxFootprint[hullCount - 1], _boxPoints[i]) <= 0f)
          hullCount--;

        _boxFootprint[hullCount++] = _boxPoints[i];
      }

      var lowerHullCount = hullCount;
      for (var i = pointCount - 2; i >= 0; i--)
      {
        while (hullCount > lowerHullCount
               && Cross(_boxFootprint[hullCount - 2], _boxFootprint[hullCount - 1], _boxPoints[i]) <= 0f)
          hullCount--;

        _boxFootprint[hullCount++] = _boxPoints[i];
      }

      _boxFootprintCount = hullCount - 1;
    }

    private bool ComesBefore(Vector2 first, Vector2 second) =>
      first.x < second.x || (Mathf.Approximately(first.x, second.x) && first.y < second.y);

    private float Cross(Vector2 origin, Vector2 first, Vector2 second) =>
      (first.x - origin.x) * (second.y - origin.y)
      - (first.y - origin.y) * (second.x - origin.x);

    private void OnDestroy() =>
      Destroyed?.Invoke(this);
  }
}
