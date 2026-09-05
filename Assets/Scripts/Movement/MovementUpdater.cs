using System.Collections.Generic;
using Map;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Profiling;
using VContainer.Unity;

namespace Movement
{
  public sealed class MovementUpdater : IInitializable, ILateTickable, System.IDisposable
  {
    private static readonly ProfilerMarker RefreshNoGoZonesMarker =
      new("MovementUpdater.RefreshNoGoZones");

    private const string PlayerTag = "Player";

    // Houses use the Damagable layer for their movement-blocking collider. Visual child meshes
    // use Destructable and must not participate in wall resolution: they can have different
    // geometry from the movement collider and produce incorrect slide normals.
    private static readonly int WallLayerMask =
      1 << LayerMask.NameToLayer("Damagable");

    private readonly MovementSettings _settings;

    private readonly FlowMap _flowMap = new();
    private readonly SpatialMap _spatialMap = new();
    private readonly List<MovementAgent> _activeAgents = new();
    private readonly List<MovementAgent> _neighbors = new(16);
    private readonly List<FlowMapNoGoZone> _knownNoGoZones = new();
    private readonly List<FlowMapNoGoZone> _noGoZones = new();
    private readonly RaycastHit[] _wallHits = new RaycastHit[64];
    private readonly Collider[] _wallOverlaps = new Collider[64];

    private Bounds _levelWorldBounds;
    private bool _hasLevelWorldBounds;
    private int _noGoZoneRevision;

    public MovementUpdater(MovementSettings settings) =>
      _settings = settings;

    internal FlowMap FlowMap => _flowMap;
    internal IReadOnlyList<MovementAgent> ActiveAgents => _activeAgents;
    internal bool HasWalkableFlowMap => _flowMap.HasField;

    internal bool IsWalkable(Vector3 position) =>
      _flowMap.IsWalkable(position);

    internal bool IsInsideFlowMap(Vector3 position) =>
      _flowMap.IsInside(position);

    internal bool IsInsideLevelBounds(Vector3 position, float radius = 0f)
    {
      if (!_hasLevelWorldBounds)
        return true;

      var margin = Mathf.Max(0f, radius + _settings.WallSkin);
      var min = _levelWorldBounds.min;
      var max = _levelWorldBounds.max;
      return position.x >= min.x + margin
             && position.x <= max.x - margin
             && position.z >= min.z + margin
             && position.z <= max.z - margin;
    }

    internal void Register(MovementAgent agent)
    {
      if (!_activeAgents.Contains(agent))
        _activeAgents.Add(agent);
    }

    internal void Unregister(MovementAgent agent) =>
      _activeAgents.Remove(agent);

    internal void QueryCircle(
      MovementAgent origin,
      float radius,
      MovementLayer collisionMask,
      List<MovementAgent> result)
    {
      if (origin == null)
      {
        result.Clear();
        return;
      }

      _spatialMap.QueryCircle(
        origin,
        Mathf.Max(0f, radius),
        collisionMask,
        result);
    }

    internal void QueryCircle(
      Vector3 centerPosition,
      float radius,
      MovementLayer collisionMask,
      List<MovementAgent> result)
    {
      _spatialMap.QueryCircle(
        centerPosition,
        Mathf.Max(0f, radius),
        collisionMask,
        result);
    }

    void IInitializable.Initialize()
    {
      SceneManager.sceneLoaded += OnSceneLoaded;
      RefreshNoGoZones();
      RefreshLevelBounds();
    }

    void ILateTickable.LateTick() =>
      Step(Time.deltaTime);

    void System.IDisposable.Dispose()
    {
      SceneManager.sceneLoaded -= OnSceneLoaded;
      ClearNoGoZones();
      _activeAgents.Clear();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
      RefreshNoGoZones();
      RefreshLevelBounds();
    }

    private void RefreshLevelBounds()
    {
      var levelData = Object.FindFirstObjectByType<LevelData>();
      _hasLevelWorldBounds = levelData != null
                             && levelData.TryGetWorldBounds(out _levelWorldBounds);
    }

    internal void RefreshNoGoZones()
    {
      RefreshNoGoZonesMarker.Begin();
      try
      {
        ClearNoGoZones();

        var noGoZones = Object.FindObjectsByType<FlowMapNoGoZone>(
          FindObjectsInactive.Include,
          FindObjectsSortMode.None);
        for (var i = 0; i < noGoZones.Length; i++)
        {
          var zone = noGoZones[i];
          zone.Initialize();
          _knownNoGoZones.Add(zone);
          zone.ActiveChanged += OnNoGoZoneActiveChanged;
          zone.Destroyed += OnNoGoZoneDestroyed;

          if (zone.isActiveAndEnabled)
            _noGoZones.Add(zone);
        }
      }
      finally
      {
        RefreshNoGoZonesMarker.End();
      }
    }

