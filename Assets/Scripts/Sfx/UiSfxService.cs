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
    private const string QuackClipResourcePath = "SFX/quackSfx";

    private readonly CharacterService _characterService;

    private AudioClip _quackClip;
    private AudioSource _source;

    public UiSfxService(CharacterService characterService)
    {
      _characterService = characterService;
    }

    void IInitializable.Initialize()
    {
      _quackClip = Resources.Load<AudioClip>(QuackClipResourcePath);
      if (_quackClip == null)
      {
        Debug.LogError($"Quack clip was not found at Resources/{QuackClipResourcePath}.");
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

      _characterService.DuckKilled += PlayQuack;
    }

    void IDisposable.Dispose()
    {
      _characterService.DuckKilled -= PlayQuack;
    }

    public void PlayQuack()
    {
      if (_source == null || _quackClip == null)
        return;

      _source.PlayOneShot(_quackClip);
    }
  }
}
