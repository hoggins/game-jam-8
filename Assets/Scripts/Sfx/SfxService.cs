using System;
using System.Collections.Generic;
using Model;
using UnityEngine;
using UnityEngine.Scripting;
using VContainer.Unity;
using Random = UnityEngine.Random;

namespace Sfx
{
  /// Owns the single AudioSource every sound effect goes through, and the clip list of each
  /// group. Registered in AppScope, so it survives scene loads together with the source it
  /// creates. Groups are tuned independently from <see cref="SfxSettings"/>.
  [Preserve]
  public class SfxService : IInitializable, ITickable, IDisposable
  {
    private const string SettingsResourcePath = "SfxSettings";

    private readonly CharacterService _characterService;
    private readonly BattleService _battleService;

    /// Ducks die in waves, often several within the same frame, and the player takes several hits
    /// in a row. Playing them all at once smears into one loud noise, and dropping the extras
    /// loses the beat entirely - so both groups queue through a throttle. The remaining groups
    /// fire at most once per event and need none.
    private readonly SfxThrottle _duckKilledThrottle = new();
    private readonly SfxThrottle _playerDamagedThrottle = new();

    private SfxSettings _settings;
    private AudioSource _source;

    private int _lastDuckKilledIndex = -1;
    private int _lastBuildingDestroyedIndex = -1;
    private int _lastButtonClickIndex = -1;
    private int _lastPlayerDamagedIndex = -1;

    public SfxService(CharacterService characterService, BattleService battleService)
    {
      _characterService = characterService;
      _battleService = battleService;
    }

    void IInitializable.Initialize()
    {
      _settings = Resources.Load<SfxSettings>(SettingsResourcePath);
      if (_settings == null)
      {
        Debug.LogError($"Sfx settings were not found at Resources/{SettingsResourcePath}. Falling back to defaults.");
        _settings = ScriptableObject.CreateInstance<SfxSettings>();
      }

      WarnOnEmptyGroups();

      var host = new GameObject(nameof(SfxService));
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
      _characterService.BuildingDestroyed += OnBuildingDestroyed;
      _characterService.Damaged += OnPlayerDamaged;
      _battleService.BattleWon += OnBattleWon;
      _battleService.BattleDefeated += OnBattleDefeated;
    }

    void IDisposable.Dispose()
    {
      _characterService.DuckKilled -= OnDuckKilled;
      _characterService.BuildingDestroyed -= OnBuildingDestroyed;
      _characterService.Damaged -= OnPlayerDamaged;
      _battleService.BattleWon -= OnBattleWon;
      _battleService.BattleDefeated -= OnBattleDefeated;
    }

    /// Called from UiClickSfx on every button this sits on.
    public void PlayButtonClick() =>
      Play(PickClip(_settings.ButtonClickClips, ref _lastButtonClickIndex), _settings.RollButtonClickVolume());

    private void OnDuckKilled()
    {
      if (_duckKilledThrottle.Request(_settings.DuckKilledSpread, _settings.DuckKilledCooldown, _settings.DuckKilledMaxQueued))
        PlayDuckKilled();
    }

    /// Unthrottled: a building takes a sustained beating to bring down, so these arrive spaced
    /// out on their own rather than in the same-frame bursts the duck and damage groups see.
    private void OnBuildingDestroyed() =>
      Play(PickClip(_settings.BuildingDestroyedClips, ref _lastBuildingDestroyedIndex), _settings.RollBuildingDestroyedVolume());

    private void OnPlayerDamaged()
    {
      if (_playerDamagedThrottle.Request(_settings.PlayerDamagedSpread, _settings.PlayerDamagedCooldown, _settings.PlayerDamagedMaxQueued))
        PlayPlayerDamaged();
    }

    /// The stings own the moment: whatever kill and hit sounds were still queued belong to a
    /// fight that just ended, and letting them trail would play them over the popup.
    private void OnBattleWon()
    {
      ClearThrottleQueues();
      Play(_settings.BattleWinClip, _settings.BattleWinVolume);
    }

    private void OnBattleDefeated()
    {
      ClearThrottleQueues();
      Play(_settings.BattleDefeatClip, _settings.BattleDefeatVolume);
    }

    private void ClearThrottleQueues()
    {
      _duckKilledThrottle.Clear();
      _playerDamagedThrottle.Clear();
    }

