using System;
using App;
using Model;
using Pooling;
using UnityEngine;
using VContainer;

namespace Weapons
{
  public abstract class Weapon : MonoBehaviour
  {
    public event Action AttackPerformed;

    [Header("Attack")]
    [SerializeField, Min(0f)] private float _attackCooldown = 0.5f;

    [Header("Attack FX")]
    [SerializeField] private GameObject _attackFxPrefab;
    [SerializeField, Min(0)] private int _attackFxPrewarmCount = 8;
    [SerializeField] private Transform _attackFxPoint;
    [SerializeField] private bool _attachAttackFxToPlayer;

    [Inject] private CharacterService _playerService;
    [Inject] private BattleService _battleService;
    [Inject] private Pool _pool;

    private float _nextAttackTime;

    protected CharacterService PlayerService => _playerService;
    protected Pool Pool => _pool;

    // Melee attacks are the attacks that drive the player's attack animation. Other weapons
    // can fire independently without making the player play that animation.
    protected virtual bool NotifyAttackPerformed => true;

    protected virtual void Awake()
    {
      this.AsInjected();

      if (_attackFxPrefab != null)
        PrewarmFx(_attackFxPrefab, _attackFxPrewarmCount);
    }

    private void Update()
    {
      TryAttack();
    }

    public bool TryAttack()
    {
      // Checked before the cooldown is consumed, so the suspension does not eat a swing.
      if (_battleService != null && _battleService.IsCombatSuspended)
        return false;

      if (_playerService == null || Time.time < _nextAttackTime)
        return false;

      _nextAttackTime = Time.time + _attackCooldown;
      SpawnAttackFx();
      Attack(_playerService.AttackPower);
      if (NotifyAttackPerformed)
        AttackPerformed?.Invoke();
      return true;
    }

    protected abstract void Attack(int damage);

    protected virtual void SpawnAttackFx()
    {
      SpawnAttackFx(false);
    }

    protected void SpawnAttackFx(bool mirrored)
    {
      if (_pool == null || _attackFxPrefab == null)
        return;

      var point = _attackFxPoint != null ? _attackFxPoint : transform;
      var parent = _attachAttackFxToPlayer ? transform.root : null;
      var attackFx = SpawnFx(_attackFxPrefab, point.position, point.rotation, parent);
      if (attackFx == null)
        return;

      var scale = attackFx.transform.localScale;
      scale.x = Mathf.Abs(scale.x) * (mirrored ? -1f : 1f);
      attackFx.transform.localScale = scale;
    }

    protected void PrewarmFx(GameObject prefab, int count)
    {
      _pool?.Prewarm(prefab, count);
    }

    protected GameObject SpawnFx(
      GameObject prefab,
      Vector3 position,
      Quaternion rotation,
      Transform parent = null)
    {
      if (_pool == null || prefab == null)
        return null;

      return _pool.Get(prefab, position, rotation, parent);
    }

    private void OnValidate()
    {
      _attackCooldown = Mathf.Max(0f, _attackCooldown);
      _attackFxPrewarmCount = Mathf.Max(0, _attackFxPrewarmCount);
    }
  }
}
