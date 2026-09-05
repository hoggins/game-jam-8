using System;
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
  public class UiSfxService : IInitializable, IDisposable
  {
    private const string QuackClipsResourceFolder = "SFX";

    /// A wave of ducks can die within the same frame - without this floor they stack into one
    /// loud smear instead of reading as separate quacks.
    private const float DuckKillQuackCooldown = 0.25f;

    private readonly CharacterService _characterService;

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

    /// Unscaled: the cooldown has to keep running while a slow-motion or paused frame is up.
    private void OnDuckKilled()
    {
      if (Time.unscaledTime - _lastDuckKillQuackTime < DuckKillQuackCooldown)
        return;

      _lastDuckKillQuackTime = Time.unscaledTime;
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