    void ITickable.Tick()
    {
      if (_settings == null)
        return;

      if (_duckKilledThrottle.TryDequeue(_settings.DuckKilledCooldown))
        PlayDuckKilled();

      if (_playerDamagedThrottle.TryDequeue(_settings.PlayerDamagedCooldown))
        PlayPlayerDamaged();
    }

    private void PlayDuckKilled() =>
      Play(PickClip(_settings.DuckKilledClips, ref _lastDuckKilledIndex), _settings.RollDuckKilledVolume());

    private void PlayPlayerDamaged() =>
      Play(PickClip(_settings.PlayerDamagedClips, ref _lastPlayerDamagedIndex), _settings.RollPlayerDamagedVolume());

    private void Play(AudioClip clip, float volume)
    {
      if (_source == null || clip == null)
        return;

      _source.PlayOneShot(clip, volume);
    }

    /// Random, but never the same variant twice in a row - a repeat reads as a stutter rather
    /// than as a second sound.
    private static AudioClip PickClip(AudioClip[] clips, ref int lastIndex)
    {
      if (clips == null || clips.Length == 0)
        return null;

      if (clips.Length == 1)
        return clips[0];

      // The first pick has nothing to avoid, so it draws from the whole set; every later pick
      // draws from the set minus the previous clip and shifts past it.
      var index = lastIndex < 0
        ? Random.Range(0, clips.Length)
        : Random.Range(0, clips.Length - 1);
      if (lastIndex >= 0 && index >= lastIndex)
        index++;

      lastIndex = index;
      return clips[index];
    }

    /// An unassigned group is silent with no other symptom, so it is worth one line at startup.
    private void WarnOnEmptyGroups()
    {
      WarnIfEmpty(_settings.DuckKilledClips, nameof(SfxSettings.DuckKilledClips));
      WarnIfEmpty(_settings.BuildingDestroyedClips, nameof(SfxSettings.BuildingDestroyedClips));
      WarnIfEmpty(_settings.ButtonClickClips, nameof(SfxSettings.ButtonClickClips));
      WarnIfEmpty(_settings.PlayerDamagedClips, nameof(SfxSettings.PlayerDamagedClips));

      if (_settings.BattleWinClip == null)
        Debug.LogError($"{nameof(SfxSettings)}.{nameof(SfxSettings.BattleWinClip)} is not assigned.");

      if (_settings.BattleDefeatClip == null)
        Debug.LogError($"{nameof(SfxSettings)}.{nameof(SfxSettings.BattleDefeatClip)} is not assigned.");
    }

    private static void WarnIfEmpty(AudioClip[] clips, string groupName)
    {
      if (clips == null || clips.Length == 0)
        Debug.LogError($"{nameof(SfxSettings)}.{groupName} has no clips assigned.");
    }

    /// Spreads a burst of same-group events over time instead of playing them on one frame, and
    /// keeps at least the cooldown between two of them. An event held back by the cooldown stays
    /// queued rather than being dropped; only a queue past its cap drops.
    /// Unscaled throughout: the spread and the cooldown have to keep running while a slow-motion
    /// or paused frame is up.
    private sealed class SfxThrottle
    {
      private readonly List<float> _pending = new();
      private float _lastPlayTime = float.NegativeInfinity;

      /// True when the caller should play the sound right now; otherwise it has been queued for
      /// a later Tick, or dropped.
      public bool Request(float spread, float cooldown, int maxQueued)
      {
        var now = Time.unscaledTime;

        // The event that opens a burst is the one the player is watching - delaying it reads as
        // lag. Only once something is queued or still inside the cooldown does the spread apply.
        if (_pending.Count == 0 && now - _lastPlayTime >= cooldown)
        {
          _lastPlayTime = now;
          return true;
        }

        if (_pending.Count >= maxQueued)
          return false;

        _pending.Add(now + Random.Range(0f, spread));
        return false;
      }

      public void Clear() =>
        _pending.Clear();

      /// Releases at most one queued sound per frame, and only once the cooldown since the last
      /// one has elapsed.
      public bool TryDequeue(float cooldown)
      {
        if (_pending.Count == 0)
          return false;

        var now = Time.unscaledTime;
        if (now - _lastPlayTime < cooldown)
          return false;

        var dueIndex = -1;
        for (var i = 0; i < _pending.Count; i++)
        {
          if (_pending[i] > now)
            continue;

          if (dueIndex < 0 || _pending[i] < _pending[dueIndex])
            dueIndex = i;
        }

        if (dueIndex < 0)
          return false;

        _pending.RemoveAt(dueIndex);
        _lastPlayTime = now;
        return true;
      }
    }
  }
}
