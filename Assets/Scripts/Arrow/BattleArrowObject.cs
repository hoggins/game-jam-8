using App;
using Destruction;
using Model;
using UnityEngine;
using VContainer;

namespace Arrow
{
  /// <summary>
  /// The in-world compass arrow: a destructible glyph lying flat on the ground that tells the player
  /// which way the current <see cref="Timer.BattleTimerObject"/> is.
  ///
  /// The glyph itself never turns — it is a solid object standing in the street, and spinning it
  /// would drag its colliders, its flow-map footprint and its debris around with it. What turns is
  /// the HUD camera hanging above it: rolling that camera about the vertical axis rotates the image
  /// it draws, so the arrow reads as a compass needle on the HUD while staying put in the world.
  ///
  /// One arrow exists per battle. It is moved rather than duplicated when the timer respawns, and
  /// smashing it costs the player navigation for the rest of the run — see
  /// <see cref="Timer.TimerRespawnService"/>, which reads <see cref="Current"/> to decide between
  /// the two.
  /// </summary>
  [DisallowMultipleComponent]
  public sealed class BattleArrowObject : MonoBehaviour
  {
    /// The one live arrow, or null when there is none — either not spawned yet, or smashed. Cleared
    /// the moment the arrow dies rather than when the husk finally retires, so a caller never gets
    /// handed an arrow that is already on its way out.
    public static BattleArrowObject Current { get; private set; }

    [Tooltip("The single destructible group that makes up the glyph. Found in children when unset.")]
    [SerializeField] private DestructibleObject _body;

    [Inject] private BattleService _battleService;

    private GameObject _hudCameraObject;
    private Transform _hudCamera;
    private Transform _player;
    private Timer.BattleTimerObject _timer;
    private bool _isDead;

    private void Awake()
    {
      this.AsInjected();
      WarnIfRootIsDestructible();

      if (_body == null)
        _body = GetComponentInChildren<DestructibleObject>(true);

      var camera = GetComponentInChildren<Camera>(true);
      _hudCameraObject = camera != null ? camera.gameObject : null;
      _hudCamera = camera != null ? camera.transform : null;

      Current = this;
    }

    private void OnEnable()
    {
      if (_body != null)
        _body.Destroyed += OnBodyDestroyed;

      if (_battleService != null)
        _battleService.TimerDestroyed += OnTimerDestroyed;
    }

    private void OnDisable()
    {
      if (_body != null)
        _body.Destroyed -= OnBodyDestroyed;

      if (_battleService != null)
        _battleService.TimerDestroyed -= OnTimerDestroyed;
    }

    private void OnDestroy()
    {
      if (Current == this)
        Current = null;
    }

    /// Aiming happens in LateUpdate so the camera is rolled after the player has finished turning
    /// for the frame; reading a stale facing shows up as the needle lagging a rotation by a frame.
    private void LateUpdate()
    {
      if (_isDead)
      {
        // Same husk problem the timer has: the decay manager retires the destructible group, but
        // nothing owns this root. Left alone it would linger holding a live camera and a render
        // texture, one more for every battle.
        if (!HasDestructibleChildren())
          Destroy(gameObject);

        return;
      }

      Aim();
    }

