using System.Collections.Generic;
using UnityEngine;

namespace Movement
{
  [DisallowMultipleComponent]
  public sealed class MovementUpdater : MonoBehaviour
  {
    private const string PlayerTag = "Player";
    private const float SelfRadiusModifier = 3f;

    [Header("Spatial Map")]
    [SerializeField, Min(0.01f)] private float _spatialCellSize = 2f;

    [Header("Flow Map")]
    [SerializeField, Min(0.01f)] private float _flowCellSize = 1f;
    [SerializeField, Min(0f)] private float _flowPadding = 15f;
    [SerializeField, Min(0)] private int _flowTargetCellDeviation = 2;
    [SerializeField, Min(1)] private int _maxFlowCellCount = 262144;

    [Header("Wall Collision")]
    [SerializeField, Min(0f)] private float _wallSkin = 0.02f;
    [SerializeField, Range(1, 4)] private int _wallSlideIterations = 3;

    [Header("Flocking")]
    [SerializeField, Min(0f)] private float _avoidanceSpread = 2f;
    [SerializeField, Min(0.01f)] private float _avoidanceTargetDistance = 3f;

    private static MovementUpdater _activeUpdater;

    private readonly FlowMap _flowMap = new();
    private readonly SpatialMap _spatialMap = new();
    private readonly List<MovementAgent> _neighbors = new(16);
    private readonly List<FlowMapNoGoZone> _knownNoGoZones = new();
    private readonly List<FlowMapNoGoZone> _noGoZones = new();
    private readonly RaycastHit[] _wallHits = new RaycastHit[64];
    private readonly Collider[] _wallOverlaps = new Collider[64];

    private int _noGoZoneRevision;

    internal FlowMap FlowMap => _flowMap;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() =>
      _activeUpdater = null;

    private void Awake()
    {
      var noGoZones = FindObjectsByType<FlowMapNoGoZone>(
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

    private void OnEnable()
    {
      if (_activeUpdater == null)
        _activeUpdater = this;
    }

    private void OnDisable()
    {
      if (_activeUpdater == this)
        _activeUpdater = null;
    }

    private void OnDestroy()
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
    }

    private void LateUpdate()
    {
      if (_activeUpdater == null)
        _activeUpdater = this;

      if (_activeUpdater != this)
        return;

      Step(Time.deltaTime);
    }

    private void Step(float deltaTime)
    {
      var agents = MovementAgent.ActiveAgents;
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
          flowClearance = Mathf.Max(flowClearance, agent.Controller.Radius + _wallSkin);

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
          _flowCellSize,
          _flowPadding,
          _flowTargetCellDeviation,
          _maxFlowCellCount);
      }

      _spatialMap.Rebuild(agents, _spatialCellSize);

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
      var remaining = displacement;
      var radius = Mathf.Max(0.01f, agent.Controller.Radius);
      position = ResolveWallOverlaps(agent, position, radius);

      for (var iteration = 0; iteration < _wallSlideIterations; iteration++)
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

        var travelDistance = Mathf.Max(0f, hit.distance - _wallSkin);
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
      return position;
    }

    private Vector3 ResolveWallOverlaps(
      MovementAgent agent,
      Vector3 position,
      float radius)
    {
      for (var iteration = 0; iteration < _wallSlideIterations; iteration++)
      {
        var overlapCount = Physics.OverlapSphereNonAlloc(
          position,
          radius + _wallSkin,
          _wallOverlaps,
          Physics.DefaultRaycastLayers,
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

          if (distance > 0.0001f && distance < radius + _wallSkin)
          {
            push = away / distance * (radius + _wallSkin - distance);
          }
          else if (distance <= 0.0001f)
          {
            var zone = wall.GetComponentInParent<FlowMapNoGoZone>();
            if (zone == null
                || !zone.TryGetCirclePushOut(center, radius + _wallSkin, out push))
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
        distance + _wallSkin,
        Physics.DefaultRaycastLayers,
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
          targetDistanceSq / (_avoidanceTargetDistance * _avoidanceTargetDistance));
      }

      var radiusModifier = isStanding
        ? 1f
        : 1f + (SelfRadiusModifier + _avoidanceSpread) * distanceFactor;
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

    private void OnValidate()
    {
      _spatialCellSize = Mathf.Max(0.01f, _spatialCellSize);
      _flowCellSize = Mathf.Max(0.01f, _flowCellSize);
      _flowPadding = Mathf.Max(0f, _flowPadding);
      _flowTargetCellDeviation = Mathf.Max(0, _flowTargetCellDeviation);
      _maxFlowCellCount = Mathf.Max(1, _maxFlowCellCount);
      _wallSkin = Mathf.Max(0f, _wallSkin);
      _wallSlideIterations = Mathf.Clamp(_wallSlideIterations, 1, 4);
      _avoidanceSpread = Mathf.Max(0f, _avoidanceSpread);
      _avoidanceTargetDistance = Mathf.Max(0.01f, _avoidanceTargetDistance);
    }
  }
}
