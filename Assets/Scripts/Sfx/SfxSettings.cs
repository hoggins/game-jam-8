using UnityEngine;

namespace Sfx
{
  /// Global sound tuning, mirroring how HitFxSettings holds the hit-flash tuning: one asset in
  /// Resources, edited from the Game Balance window. One group per gameplay beat - every group
  /// folds the master <see cref="Volume"/> in, so callers pass the result straight to PlayOneShot.
  [CreateAssetMenu(fileName = "SfxSettings", menuName = "Sfx/Sfx Settings")]
  public sealed class SfxSettings : ScriptableObject
  {
    [Tooltip("Master multiplier applied to every sound effect.")]
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    [Header("Duck Killed")]
    [Tooltip("One of these is picked per kill, never the same one twice in a row.")]
    [SerializeField] private AudioClip[] duckKilledClips;
    [Tooltip("Each kill picks a volume in this range, so a burst of them does not sound flat.")]
    [SerializeField, Range(0f, 1f)] private float duckKilledVolumeMin = 0.6f;
    [SerializeField, Range(0f, 1f)] private float duckKilledVolumeMax = 1f;
    [Tooltip("Kills that cannot play immediately are spread over up to this many seconds.")]
    [SerializeField, Min(0f)] private float duckKilledSpread = 0.5f;
    [Tooltip("Minimum seconds between two duck-kill sounds.")]
    [SerializeField, Min(0f)] private float duckKilledCooldown = 0.15f;
    [Tooltip("Kills queued beyond this are dropped, so a big wave does not trail for seconds.")]
    [SerializeField, Min(0)] private int duckKilledMaxQueued = 8;

    [Header("Building Destroyed")]
    [Tooltip("One of these is picked per building, never the same one twice in a row.")]
    [SerializeField] private AudioClip[] buildingDestroyedClips;
    [Tooltip("Each building picks a volume in this range, so a run of them does not sound flat.")]
    [SerializeField, Range(0f, 1f)] private float buildingDestroyedVolumeMin = 0.6f;
    [SerializeField, Range(0f, 1f)] private float buildingDestroyedVolumeMax = 1f;

    [Header("Button Click")]
    [Tooltip("One of these is picked per click, never the same one twice in a row.")]
    [SerializeField] private AudioClip[] buttonClickClips;
    [SerializeField, Range(0f, 1f)] private float buttonClickVolumeMin = 0.6f;
    [SerializeField, Range(0f, 1f)] private float buttonClickVolumeMax = 1f;

    [Header("Player Damaged")]
    [Tooltip("One of these is picked per hit, never the same one twice in a row.")]
    [SerializeField] private AudioClip[] playerDamagedClips;
    [SerializeField, Range(0f, 1f)] private float playerDamagedVolumeMin = 0.6f;
    [SerializeField, Range(0f, 1f)] private float playerDamagedVolumeMax = 1f;
    [Tooltip("Hits that cannot play immediately are spread over up to this many seconds.")]
    [SerializeField, Min(0f)] private float playerDamagedSpread = 0.2f;
    [Tooltip("Minimum seconds between two player-damaged sounds.")]
    [SerializeField, Min(0f)] private float playerDamagedCooldown = 0.35f;
    [Tooltip("Hits queued beyond this are dropped.")]
    [SerializeField, Min(0)] private int playerDamagedMaxQueued = 2;

    [Header("Battle Win")]
    [SerializeField] private AudioClip battleWinClip;
    [SerializeField, Range(0f, 1f)] private float battleWinVolume = 1f;

    [Header("Battle Music")]
    [Tooltip("Plays for the length of a battle, from the first frame to win, defeat or abandon. " +
             "Battles cycle through these in order, starting over when the app restarts.")]
    [SerializeField] private AudioClip[] battleMusicClips;
    [SerializeField, Range(0f, 1f)] private float battleMusicVolume = 1f;
    [SerializeField] private bool battleMusicLoop = true;
    [Tooltip("Seconds the track takes to fade out under the win or defeat sting. 0 cuts it dead.")]
    [SerializeField, Min(0f)] private float battleMusicFadeOut = 1f;

    [Header("Battle Defeat")]
    [SerializeField] private AudioClip battleDefeatClip;
    [SerializeField, Range(0f, 1f)] private float battleDefeatVolume = 1f;

    public float Volume => volume;

    public AudioClip[] DuckKilledClips => duckKilledClips;
    public AudioClip[] BuildingDestroyedClips => buildingDestroyedClips;
    public AudioClip[] ButtonClickClips => buttonClickClips;
    public AudioClip[] PlayerDamagedClips => playerDamagedClips;
    public AudioClip BattleWinClip => battleWinClip;
    public AudioClip BattleDefeatClip => battleDefeatClip;
    public AudioClip[] BattleMusicClips => battleMusicClips;
    public bool BattleMusicLoop => battleMusicLoop;
    public float BattleMusicFadeOut => battleMusicFadeOut;

    public float DuckKilledSpread => duckKilledSpread;
    public float DuckKilledCooldown => duckKilledCooldown;
    public int DuckKilledMaxQueued => duckKilledMaxQueued;

    public float PlayerDamagedSpread => playerDamagedSpread;
    public float PlayerDamagedCooldown => playerDamagedCooldown;
    public int PlayerDamagedMaxQueued => playerDamagedMaxQueued;

    public float RollDuckKilledVolume() => Random.Range(duckKilledVolumeMin, duckKilledVolumeMax) * volume;
    public float RollBuildingDestroyedVolume() => Random.Range(buildingDestroyedVolumeMin, buildingDestroyedVolumeMax) * volume;
    public float RollButtonClickVolume() => Random.Range(buttonClickVolumeMin, buttonClickVolumeMax) * volume;
    public float RollPlayerDamagedVolume() => Random.Range(playerDamagedVolumeMin, playerDamagedVolumeMax) * volume;

    public float BattleWinVolume => battleWinVolume * volume;
    public float BattleDefeatVolume => battleDefeatVolume * volume;
    public float BattleMusicVolume => battleMusicVolume * volume;

    private void OnValidate()
    {
      duckKilledVolumeMax = Mathf.Max(duckKilledVolumeMin, duckKilledVolumeMax);
      buildingDestroyedVolumeMax = Mathf.Max(buildingDestroyedVolumeMin, buildingDestroyedVolumeMax);
      buttonClickVolumeMax = Mathf.Max(buttonClickVolumeMin, buttonClickVolumeMax);
      playerDamagedVolumeMax = Mathf.Max(playerDamagedVolumeMin, playerDamagedVolumeMax);
    }
  }
}
