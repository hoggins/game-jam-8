using UnityEngine;
using UnityEngine.Scripting;
using VContainer.Unity;

namespace Sfx
{
  /// Owns the single AudioSource used for UI feedback. Registered in AppScope, so it survives
  /// scene loads together with the source it creates.
  [Preserve]
  public class UiSfxService : IInitializable
  {
    private const string ClickClipResourcePath = "SFX/quackSfx";

    private AudioClip _clickClip;
    private AudioSource _source;

    void IInitializable.Initialize()
    {
      _clickClip = Resources.Load<AudioClip>(ClickClipResourcePath);
      if (_clickClip == null)
      {
        Debug.LogError($"UI click clip was not found at Resources/{ClickClipResourcePath}.");
        return;
      }

      var host = new GameObject(nameof(UiSfxService));
      Object.DontDestroyOnLoad(host);

      _source = host.AddComponent<AudioSource>();
      _source.playOnAwake = false;
      _source.spatialBlend = 0f;
      // spatialBlend alone does not disable Doppler: the listener rides the battle camera, and its
      // velocity relative to this source would pitch-shift every click.
      _source.dopplerLevel = 0f;
      // The pause menu sets Time.timeScale to 0 - the click still has to be audible there.
      _source.ignoreListenerPause = true;
    }

    public void PlayClick()
    {
      if (_source == null || _clickClip == null)
        return;

      _source.PlayOneShot(_clickClip);
    }
  }
}