    private void ClearNoGoZones()
    {
      for (var i = 0; i < _knownNoGoZones.Count; i++)
      {
        var zone = _knownNoGoZones[i];
        if (zone != null)
        {
          zone.ActiveChanged -= OnNoGoZoneActiveChanged;
          zone.Destroyed -= OnNoGoZoneDestroyed;
        }
      }

      _knownNoGoZones.Clear();
      _noGoZones.Clear();
      _noGoZoneRevision++;
    }

    private void Step(float deltaTime)
    {
      var agents = _activeAgents;
      if (agents.Count == 0)
        return;

      MovementAgent player = null;
      var flowClearance = 0f;
      for (var i = 0; i < agents.Count; i++)
      {
        var agent = agents[i];
        if (agent == null || !agent.isActiveAndEnabled)
          continue;

        agent.ReadTransform();
        if (agent.Controller != null)
          flowClearance = Mathf.Max(flowClearance, agent.Controller.Radius + _settings.WallSkin);

        if (player == null && agent.CompareTag(PlayerTag))
          player = agent;
      }

      var hasPlayer = player != null;
      var playerPosition = hasPlayer ? player.Position : Vector3.zero;

      if (hasPlayer)
      {
        _flowMap.Update(
          playerPosition,
          agents,
          _noGoZones,
          _noGoZoneRevision,
          flowClearance,
          _settings.FlowCellSize,
          _settings.FlowPadding,
          _settings.FlowTargetCellDeviation,
          _settings.MaxFlowCellCount);
      }

      _spatialMap.Rebuild(agents, _settings.SpatialCellSize);

      for (var i = 0; i < agents.Count; i++)
      {
        var agent = agents[i];
        if (agent == null || !agent.isActiveAndEnabled || agent.Controller == null)
          continue;

        var context = new MovementContext(agent, _flowMap, playerPosition, hasPlayer);
        agent.DesiredVelocity = agent.Controller.GetDesiredVelocity(context);
      }

      for (var i = 0; i < agents.Count; i++)
      {
        var agent = agents[i];
        if (agent == null || !agent.isActiveAndEnabled || agent.Controller == null)
          continue;

        CalculateResult(agent, playerPosition, hasPlayer, deltaTime);
      }

      for (var i = 0; i < agents.Count; i++)
      {
        var agent = agents[i];
        if (agent != null && agent.isActiveAndEnabled && agent.Controller != null)
          agent.ApplyResult();
      }
    }

    private void OnNoGoZoneDestroyed(FlowMapNoGoZone zone)
    {
      zone.ActiveChanged -= OnNoGoZoneActiveChanged;
      zone.Destroyed -= OnNoGoZoneDestroyed;
      _knownNoGoZones.Remove(zone);

      if (_noGoZones.Remove(zone))
        _noGoZoneRevision++;
    }

    private void OnNoGoZoneActiveChanged(FlowMapNoGoZone zone, bool isActive)
    {
      var changed = isActive
        ? AddNoGoZone(zone)
        : _noGoZones.Remove(zone);

      if (changed)
        _noGoZoneRevision++;
    }

    private bool AddNoGoZone(FlowMapNoGoZone zone)
    {
      if (_noGoZones.Contains(zone))
        return false;

      _noGoZones.Add(zone);
      return true;
    }

