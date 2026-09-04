using System.Collections;
using System;
using System.Collections.Generic;
using App;
using Balance;
using Model;
using Movement;
using Pooling;
using UnityEngine;
using VContainer;

namespace Combat
{
  [DisallowMultipleComponent]
  public sealed class Mob : MonoBehaviour, IDamageable
  {
    [SerializeField, Min(0f)] private float _deathDissolveDuration = 1f;
    [SerializeField] private AnimationCurve _deathDissolveCurve = DefaultDissolveCurve();

    [Header("Throw (death while attached)")]
    [SerializeField, Min(0f)] private float _throwDistance = 10f;
    [SerializeField, Min(0f)] private float _throwLaunchAngle = 45f;
    [SerializeField, Min(0f)] private float _throwSpinTorque = 5f;
    [SerializeField, Min(0f)] private float _throwColliderRadius = 0.5f;
    [SerializeField, Min(0f)] private float _throwColliderHeight = 1.5f;
    [SerializeField, Min(0f)] private float _throwSettleDuration = 0.5f;

    [Header("Attach")]
    [SerializeField, Min(0f)] private float _attachDuration = 0.35f;
    [SerializeField] private AnimationCurve _attachPositionCurve = DefaultAttachCurve();
    [SerializeField] private AnimationCurve _attachRotationCurve = DefaultAttachCurve();
    [SerializeField] private Vector3 _attachRotationMin = new(-30f, -180f, -30f);
    [SerializeField] private Vector3 _attachRotationMax = new(30f, 180f, 30f);

    [Inject] private Pool _pool;
    [Inject] private CharacterService _characterService;
    [Inject] private BattleService _battleService;

    private const string CoinPickupPrefabPath = "Prefabs/Interface/Coin01";

    private static readonly int InflateId = Shader.PropertyToID("_Inflate");
    private static GameObject _coinPickupPrefab;

    private MovementAgent _movementAgent;
    private Renderer[] _renderers;
    private MaterialPropertyBlock _propertyBlock;
    private Transform _player;
    private Coroutine _deathCoroutine;
    private Coroutine _attachCoroutine;
    private Rigidbody _throwRigidbody;
    private Collider _throwCollider;
    private bool _isDying;
    private bool _hasAttacked;
    private bool _isAttached;

    public int CurrentHealth { get; private set; }
    public bool IsAlive => CurrentHealth > 0 && !_isDying;
    internal bool IsAttached => _isAttached;

    private void Awake()
    {
      this.AsInjected();
      _movementAgent = GetComponent<MovementAgent>();
      _renderers = GetComponentsInChildren<Renderer>(true);
      _propertyBlock = new MaterialPropertyBlock();
      CurrentHealth = BattleBalance.DuckMaxHealth;
    }

    private void Start() =>
      _pool?.Register(gameObject);

    private void OnEnable()
    {
      CurrentHealth = BattleBalance.DuckMaxHealth;
      _isDying = false;
      _hasAttacked = false;
      _isAttached = false;
      _player = null;
      SetDissolve(0f);

      if (_movementAgent != null)
        _movementAgent.enabled = true;
    }

    private void Update()
    {
      if (!IsAlive || _hasAttacked)
        return;

      if (_battleService != null && _battleService.IsCombatSuspended)
        return;

      ResolvePlayer();
      if (_player == null || !IsWithinAttackDistance())
        return;

      _hasAttacked = true;
      _characterService?.TakeDamage(BattleBalance.DuckAttackDamage);
      AttachToPlayer();
    }

    public void TakeDamage(int damage)
    {
      if (damage < 0)
        throw new ArgumentOutOfRangeException(nameof(damage), damage, "Damage cannot be negative.");

      if (!IsAlive)
        return;

      CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
      if (CurrentHealth == 0)
        BeginDeath();
    }

    private void BeginDeath()
    {
      _isDying = true;
      SpawnCoinPickups(_characterService?.RegisterDuckKill() ?? 0);
      // Movement and any in-flight attach animation keep running: a dying duck walks
      // out its dissolve. The IsAlive guard in Update is what stops it dealing damage.
      _deathCoroutine = StartCoroutine(PlayDeath());
    }

    private void SpawnCoinPickups(int count)
    {
      if (count <= 0 || _pool == null)
        return;

      var prefab = CoinPickupPrefab;
      if (prefab == null)
        return;

      for (var i = 0; i < count; i++)
        _pool.Get(prefab, transform.position, Quaternion.identity);
    }

    private static GameObject CoinPickupPrefab =>
      _coinPickupPrefab = _coinPickupPrefab != null
        ? _coinPickupPrefab
        : Resources.Load<GameObject>(CoinPickupPrefabPath);

