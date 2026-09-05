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
    /// Fixed hold on 00:00, blinking the timer's hit material, before the timeout defeat lands.
    private const float TimerExpiredBlinkDuration = 5f;

    private readonly IEnumerable<IBattleStarted> _battleStartedHandlers;
    private readonly IEnumerable<IBattleEnd> _battleEndHandlers;
    private readonly CharacterService _characterService;
    private readonly BattleBalanceConfig _battleBalance;

    private float _defeatDelay;
    private float _winGraceDelay;

    public event Action BattleStarted;
    public event Action BattleWinStarted;
    public event Action BattleWon;
    public event Action BattleDefeated;
    public event Action BattleAbandoned;
    public event Action TimerDestroyed;

    /// Fired once the clock hits 00:00 and the timeout defeat has been scheduled, carrying the delay
    /// in seconds until it lands. Lets the in-world timer blink a warning for that same window.
    public event Action<float> TimerExpired;

    public bool IsBattleActive { get; private set; }
    public bool IsTimerDestroyed { get; private set; }
    public bool IsTimerInfinite { get; private set; }

    /// True while the clock sits on 00:00, blinking, waiting for the timeout defeat. Combat keeps
    /// running during this window — it is bonus time, not a pause.
    public bool IsTimingOut { get; private set; }

    /// True while the goal has been destroyed and the battlefield is playing its victory
    /// animation. The win screen is shown after this window, not before it.
    public bool IsWinning { get; private set; }

    public float Timer { get; private set; }

    /// The base clock plus the seconds the character's Timer stat has bought.
    private float StartingDuration => _battleBalance.BattleDuration + _characterService.Timer;

    public BattleService(
      IEnumerable<IBattleStarted> battleStartedHandlers,
      IEnumerable<IBattleEnd> battleEndHandlers,
      CharacterService characterService,
      BattleBalanceConfig battleBalance)
    {
      _battleStartedHandlers = battleStartedHandlers;
      _battleEndHandlers = battleEndHandlers;
      _characterService = characterService;
      _battleBalance = battleBalance;
      Timer = _battleBalance.BattleDuration;
    }

    void IInitializable.Initialize()
    {
      _characterService.Died += DefeatBattle;
      _characterService.TimerBonusAdded += OnTimerBonusAdded;
    }

    /// A Timer upgrade bought mid-battle extends the clock that is already running; outside a
    /// battle the stat is picked up by the next StartBattle instead.
    private void OnTimerBonusAdded(int seconds)
    {
      if (!IsBattleActive || IsTimerDestroyed)
        return;

      SetTimer(Timer + seconds);
    }

    void ITickable.Tick()
    {
      if (IsWinning)
      {
        _winGraceDelay -= Time.deltaTime;
        if (_winGraceDelay <= 0f)
          CompleteWinBattle();

        return;
      }

      if (!IsBattleActive)
        return;

      if (IsTimerInfinite)
        return;

      if (IsTimingOut)
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
    /// very next frame and read as a bug. Hold on 00:00 for a beat first instead — a fixed grace
    /// window with combat still running, so a lucky last stand can still turn the battle around.
    /// </summary>
    private void BeginTimeout()
    {
      _defeatDelay = TimerExpiredBlinkDuration;
      IsTimingOut = true;
      TimerExpired?.Invoke(_defeatDelay);
    }

    void IDisposable.Dispose()
    {
      _characterService.Died -= DefeatBattle;
      _characterService.TimerBonusAdded -= OnTimerBonusAdded;
    }

    public void StartBattle()
    {
      if (IsBattleActive)
        return;

      IsBattleActive = true;
      IsTimerDestroyed = false;
      IsTimerInfinite = false;
      IsTimingOut = false;
      IsWinning = false;
      _defeatDelay = 0f;
      _winGraceDelay = 0f;
      Timer = StartingDuration;
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
      IsWinning = true;
      _winGraceDelay = _battleBalance.WinGraceDuration;
      Time.timeScale = 1f;
      _characterService.BeginVictoryProtection();
      BattleWinStarted?.Invoke();

      if (_winGraceDelay <= 0f)
        CompleteWinBattle();
    }

    private void CompleteWinBattle()
    {
      if (!IsWinning)
        return;

      IsWinning = false;
      _winGraceDelay = 0f;
      _characterService.EndVictoryProtection();
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

    /// Leaving a battle without winning or losing it — quitting to the main menu. Clears the
    /// state so the next StartBattle is not swallowed by the IsBattleActive guard, and fires no
    /// win/defeat event because neither happened.
    public void AbandonBattle()
    {
      if (!IsBattleActive && !IsWinning)
        return;

      IsBattleActive = false;
      IsTimerDestroyed = false;
      IsTimerInfinite = false;
      IsTimingOut = false;
      IsWinning = false;
      _defeatDelay = 0f;
      _winGraceDelay = 0f;
      _characterService.EndVictoryProtection();
      Timer = StartingDuration;
      Time.timeScale = 1f;
      foreach (IBattleEnd handler in _battleEndHandlers)
        handler.OnBattleEnd();
      BattleAbandoned?.Invoke();
    }

    public void DestroyTimer()
    {
      // Smashing the last digit while the clock is blinking out its timeout is how the player earns
      // extra time: cancel the pending defeat, a fresh timer is on its way in via TimerRespawnService.
      IsTimingOut = false;
      _defeatDelay = 0f;

      IsTimerDestroyed = true;
      TimerDestroyed?.Invoke();
    }

    /// Brings the timer back after <see cref="TimerDestroyed"/>, once a new one has been placed
    /// in the world. The countdown resumes from the computed budget for the next route hop.
    public void RespawnTimer(float seconds)
    {
      IsTimerDestroyed = false;
      Timer = Mathf.Max(0f, seconds);
    }

    public void EnableInfiniteTimer()
    {
      IsTimerInfinite = true;
      IsTimingOut = false;
      _defeatDelay = 0f;

      if (Timer <= 0f)
        Timer = StartingDuration;
    }

    /// Overwrites the remaining time. Used when part of the in-world timer is destroyed and the
    /// countdown has to continue from whatever the surviving digits still spell out.
    public void SetTimer(float seconds)
    {
      Timer = Mathf.Max(0f, seconds);

      // Time found its way back above zero while the timeout blink was already running (e.g. a
      // Timer stat bonus lands mid-blink) — call off the pending defeat instead of still landing it.
      if (IsTimingOut && Timer > 0f)
      {
        IsTimingOut = false;
        _defeatDelay = 0f;
      }
    }

    private void HandleBattleEnd()
    {
      IsTimingOut = false;
      _defeatDelay = 0f;
      Time.timeScale = 0f;
      foreach (IBattleEnd handler in _battleEndHandlers)
        handler.OnBattleEnd();
    }
  }
}
