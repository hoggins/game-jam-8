using System;
using System.Collections.Generic;
using App;
using Combat;
using Destruction;
using Movement;
using Pooling;
using UnityEngine;
using VContainer;

namespace Weapons
{
  [DisallowMultipleComponent]
  public sealed class Bullet : MonoBehaviour
  {
    private const float ProjectileScaleGrowth = 0.1f;

    [SerializeField, Min(0f)] private float _speed = 20f;
    [SerializeField, Min(0.01f)] private float _lifetime = 3f;
    [SerializeField, Min(0.001f)] private float _radius = 0.05f;

    [Header("Hit FX")]
    [SerializeField] private GameObject _hitFxPrefab;
    [SerializeField, Min(0)] private int _hitFxPrewarmCount = 8;

    [Inject] private Pool _pool;
    [Inject] private MovementUpdater _movementUpdater;

    private readonly RaycastHit[] _hits = new RaycastHit[16];
    private readonly List<MovementAgent> _targets = new(16);

    private Transform _owner;
    private Vector3 _direction;
    private float _launchSpeed;
    private float _remainingLifetime;
    private int _damage;
    private int _collisionLayerMask;
    private bool _launched;

    private void Awake()
    {
      this.AsInjected();
      _collisionLayerMask = LayerMask.GetMask("Actors", DestructibleLayers.Damagable);
      _pool?.Prewarm(_hitFxPrefab, _hitFxPrewarmCount);
    }

    private void OnEnable() =>
      _launched = false;

    private void OnDisable()
    {
      _launched = false;
      _owner = null;
    }

    public void Launch(
      Vector3 direction,
      int damage,
      Transform owner,
      float scale = 1f,
      float speedMultiplier = 1f)
    {
      if (damage < 0)
        throw new ArgumentOutOfRangeException(nameof(damage), damage, "Damage cannot be negative.");

      if (direction.sqrMagnitude <= Mathf.Epsilon)
        direction = transform.forward;

      _direction = direction.normalized;
      _damage = damage;
      _owner = owner;
      scale = Mathf.Max(0f, scale);
      speedMultiplier = Mathf.Max(0f, speedMultiplier);
      var projectileScale = Mathf.Lerp(1f, scale, ProjectileScaleGrowth);
      _launchSpeed = _speed * projectileScale * speedMultiplier;
      _remainingLifetime = _lifetime * projectileScale;
      _launched = true;
    }

    private void Update()
    {
      if (!_launched)
        return;

      var deltaTime = Time.deltaTime;
      if (deltaTime <= 0f)
        return;

      var distance = _launchSpeed * deltaTime;
      if (distance > 0f && TryHit(distance))
        return;

      transform.position += _direction * distance;
      _remainingLifetime -= deltaTime;
      if (_remainingLifetime <= 0f)
        Release();
    }

    private bool TryHit(float distance)
    {
      var hasHit = TryGetMobHit(distance, out var mob, out var mobHitDistance);
      var damageable = mob as IDamageable;
      var closestDistance = mobHitDistance;
      var hitPosition = transform.position;
      var hitRotation = Quaternion.LookRotation(_direction, Vector3.up);

      if (hasHit)
        hitPosition += _direction * closestDistance;

      if (TryGetPhysicsHit(distance, out var physicsHit)
          && (!hasHit || physicsHit.distance < closestDistance))
      {
        hasHit = true;
        closestDistance = physicsHit.distance;
        damageable = FindDamageable(physicsHit.collider);
        hitPosition = physicsHit.point;
        hitRotation = Quaternion.LookRotation(physicsHit.normal, Vector3.up);
      }

      if (!hasHit)
        return false;

      if (damageable != null && damageable.IsAlive && IsOnScreen(hitPosition))
      {
        if (damageable is IImpactDamageable impactDamageable)
          impactDamageable.TakeDamage(_damage, transform.position);
        else
          damageable.TakeDamage(_damage);
      }

      SpawnHitFx(hitPosition, hitRotation);

      // A bullet is consumed by the first non-owner collider on a damageable or actor layer.
      Release();
      return true;
    }