    private void AttachToPlayer()
    {
      if (_player == null)
        return;

      var startPosition = transform.position;
      var startRotation = transform.rotation;
      var targetPosition = GetRandomClosestPoint(_player, startPosition);
      var targetLocalPosition = _player.InverseTransformPoint(targetPosition);
      var targetLocalRotation = Quaternion.Euler(
        RandomBetween(_attachRotationMin, _attachRotationMax));

      if (_movementAgent != null)
        _movementAgent.enabled = false;

      _isAttached = true;
      transform.SetParent(_player, true);

      var startLocalPosition = transform.localPosition;
      var startLocalRotation = transform.localRotation;
      _attachCoroutine = StartCoroutine(AnimateAttachment(
        startLocalPosition,
        startLocalRotation,
        targetLocalPosition,
        targetLocalRotation));
    }

    private IEnumerator AnimateAttachment(
      Vector3 startPosition,
      Quaternion startRotation,
      Vector3 targetPosition,
      Quaternion targetRotation)
    {
      if (_attachDuration <= 0f)
      {
        transform.localPosition = targetPosition;
        transform.localRotation = targetRotation;
        _attachCoroutine = null;
        yield break;
      }

      var elapsed = 0f;
      while (elapsed < _attachDuration)
      {
        elapsed += Time.deltaTime;
        var progress = Mathf.Clamp01(elapsed / _attachDuration);
        var positionProgress = Evaluate(_attachPositionCurve, progress);
        var rotationProgress = Evaluate(_attachRotationCurve, progress);

        transform.localPosition = Vector3.LerpUnclamped(
          startPosition,
          targetPosition,
          positionProgress);
        transform.localRotation = Quaternion.SlerpUnclamped(
          startRotation,
          targetRotation,
          rotationProgress);
        yield return null;
      }

      transform.localPosition = targetPosition;
      transform.localRotation = targetRotation;
      _attachCoroutine = null;
    }

    private bool IsWithinAttackDistance()
    {
      var offset = transform.position - _player.position;
      offset.y = 0f;
      return offset.sqrMagnitude <= BattleBalance.DuckAttackDistance * BattleBalance.DuckAttackDistance;
    }