    /// <summary>
    /// Rolls the HUD camera so the glyph reads as a needle pointing at the timer.
    ///
    /// The camera looks straight down, so rolling it about the vertical axis rotates the image it
    /// draws: setting its yaw to Y makes a glyph pointing at world yaw Y read as pointing straight
    /// up, and every degree taken off that yaw swings the image one degree clockwise. So aiming at a
    /// bearing is just <c>glyphYaw - bearing</c>.
    ///
    /// The glyph's own yaw has to be measured rather than assumed: it points along the prefab root's
    /// +Z, and <see cref="Map.MapEnvironmentSpawner.TrySpawnSpecial"/> plants every special turned to
    /// face the player, so the signpost lies at whatever angle it was dropped at.
    ///
    /// The bearing is measured from the *player*, not from the arrow — the arrow is a signpost for
    /// where the player has to go, so it has to answer "which way do I turn", not "which way is the
    /// timer from this patch of road".
    /// </summary>
    private void Aim()
    {
      if (_hudCamera == null)
        return;

      if (_player == null)
      {
        var player = GameObject.FindGameObjectWithTag("Player");
        _player = player != null ? player.transform : null;
      }

      if (_timer == null && _battleService != null && !_battleService.IsTimerDestroyed)
        _timer = FindLiveTimer();

      if (_player == null || _timer == null)
        return;

      var toTimer = _timer.transform.position - _player.position;
      toTimer.y = 0f;

      var forward = _player.forward;
      forward.y = 0f;

      if (toTimer.sqrMagnitude < 0.0001f || forward.sqrMagnitude < 0.0001f)
        return;

      var bearing = Vector3.SignedAngle(forward, toTimer, Vector3.up);

      var facing = transform.forward;
      facing.y = 0f;
      var glyphYaw = facing.sqrMagnitude > 0.0001f
        ? Mathf.Atan2(facing.x, facing.z) * Mathf.Rad2Deg
        : 0f;

      // World rotation, not local: the roll is measured against the world, and reading it as an
      // offset from the root would apply the root's own yaw twice.
      _hudCamera.rotation = Quaternion.Euler(90f, glyphYaw - bearing, 0f);
    }

    /// <summary>
    /// The timer is replaced rather than moved, so the reference has to be dropped the moment the
    /// old one dies. Re-finding waits until <see cref="BattleService.IsTimerDestroyed"/> clears,
    /// which is the respawn service's signal that a fresh timer is standing.
    /// </summary>
    private void OnTimerDestroyed() => _timer = null;

    /// <summary>
    /// A dead timer's husk stays in the scene while its debris decays, so the first match is not
    /// necessarily the one the player is meant to walk to.
    /// </summary>
    private static Timer.BattleTimerObject FindLiveTimer()
    {
      var timers = FindObjectsByType<Timer.BattleTimerObject>(FindObjectsSortMode.None);
      foreach (var timer in timers)
        if (!timer.IsDead)
          return timer;

      return null;
    }

    private void OnBodyDestroyed(DestructibleObject destroyed)
    {
      if (_isDead)
        return;

      _isDead = true;

      // Clear the slot now rather than in OnDestroy: the husk survives for a few seconds, and a
      // timer respawning in that window must treat the arrow as gone, not move a corpse.
      if (Current == this)
        Current = null;

      if (_hudCameraObject != null)
        _hudCameraObject.SetActive(false);
    }

    private bool HasDestructibleChildren()
    {
      for (var i = 0; i < transform.childCount; i++)
        if (transform.GetChild(i).GetComponent<DestructibleObject>() != null)
          return true;

      return false;
    }

    private void OnValidate() => WarnIfRootIsDestructible();

    /// <summary>
    /// The glyph is one destructible group hanging off this root, and this root is what carries the
    /// HUD camera. A <see cref="DestructibleObject"/> here instead would break every part in a
    /// single hit without <see cref="OnBodyDestroyed"/> ever firing, so the camera would keep
    /// drawing a needle for an arrow that is already rubble. Tools > Destruction > Destructible
    /// Object Setup adds exactly that, so say so out loud instead of just misbehaving.
    /// </summary>
    private void WarnIfRootIsDestructible()
    {
      if (GetComponent<DestructibleObject>() == null && GetComponent<DestructibleHealth>() == null)
        return;

      Debug.LogError(
        $"{name}: the arrow root carries a DestructibleObject/DestructibleHealth, which breaks the "
        + "glyph in one hit without notifying this component, leaving a live HUD camera on rubble. "
        + "Remove them from the prefab root, or rebuild it with "
        + "Tools > Destruction > Rebuild Battle Arrow Prefab.",
        this);
    }
  }
}