    private void CalculateResult(
      MovementAgent agent,
      Vector3 playerPosition,
      bool hasPlayer,
      float deltaTime)
    {
      var controller = agent.Controller;
      var velocity = agent.DesiredVelocity;

      if (controller.AvoidancePower > 0.01f)
      {
        velocity += CalculateAvoidance(agent, playerPosition, hasPlayer) * controller.AvoidancePower;
      }

      velocity = Vector3.ClampMagnitude(velocity, controller.Speed);
      var previousPosition = agent.Position;
      agent.Position = MoveWithWallCollision(agent, velocity * deltaTime);
      agent.Velocity = deltaTime > 0f
        ? (agent.Position - previousPosition) / deltaTime
        : Vector3.zero;
      velocity = agent.Velocity;

      var velocitySmoothing = controller.VelocitySmoothing;
      if (velocitySmoothing > 0f)
      {
        var velocityBlend = 1f - Mathf.Exp(-velocitySmoothing * deltaTime);
        agent.SmoothedVelocity = Vector3.Lerp(agent.SmoothedVelocity, velocity, velocityBlend);
      }
      else
      {
        agent.SmoothedVelocity = velocity;
      }

      var planarVelocity = agent.SmoothedVelocity;
      planarVelocity.y = 0f;
      if (planarVelocity.sqrMagnitude <= 0.0001f)
        return;

      var targetRotation = Quaternion.LookRotation(planarVelocity.normalized, Vector3.up);
      var rotationBlend = 1f - Mathf.Exp(-controller.RotationSpeed * deltaTime);
      agent.Rotation = Quaternion.Slerp(agent.Rotation, targetRotation, rotationBlend);
    }

    private Vector3 MoveWithWallCollision(MovementAgent agent, Vector3 displacement)
    {
      displacement.y = 0f;
      var position = agent.Position;
      var radius = Mathf.Max(0.01f, agent.Controller.Radius);

      // The only colliders these physics queries can ever hit are the no-go zone boxes that
      // already mark flow-field cells blocked, so an agent nowhere near a blocked cell cannot be
      // touching a wall. Props are intentionally omitted from the mob flow map, so keep the
      // player's physical check unconditional; the player still collides with those props.
      var checkRadius = radius + _settings.WallSkin + displacement.magnitude;
      var isPlayer = agent.Controller.Layer == MovementLayer.Player;
      if (!isPlayer && !_flowMap.HasBlockedCellNear(position, checkRadius))
      {
        position += displacement;
        position.y = agent.Position.y;
        return ClampToLevelBounds(position, radius);
      }

      var remaining = displacement;
      position = ResolveWallOverlaps(agent, position, radius);

      for (var iteration = 0; iteration < _settings.WallSlideIterations; iteration++)
      {
        var distance = remaining.magnitude;
        if (distance <= 0.0001f)
          break;

        var direction = remaining / distance;
        if (!TryGetClosestWallHit(agent, position, radius, direction, distance, out var hit))
        {
          position += remaining;
          break;
        }

        var travelDistance = Mathf.Max(0f, hit.distance - _settings.WallSkin);
        position += direction * travelDistance;
        remaining -= direction * travelDistance;

        var wallNormal = hit.normal;
        wallNormal.y = 0f;
        wallNormal = wallNormal.normalized;
        remaining = Vector3.ProjectOnPlane(remaining, wallNormal);
        remaining.y = 0f;

        if (Vector3.Dot(remaining, displacement) <= 0f)
          break;
      }

      position = ResolveWallOverlaps(agent, position, radius);
      position.y = agent.Position.y;
      return ClampToLevelBounds(position, radius);
    }

    private Vector3 ClampToLevelBounds(Vector3 position, float radius)
    {
      if (!_hasLevelWorldBounds)
        return position;

      var margin = Mathf.Max(0f, radius + _settings.WallSkin);
      var min = _levelWorldBounds.min;
      var max = _levelWorldBounds.max;
      position.x = ClampAxis(position.x, min.x + margin, max.x - margin, _levelWorldBounds.center.x);
      position.z = ClampAxis(position.z, min.z + margin, max.z - margin, _levelWorldBounds.center.z);
      return position;
    }

    private static float ClampAxis(float value, float min, float max, float fallback)
    {
      if (min > max)
        return fallback;

      return Mathf.Clamp(value, min, max);
    }

    private Vector3 ResolveWallOverlaps(
      MovementAgent agent,
      Vector3 position,
      float radius)
    {
      for (var iteration = 0; iteration < _settings.WallSlideIterations; iteration++)
      {
        var overlapCount = Physics.OverlapSphereNonAlloc(
          position,
          radius + _settings.WallSkin,
          _wallOverlaps,
          WallLayerMask,
          QueryTriggerInteraction.Ignore);
        var pushed = false;

        for (var i = 0; i < overlapCount; i++)
        {
          var wall = _wallOverlaps[i];
          if (!IsWallCollider(agent, wall))
            continue;

          var center = new Vector2(position.x, position.z);
          var closestPoint3D = wall.ClosestPoint(position);
          var closestPoint = new Vector2(closestPoint3D.x, closestPoint3D.z);
          var away = center - closestPoint;
          var distance = away.magnitude;
          var push = Vector2.zero;

          if (distance > 0.0001f && distance < radius + _settings.WallSkin)
          {
            push = away / distance * (radius + _settings.WallSkin - distance);
          }
          else if (distance <= 0.0001f)
          {
            var zone = wall.GetComponentInParent<FlowMapNoGoZone>();
            if (zone == null
                || !zone.TryGetCirclePushOut(center, radius + _settings.WallSkin, out push))
              continue;
          }

          position.x += push.x;
          position.z += push.y;
          pushed = true;
        }

        if (!pushed)
          break;
      }

      return position;
    }

