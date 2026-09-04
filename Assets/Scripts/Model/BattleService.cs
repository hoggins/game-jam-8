using System;
using System.Collections.Generic;
using Balance;
using UnityEngine;
using UnityEngine.Scripting;
using VContainer.Unity;

namespace Model
{
  [Preserve]
  public sealed class BattleService : IInitializable, ITickable, IDisposable
  {
    private readonly IEnumerable<IBattleStarted> _battleStartedHandlers;
    private readonly IEnumerable<IBattleEnd> _battleEndHandlers;
    private readonly CharacterService _characterService;

    public event Action BattleStarted;
    public event Action BattleWon;
    public event Action BattleDefeated;

    public bool IsBattleActive { get; private set; }
    public bool IsTimerDestroyed { get; private set; }
    public float Timer { get; private set; } = BattleBalance.BattleDuration;

    public BattleService(
      IEnumerable<IBattleStarted> battleStartedHandlers,
      IEnumerable<IBattleEnd> battleEndHandlers,
      CharacterService characterService)
    {
      _battleStartedHandlers = battleStartedHandlers;
      _battleEndHandlers = battleEndHandlers;
      _characterService = characterService;
    }

    void IInitializable.Initialize() =>
      _characterService.Died += DefeatBattle;

    void ITickable.Tick()
    {
      if (!IsBattleActive || IsTimerDestroyed)
        return;

      Timer = Mathf.Max(0f, Timer - Time.deltaTime);
      if (Timer <= 0f)
        DefeatBattle();
    }

    void IDisposable.Dispose()
    {
      _characterService.Died -= DefeatBattle;
    }

    public void StartBattle()
    {
      if (IsBattleActive)
        return;

      IsBattleActive = true;
      IsTimerDestroyed = false;
      Timer = BattleBalance.BattleDuration;
      foreach (var handler in _battleStartedHandlers)
        handler.OnBattleStarted();

      BattleStarted?.Invoke();
    }

    public void WinBattle()
    {
      if (!IsBattleActive)
        return;

      IsBattleActive = false;
      HandleBattleEnd();
      BattleWon?.Invoke();
    }

    private void DefeatBattle()
    {
      if (!IsBattleActive)
        return;

      IsBattleActive = false;
      HandleBattleEnd();
      BattleDefeated?.Invoke();
    }

    public void DestroyTimer()
    {
      IsTimerDestroyed = true;
    }

    private void HandleBattleEnd()
    {
      foreach (IBattleEnd handler in _battleEndHandlers)
        handler.OnBattleEnd();
    }
  }
}
