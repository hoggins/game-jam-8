using System.Collections;
using System;
using App;
using Movement;
using Pooling;
using UnityEngine;
using VContainer;

namespace Combat
{
  [DisallowMultipleComponent]
  public sealed class Mob : MonoBehaviour, IDamageable
  {
    [SerializeField, Min(1)] private int _maxHealth = 1;
    [SerializeField, Min(0f)] private float _deathDelay = 0.5f;

    [Inject] private Pool _pool;

    private MovementAgent _movementAgent;
    private Coroutine _deathCoroutine;
    private bool _isDying;

    public int CurrentHealth { get; private set; }
    public bool IsAlive => CurrentHealth > 0 && !_isDying;

    private void Awake()
    {
      this.AsInjected();
      _movementAgent = GetComponent<MovementAgent>();
      CurrentHealth = _maxHealth;
    }

    private void Start() =>
      _pool?.Register(gameObject);

    private void OnEnable()
    {
      CurrentHealth = _maxHealth;
      _isDying = false;

      if (_movementAgent != null)
        _movementAgent.enabled = true;
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

      if (_movementAgent != null)
        _movementAgent.enabled = false;

      _deathCoroutine = StartCoroutine(ReturnToPoolAfterDelay());
    }

    private IEnumerator ReturnToPoolAfterDelay()
    {
      yield return new WaitForSeconds(_deathDelay);

      _deathCoroutine = null;
      _pool?.Release(gameObject);
    }

    private void OnDisable()
    {
      if (_deathCoroutine == null)
        return;

      StopCoroutine(_deathCoroutine);
      _deathCoroutine = null;
    }

    private void OnValidate()
    {
      _maxHealth = Mathf.Max(1, _maxHealth);
      _deathDelay = Mathf.Max(0f, _deathDelay);
    }
  }
}