    private bool TryGetClosestWallHit(
      MovementAgent agent,
      Vector3 origin,
      float radius,
      Vector3 direction,
      float distance,
      out RaycastHit closestHit)
    {
      var hitCount = Physics.SphereCastNonAlloc(
        origin,
        radius,
        direction,
        _wallHits,
        distance + _settings.WallSkin,
        WallLayerMask,
        QueryTriggerInteraction.Ignore);
      var closestDistance = float.PositiveInfinity;
      closestHit = default;

      for (var i = 0; i < hitCount; i++)
      {
        var hit = _wallHits[i];
        var hitCollider = hit.collider;
        if (!IsWallCollider(agent, hitCollider))
          continue;

        var planarNormal = new Vector2(hit.normal.x, hit.normal.z);
        if (planarNormal.sqrMagnitude <= 0.0001f || hit.distance >= closestDistance)
          continue;

        closestDistance = hit.distance;
        closestHit = hit;
      }

      return closestDistance < float.PositiveInfinity;
    }

    private bool IsWallCollider(MovementAgent agent, Collider collider) =>
      collider != null
      && !collider.transform.IsChildOf(agent.transform)
      && collider.GetComponentInParent<MovementAgent>() == null
      && !(agent.Controller.Layer == MovementLayer.Mob
           && collider.GetComponentInParent<FlowMapNoGoZone>()?.IgnoreMobs == true)
      && !Physics.GetIgnoreLayerCollision(
        agent.gameObject.layer,
        collider.gameObject.layer);

    private Vector3 CalculateAvoidance(
      MovementAgent agent,
      Vector3 playerPosition,
      bool hasPlayer)
    {
      var controller = agent.Controller;
      var radius = controller.Radius;
      var isStanding = agent.DesiredVelocity.sqrMagnitude < 0.01f;

      var distanceFactor = 1f;
      if (hasPlayer)
      {
        var targetDistanceSq = (playerPosition - agent.Position).sqrMagnitude - radius * radius;
        distanceFactor = Mathf.Clamp01(
          targetDistanceSq / (_settings.AvoidanceTargetDistance * _settings.AvoidanceTargetDistance));
      }

      var radiusModifier = isStanding
        ? 1f
        : 1f + (_settings.SelfRadiusModifier + _settings.AvoidanceSpread) * distanceFactor;
      var queryRadius = radius * radiusModifier;

      _spatialMap.QueryCircle(agent, queryRadius, controller.CollidesWith, _neighbors);

      var center = new Vector2(agent.Position.x, agent.Position.z);
      var avoidance = Vector2.zero;
      var weight = 0f;

      for (var i = 0; i < _neighbors.Count; i++)
      {
        var other = _neighbors[i];
        var otherPosition = new Vector2(other.Position.x, other.Position.z);
        var away = center - otherPosition;
        var distanceSq = away.sqrMagnitude;
        var combinedRadius = queryRadius + other.Controller.Radius;
        var combinedRadiusSq = combinedRadius * combinedRadius;
        if (combinedRadiusSq <= 0.0001f)
          continue;

        if (distanceSq < 0.01f)
          away = GetUnstuckDirection(agent, other);

        avoidance += away.normalized
                     * (Mathf.Max(0f, combinedRadiusSq - distanceSq) / combinedRadiusSq);
        weight += other.Controller.Radius / Mathf.Max(0.01f, queryRadius);

        if (distanceSq < radius * radius)
          weight = Mathf.Max(weight, 1f);
      }

      var result = avoidance * weight;
      return new Vector3(result.x, 0f, result.y);
    }

    private static Vector2 GetUnstuckDirection(MovementAgent first, MovementAgent second)
    {
      var hash = first.GetInstanceID() * 73856093 ^ second.GetInstanceID() * 19349663;
      var angle = (hash & 1023) / 1024f * Mathf.PI * 2f;
      return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }

  }
}
