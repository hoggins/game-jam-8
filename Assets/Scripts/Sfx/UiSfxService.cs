using System;
using System.Collections.Generic;
using Model;
using UnityEngine;
using UnityEngine.Scripting;
using VContainer.Unity;

namespace Sfx
{
  /// Owns the single AudioSource used for the quack feedback sound - UI clicks, plus the
  /// gameplay beats that share the same clip. Registered in AppScope, so it survives scene
  /// loads together with the source it creates.
  [Preserve]
  public class UiSfxService : IInitializable, ITickable, IDisposable
  {
    private const string QuackClipsResourceFolder = "SFX";

    /// Ducks die in waves, often several within the same frame. Playing them all at once smears
    /// into one loud noise, and dropping the extras loses the wave entirely - so each kill is
    /// scheduled at a random offset up to this, and the queue drains no faster than the cooldown.
    private const float MaxDuckKillQuackDelay = 0.5f;
    private const float DuckKillQuackCooldown = 0.15f;

    /// A big enough wave would otherwise trail quacks for seconds after the fight moved on.
    private const int MaxPendingDuckKillQuacks = 8;

    private readonly CharacterService _characterService;
    private readonly List<float> _pendingDuckKillQuacks = new();

    private AudioClip[] _quackClips;
    private AudioSource _source;
    private int _lastQuackIndex = -1;
    private float _lastDuckKillQuackTime = float.NegativeInfinity;

    public UiSfxService(CharacterService characterService)
    {
      _characterService = characterService;
    }

    void IInitializable.Initialize()
    {
      // Every clip in the folder is a variant - dropping another wav in there is all it takes.
      _quackClips = Resources.LoadAll<AudioClip>(QuackClipsResourceFolder);
      if (_quackClips.Length == 0)
      {
        Debug.LogError($"No quack clips were found in Resources/{QuackClipsResourceFolder}.");
        return;
      }

      var host = new GameObject(nameof(UiSfxService));
      UnityEngine.Object.DontDestroyOnLoad(host);

      _source = host.AddComponent<AudioSource>();
      _source.playOnAwake = false;
      _source.spatialBlend = 0f;
      // spatialBlend alone does not disable Doppler: the listener rides the battle camera, and its
      // velocity relative to this source would pitch-shift every click.
      _source.dopplerLevel = 0f;
      // The pause menu sets Time.timeScale to 0 - the click still has to be audible there.
      _source.ignoreListenerPause = true;

      _characterService.DuckKilled += OnDuckKilled;
    }

    void IDisposable.Dispose()
    {
      _characterService.DuckKilled -= OnDuckKilled;
    }

    /// Unscaled throughout: the spread and the cooldown have to keep running while a
    /// slow-motion or paused frame is up.
    private void OnDuckKilled()
    {
      var now = Time.unscaledTime;

      // The kill that opens a wave is the one the player is watching - delaying it reads as lag.
      // Only once something is already queued or still inside the cooldown does the spread apply.
      if (_pendingDuckKillQuacks.Count == 0 && now - _lastDuckKillQuackTime >= DuckKillQuackCooldown)
      {
        _lastDuckKillQuackTime = now;
        PlayQuack();
        return;
      }

      if (_pendingDuckKillQuacks.Count >= MaxPendingDuckKillQuacks)
        return;

      _pendingDuckKillQuacks.Add(now + UnityEngine.Random.Range(0f, MaxDuckKillQuackDelay));
    }

    /// Plays at most one queued quack per frame, and only once the cooldown since the last one
    /// has elapsed - a quack held back by the cooldown stays queued rather than being dropped.
    void ITickable.Tick()
    {
      if (_pendingDuckKillQuacks.Count == 0)
        return;

      var now = Time.unscaledTime;
      if (now - _lastDuckKillQuackTime < DuckKillQuackCooldown)
        return;

      var dueIndex = -1;
      for (var i = 0; i < _pendingDuckKillQuacks.Count; i++)
      {
        if (_pendingDuckKillQuacks[i] > now)
          continue;

        if (dueIndex < 0 || _pendingDuckKillQuacks[i] < _pendingDuckKillQuacks[dueIndex])
          dueIndex = i;
      }

      if (dueIndex < 0)
        return;

      _pendingDuckKillQuacks.RemoveAt(dueIndex);
      _lastDuckKillQuackTime = now;
      PlayQuack();
    }

    public void PlayQuack()
    {
      if (_source == null || _quackClips == null || _quackClips.Length == 0)
        return;

      _source.PlayOneShot(_quackClips[NextQuackIndex()]);
    }

    /// Random, but never the same variant twice in a row - a repeat reads as a stutter rather
    /// than as a second quack.
    private int NextQuackIndex()
    {
      if (_quackClips.Length == 1)
        return 0;

      var index = UnityEngine.Random.Range(0, _quackClips.Length - 1);
      if (index >= _lastQuackIndex)
        index++;

      _lastQuackIndex = index;
      return index;
    }
  }
}