    private static bool IsOnScreen(Vector3 worldPosition)
    {
      var camera = Camera.main;
      if (camera == null)
        return true;

      var viewportPosition = camera.WorldToViewportPoint(worldPosition);
      return viewportPosition.z > 0f
        && viewportPosition.x >= 0f
        && viewportPosition.x <= 1f
        && viewportPosition.y >= 0f
        && viewportPosition.y <= 1f;
    }

    private bool TryGetMobHit(
      float distance,
      out Mob closestMob,
      out float closestDistance)
    {
      closestMob = null;
      closestDistance = float.PositiveInfinity;
      if (_movementUpdater == null)
        return false;

      var start = transform.position;
      var middle = start + _direction * (distance * 0.5f);
      _movementUpdater.QueryCircle(
        middle,
        distance * 0.5f + _radius,
        MovementLayer.Mob,
        _targets);

      for (var i = 0; i < _targets.Count; i++)
      {
        var target = _targets[i];
        if (target == null || !target.isActiveAndEnabled || target.Controller == null)
          continue;

        var mob = target.GetComponent<Mob>();
        if (mob == null || !mob.IsAlive)
          continue;

        var offset = target.Position - start;
        offset.y = 0f;
        var projectedDistance = Mathf.Clamp(Vector3.Dot(offset, _direction), 0f, distance);
        var closestPoint = start + _direction * projectedDistance;
        var combinedRadius = _radius + target.Controller.Radius;
        if ((target.Position - closestPoint).sqrMagnitude > combinedRadius * combinedRadius)
          continue;

        var alongHitOffset = Mathf.Sqrt(Mathf.Max(
          0f,
          combinedRadius * combinedRadius
          - (target.Position - closestPoint).sqrMagnitude));
        var hitDistance = Mathf.Max(0f, projectedDistance - alongHitOffset);
        if (hitDistance >= closestDistance)
          continue;

        closestMob = mob;
        closestDistance = hitDistance;
      }

      return closestMob != null;
    }

    private bool TryGetPhysicsHit(float distance, out RaycastHit closestHit)
    {
      var hitCount = Physics.SphereCastNonAlloc(
        transform.position,
        _radius,
        _direction,
        _hits,
        distance,
        _collisionLayerMask,
        QueryTriggerInteraction.Ignore);

      var closestDistance = float.PositiveInfinity;
      closestHit = default;

      for (var i = 0; i < hitCount; i++)
      {
        var hit = _hits[i];
        if (hit.collider == null
            || IsOwnerCollider(hit.collider)
            || IsBulletCollider(hit.collider)
            || hit.distance >= closestDistance)
          continue;

        closestDistance = hit.distance;
        closestHit = hit;
      }

      return closestDistance < float.PositiveInfinity;
    }

    private bool IsOwnerCollider(Collider collider) =>
      _owner != null
      && (collider.transform == _owner || collider.transform.IsChildOf(_owner));

    private bool IsBulletCollider(Collider collider) =>
      collider.transform == transform || collider.transform.IsChildOf(transform);

    private void SpawnHitFx(Vector3 position, Quaternion rotation)
    {
      if (_pool == null || _hitFxPrefab == null)
        return;

      _pool.Get(_hitFxPrefab, position, rotation);
    }

    private static IDamageable FindDamageable(Collider collider)
    {
      var components = collider.GetComponentsInParent<MonoBehaviour>();
      for (var i = 0; i < components.Length; i++)
      {
        if (components[i] is IDamageable damageable)
          return damageable;
      }

      return null;
    }

    private void Release()
    {
      _launched = false;
      if (_pool != null)
        _pool.Release(gameObject);
      else
        gameObject.SetActive(false);
    }

    private void OnValidate()
    {
      _speed = Mathf.Max(0f, _speed);
      _lifetime = Mathf.Max(0.01f, _lifetime);
      _radius = Mathf.Max(0.001f, _radius);
      _hitFxPrewarmCount = Mathf.Max(0, _hitFxPrewarmCount);
    }
  }
}