    private void ResolvePlayer()
    {
      if (_player == null)
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    private static Vector3 GetRandomClosestPoint(
      Transform actor,
      Vector3 sourcePosition)
    {
      var colliders = actor.GetComponentsInChildren<Collider>(true);
      if (TryGetRandomClosestPoint(colliders, sourcePosition, out var colliderPoint))
        return colliderPoint;

      var renderers = actor.GetComponentsInChildren<Renderer>(true);
      if (TryGetRandomClosestPoint(renderers, sourcePosition, out var rendererPoint))
        return rendererPoint;

      return actor.position;
    }

    private static bool TryGetRandomClosestPoint(
      Collider[] colliders,
      Vector3 sourcePosition,
      out Vector3 closestPoint)
    {
      closestPoint = default;
      var closestDistance = float.PositiveInfinity;
      var equallyCloseCount = 0;

      for (var i = 0; i < colliders.Length; i++)
      {
        var collider = colliders[i];
        if (collider == null || !collider.enabled)
          continue;

        var candidate = collider.ClosestPoint(sourcePosition);
        var distance = (candidate - sourcePosition).sqrMagnitude;
        if (distance < closestDistance - 0.0001f)
        {
          closestDistance = distance;
          closestPoint = candidate;
          equallyCloseCount = 1;
        }
        else if (Mathf.Abs(distance - closestDistance) <= 0.0001f
                 && UnityEngine.Random.Range(0, ++equallyCloseCount) == 0)
        {
          closestPoint = candidate;
        }
      }

      return closestDistance < float.PositiveInfinity;
    }

    private static bool TryGetRandomClosestPoint(
      Renderer[] renderers,
      Vector3 sourcePosition,
      out Vector3 closestPoint)
    {
      closestPoint = default;
      var closestDistance = float.PositiveInfinity;
      var equallyCloseCount = 0;

      for (var i = 0; i < renderers.Length; i++)
      {
        var renderer = renderers[i];
        if (renderer == null || !renderer.enabled)
          continue;

        var candidate = renderer.bounds.ClosestPoint(sourcePosition);
        var distance = (candidate - sourcePosition).sqrMagnitude;
        if (distance < closestDistance - 0.0001f)
        {
          closestDistance = distance;
          closestPoint = candidate;
          equallyCloseCount = 1;
        }
        else if (Mathf.Abs(distance - closestDistance) <= 0.0001f
                 && UnityEngine.Random.Range(0, ++equallyCloseCount) == 0)
        {
          closestPoint = candidate;
        }
      }

      return closestDistance < float.PositiveInfinity;
    }

    private static Vector3 RandomBetween(Vector3 min, Vector3 max) =>
      new(
        RandomBetween(min.x, max.x),
        RandomBetween(min.y, max.y),
        RandomBetween(min.z, max.z));

    private static float RandomBetween(float min, float max) =>
      UnityEngine.Random.Range(Mathf.Min(min, max), Mathf.Max(min, max));

    private static float Evaluate(AnimationCurve curve, float progress) =>
      curve == null ? progress : curve.Evaluate(progress);

    private static AnimationCurve DefaultAttachCurve() =>
      AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private static AnimationCurve DefaultDissolveCurve() =>
      AnimationCurve.Linear(0f, 0f, 1f, 1f);

    private IEnumerator PlayDeath()
    {
      if (_isAttached)
      {
        ThrowOffPlayer();
        yield return new WaitForSeconds(_throwSettleDuration);
      }

      var elapsed = 0f;
      while (elapsed < _deathDissolveDuration)
      {
        elapsed += Time.deltaTime;
        var progress = Mathf.Clamp01(elapsed / _deathDissolveDuration);
        SetDissolve(Mathf.Clamp01(Evaluate(_deathDissolveCurve, progress)));
        yield return null;
      }

      SetDissolve(1f);

      _deathCoroutine = null;
      DetachFromPlayer();
      RemoveThrowPhysics();
      _pool?.Release(gameObject);
    }

    // A duck that died mid-attach gets knocked off the player instead of just
    // dissolving in place. Rigidbody/Collider are added only for this instant,
    // since most ducks never attach and don't need to carry physics components.
    private void ThrowOffPlayer()
    {
      var throwOrigin = _player != null ? _player.position : transform.position;
      DetachFromPlayer();

      if (_movementAgent != null)
        _movementAgent.enabled = false;

      var direction = transform.position - throwOrigin;
      direction.y = 0f;
      if (direction.sqrMagnitude < 0.0001f)
        direction = UnityEngine.Random.insideUnitSphere;
      direction.Normalize();

      var capsule = gameObject.AddComponent<CapsuleCollider>();
      capsule.radius = _throwColliderRadius;
      capsule.height = _throwColliderHeight;
      capsule.center = new Vector3(0f, _throwColliderHeight * 0.5f, 0f);
      _throwCollider = capsule;

      var rigidbody = gameObject.AddComponent<Rigidbody>();
      var gravity = Mathf.Abs(Physics.gravity.y);
      var launchSpeed = gravity > 0f ? Mathf.Sqrt(_throwDistance * gravity) : 0f;
      var angleRad = _throwLaunchAngle * Mathf.Deg2Rad;
      var launchVelocity = direction * (launchSpeed * Mathf.Cos(angleRad))
        + Vector3.up * (launchSpeed * Mathf.Sin(angleRad));
      rigidbody.AddForce(launchVelocity, ForceMode.VelocityChange);
      rigidbody.AddTorque(UnityEngine.Random.insideUnitSphere * _throwSpinTorque, ForceMode.VelocityChange);
      _throwRigidbody = rigidbody;
    }

    private void RemoveThrowPhysics()
    {
      if (_throwRigidbody != null)
      {
        Destroy(_throwRigidbody);
        _throwRigidbody = null;
      }

      if (_throwCollider != null)
      {
        Destroy(_throwCollider);
        _throwCollider = null;
      }
    }

    private void SetDissolve(float value)
    {
      if (_renderers == null || _propertyBlock == null)
        return;

      for (var i = 0; i < _renderers.Length; i++)
      {
        var renderer = _renderers[i];
        if (renderer == null)
          continue;

        renderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetFloat(InflateId, value);
        renderer.SetPropertyBlock(_propertyBlock);
      }
    }

    private void DetachFromPlayer()
    {
      if (transform.parent == _player)
        transform.SetParent(null, true);

      _isAttached = false;
    }

    private void OnDisable()
    {
      if (_deathCoroutine != null)
      {
        StopCoroutine(_deathCoroutine);
        _deathCoroutine = null;
      }

      if (_attachCoroutine != null)
      {
        StopCoroutine(_attachCoroutine);
        _attachCoroutine = null;
      }

      _isAttached = false;
      RemoveThrowPhysics();
    }

    private void OnValidate()
    {
      _deathDissolveDuration = Mathf.Max(0f, _deathDissolveDuration);
      _deathDissolveCurve ??= DefaultDissolveCurve();
      _throwDistance = Mathf.Max(0f, _throwDistance);
      _throwLaunchAngle = Mathf.Clamp(_throwLaunchAngle, 0f, 90f);
      _throwSpinTorque = Mathf.Max(0f, _throwSpinTorque);
      _throwColliderRadius = Mathf.Max(0f, _throwColliderRadius);
      _throwColliderHeight = Mathf.Max(0f, _throwColliderHeight);
      _throwSettleDuration = Mathf.Max(0f, _throwSettleDuration);
      _attachDuration = Mathf.Max(0f, _attachDuration);
      _attachPositionCurve ??= DefaultAttachCurve();
      _attachRotationCurve ??= DefaultAttachCurve();
    }
  }
}
