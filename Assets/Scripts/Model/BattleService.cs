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

    private float _defeatDelay;

    public event Action BattleStarted;
    public event Action BattleWon;
    public event Action BattleDefeated;

    public bool IsBattleActive { get; private set; }
    public bool IsTimerDestroyed { get; private set; }

    /// True while the clock sits on 00:00 waiting for the timeout defeat. Nothing deals damage in
    /// that window, so the player can read the zero instead of being hit during it.
    public bool IsCombatSuspended { get; private set; }

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
      if (!IsBattleActive)
        return;

      if (IsCombatSuspended)
      {
        _defeatDelay -= Time.deltaTime;
        if (_defeatDelay <= 0f)
          DefeatBattle();

        return;
      }

      if (IsTimerDestroyed)
        return;

      Timer = Mathf.Max(0f, Timer - Time.deltaTime);
      if (Timer <= 0f)
        BeginTimeout();
    }

    /// <summary>
    /// Smashing a digit can drop the clock straight to zero, which used to defeat the player on the
    /// very next frame and read as a bug. Hold on 00:00 for a beat first, with damage suspended in
    /// both directions so nothing can resolve during the pause.
    /// </summary>
    private void BeginTimeout()
    {
      _defeatDelay = BattleBalance.TimerExpiredDefeatDelay;
      if (_defeatDelay <= 0f)
      {
        DefeatBattle();
        return;
      }

      IsCombatSuspended = true;
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
      IsCombatSuspended = false;
      _defeatDelay = 0f;
      Timer = BattleBalance.BattleDuration;
      Time.timeScale = 1f;
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

    /// Overwrites the remaining time. Used when part of the in-world timer is destroyed and the
    /// countdown has to continue from whatever the surviving digits still spell out.
    public void SetTimer(float seconds) =>
      Timer = Mathf.Max(0f, seconds);

    private void HandleBattleEnd()
    {
      IsCombatSuspended = false;
      _defeatDelay = 0f;
      Time.timeScale = 0f;
      foreach (IBattleEnd handler in _battleEndHandlers)
        handler.OnBattleEnd();
    }
  }
}
