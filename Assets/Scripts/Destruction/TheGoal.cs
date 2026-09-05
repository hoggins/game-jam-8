using System;
using App;
using Model;
using UnityEngine;
using VContainer;

namespace Destruction
{
  /// <summary>
  /// Scene-authored final objective. The goal deliberately listens to the shared destructible
  /// object instead of teaching every destructible house about win conditions.
  /// </summary>
  [DisallowMultipleComponent]
  [RequireComponent(typeof(DestructibleObject), typeof(DestructibleHealth))]
  public sealed class TheGoal : MonoBehaviour
  {
    public static TheGoal Current { get; private set; }

    [Tooltip("The destructible body that completes the battle when it breaks.")]
    [SerializeField] private DestructibleObject _body;

    [Inject] private BattleService _battleService;

    public bool IsDestroyed { get; private set; }

    public event Action<TheGoal> Destroyed;

    private void Awake()
    {
      this.AsInjected();

      if (_body == null)
        _body = GetComponent<DestructibleObject>();

      if (Current != null && Current != this)
        Debug.LogError($"Multiple {nameof(TheGoal)} objects are active. Only one final goal is supported.", this);

      Current = this;
    }

    private void OnEnable()
    {
      if (_body != null)
        _body.Destroyed += OnBodyDestroyed;
    }

    private void OnDisable()
    {
      if (_body != null)
        _body.Destroyed -= OnBodyDestroyed;
    }

    private void OnDestroy()
    {
      if (Current == this)
        Current = null;
    }

    private void OnBodyDestroyed(DestructibleObject destroyed)
    {
      if (IsDestroyed)
        return;

      IsDestroyed = true;
      if (Current == this)
        Current = null;

      Destroyed?.Invoke(this);
      _battleService?.WinBattle();
    }
  }
}
