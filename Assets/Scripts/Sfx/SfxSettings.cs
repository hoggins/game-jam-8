using UnityEngine;

namespace Sfx
{
  /// Global sound tuning, mirroring how HitFxSettings holds the hit-flash tuning: one asset in
  /// Resources, edited from the Game Balance window.
  [CreateAssetMenu(fileName = "SfxSettings", menuName = "Sfx/Sfx Settings")]
  public sealed class SfxSettings : ScriptableObject
  {
    [Tooltip("Master multiplier applied to every sound effect.")]
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    [Tooltip("Each quack picks a volume in this range, so a burst of them does not sound flat.")]
    [SerializeField, Range(0f, 1f)] private float quackVolumeMin = 0.6f;
    [SerializeField, Range(0f, 1f)] private float quackVolumeMax = 1f;

    public float Volume => volume;
    public float QuackVolumeMin => quackVolumeMin;
    public float QuackVolumeMax => quackVolumeMax;

    /// The master volume folded in, so callers pass the result straight to PlayOneShot.
    public float RollQuackVolume() =>
      Random.Range(quackVolumeMin, quackVolumeMax) * volume;

    private void OnValidate()
    {
      quackVolumeMax = Mathf.Max(quackVolumeMin, quackVolumeMax);
    }
  }
}
