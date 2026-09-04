using App;
using Model;
using Pooling;
using UnityEngine;
using VContainer;

namespace Weapons
{
  public abstract class Weapon : MonoBehaviour
  {
    [Header("Attack")]
    [SerializeField, Min(0f)] private float _attackCooldown = 0.5f;

    [Header("Attack FX")]
    [SerializeField] private GameObject _attackFxPrefab;
    [SerializeField, Min(0)] private int _attackFxPrewarmCount = 8;
    [SerializeField] private Transform _attackFxPoint;

    [Inject] private CharacterService _playerService;
    [Inject] private Pool _pool;

    private float _nextAttackTime;

    protected CharacterService PlayerService => _playerService;

    protected virtual void Awake()
    {
      this.AsInjected();

      if (_attackFxPrefab != null)
        _pool.Prewarm(_attackFxPrefab, _attackFxPrewarmCount);
    }

    private void Update()
    {
      TryAttack();
    }

    public bool TryAttack()
    {
      if (_playerService == null || Time.time < _nextAttackTime)
        return false;

      _nextAttackTime = Time.time + _attackCooldown;
      SpawnAttackFx();
      Attack(_playerService.AttackPower);
      return true;
    }

    protected abstract void Attack(int damage);

    protected virtual void SpawnAttackFx()
    {
      if (_pool == null || _attackFxPrefab == null)
        return;

      var point = _attackFxPoint != null ? _attackFxPoint : transform;
      _pool.Get(_attackFxPrefab, point.position, point.rotation);
    }

    private void OnValidate()
    {
      _attackCooldown = Mathf.Max(0f, _attackCooldown);
      _attackFxPrewarmCount = Mathf.Max(0, _attackFxPrewarmCount);
    }
  }
}
