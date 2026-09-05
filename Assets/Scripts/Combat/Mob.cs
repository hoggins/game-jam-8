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
    [Serializable]
    private sealed class MaterialWeight
    {
      [SerializeField] private Material _material;
      [SerializeField, Min(0f)] private float _weight = 1f;

      internal Material Material => _material;
      internal float Weight => _weight;

      internal void Validate() =>
        _weight = Mathf.Max(0f, _weight);
    }

    [Header("Appearance")]
    [SerializeField] private List<MaterialWeight> _materials = new();

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

    private static GameObject _coinPickupPrefab;

    // Tiny registry of currently-attached mobs so MeleeWeapon doesn't need to scan every Mob in
    // the scene (there can be thousands on a big map) just to find the handful stuck to the player.
    private static readonly List<Mob> _attachedMobs = new();
    internal static IReadOnlyList<Mob> AttachedMobs => _attachedMobs;

    private MovementAgent _movementAgent;
    private MobDeath _death;
    private ActorHitAnimation _hitAnimation;
    private Renderer[] _renderers;
    private Transform _player;
    private Coroutine _attachCoroutine;
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
      _death = GetComponent<MobDeath>();
      _hitAnimation = GetComponent<ActorHitAnimation>();
      _renderers = GetComponentsInChildren<Renderer>(true);
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
      ApplyRandomMaterial();
      _death?.ResetVisual();

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
      if (damage > 0)
        _hitAnimation?.PlayHit();

      if (CurrentHealth == 0)
        BeginDeath();
    }

    private void BeginDeath()
    {
      _isDying = true;

      // Dead mobs must stop participating in movement and avoidance immediately. Otherwise an
      // unattached mob can still be moved by MovementUpdater while it dissolves.
      if (_movementAgent != null)
        _movementAgent.enabled = false;

      SpawnCoinPickups(_characterService?.RegisterDuckKill() ?? 0);

      ResolvePlayer();

      if (_attachCoroutine != null)
      {
        StopCoroutine(_attachCoroutine);
        _attachCoroutine = null;
      }

      // MobDeath detaches attached mobs while preserving their current world position, then plays
      // the detach throw from that position.
      _death?.Play(_isAttached ? _player : null, _movementAgent, OnDeathPlayed);
    }

    private void OnDeathPlayed()
    {
      DetachFromPlayer();
      _pool?.Release(gameObject);
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
      _attachedMobs.Add(this);
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
        return AddRandomHeight(actor, colliderPoint);

      var renderers = actor.GetComponentsInChildren<Renderer>(true);
      if (TryGetRandomClosestPoint(renderers, sourcePosition, out var rendererPoint))
        return AddRandomHeight(actor, rendererPoint);

      return AddRandomHeight(actor, actor.position);
    }

    private static Vector3 AddRandomHeight(Transform actor, Vector3 point)
    {
      var hasBounds = false;
      var bounds = default(Bounds);
      var colliders = actor.GetComponentsInChildren<Collider>(true);
      for (var i = 0; i < colliders.Length; i++)
      {
        var collider = colliders[i];
        if (collider == null || !collider.enabled)
          continue;

        if (hasBounds)
          bounds.Encapsulate(collider.bounds);
        else
        {
          bounds = collider.bounds;
          hasBounds = true;
        }
      }

      if (!hasBounds)
        return point;

      var minHeight = Mathf.Max(actor.position.y, bounds.min.y);
      point.y = RandomBetween(minHeight, Mathf.Max(minHeight, bounds.max.y));
      return point;
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

    private void ApplyRandomMaterial()
    {
      var material = PickRandomMaterial();
      if (material == null || _renderers == null)
        return;

      for (var i = 0; i < _renderers.Length; i++)
      {
        var renderer = _renderers[i];
        if (renderer == null)
          continue;

        var materials = renderer.sharedMaterials;
        if (materials == null || materials.Length == 0)
        {
          renderer.sharedMaterial = material;
          continue;
        }

        for (var slot = 0; slot < materials.Length; slot++)
          materials[slot] = material;

        renderer.sharedMaterials = materials;
      }
    }

    private Material PickRandomMaterial()
    {
      var totalWeight = 0f;
      Material lastValidMaterial = null;

      for (var i = 0; i < _materials.Count; i++)
      {
        var entry = _materials[i];
        if (entry == null || entry.Material == null || entry.Weight <= 0f)
          continue;

        totalWeight += entry.Weight;
        lastValidMaterial = entry.Material;
      }

      if (totalWeight <= 0f)
        return null;

      var roll = UnityEngine.Random.value * totalWeight;
      var accumulatedWeight = 0f;
      for (var i = 0; i < _materials.Count; i++)
      {
        var entry = _materials[i];
        if (entry == null || entry.Material == null || entry.Weight <= 0f)
          continue;

        accumulatedWeight += entry.Weight;
        if (roll < accumulatedWeight)
          return entry.Material;
      }

      return lastValidMaterial;
    }

    private static AnimationCurve DefaultAttachCurve() =>
      AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private void DetachFromPlayer()
    {
      if (transform.parent == _player)
        transform.SetParent(null, true);

      _isAttached = false;
      _attachedMobs.Remove(this);
    }

    private void OnDisable()
    {
      if (_attachCoroutine != null)
      {
        StopCoroutine(_attachCoroutine);
        _attachCoroutine = null;
      }

      _isAttached = false;
      _attachedMobs.Remove(this);
    }

    private void OnValidate()
    {
      _attachDuration = Mathf.Max(0f, _attachDuration);
      _attachPositionCurve ??= DefaultAttachCurve();
      _attachRotationCurve ??= DefaultAttachCurve();

      for (var i = 0; i < _materials.Count; i++)
        _materials[i]?.Validate();
    }
  }
}
