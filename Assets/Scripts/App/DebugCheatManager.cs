using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using VContainer;
using Battle;
using Destruction;
using Model;
using Movement;
using ScenesManagement;
using Timer;

namespace App
{
  [DisallowMultipleComponent]
  public sealed class DebugCheatManager : MonoBehaviour
  {
    [Header("Player Teleport")]
    [SerializeField] private Vector3 _teleportPosition;

    [Header("Player Money")]
    [SerializeField, Min(1)] private int _coinsToAdd = 100;

    [Inject] private CharacterService _characterService;
    [Inject] private BattleService _battleService;
    [Inject] private SceneService _sceneService;

    private void Awake() => this.AsInjected();

    private void Update()
    {
      var keyboard = Keyboard.current;
      if (keyboard == null)
        return;

      if (keyboard.equalsKey.wasPressedThisFrame || keyboard.numpadPlusKey.wasPressedThisFrame)
        Time.timeScale *= 2f;

      if (keyboard.minusKey.wasPressedThisFrame || keyboard.numpadMinusKey.wasPressedThisFrame)
        Time.timeScale *= 0.5f;

      if (keyboard.f2Key.wasPressedThisFrame)
        ToggleCheatsInfo();

      if (keyboard.f8Key.wasPressedThisFrame)
        TeleportPlayer(_teleportPosition);

      if (keyboard.f9Key.wasPressedThisFrame)
        TeleportPlayer(Vector3.zero);

      if (keyboard.f3Key.wasPressedThisFrame)
        ResetProgress();

      if (keyboard.f10Key.wasPressedThisFrame)
        EnablePlayerInvincibility();

      if (keyboard.f5Key.wasPressedThisFrame)
        TeleportPlayerToGoal();

      if (keyboard.f6Key.wasPressedThisFrame)
        AddPlayerMoney();

      if (keyboard.f11Key.wasPressedThisFrame)
        EnableInfiniteTimer();

      if (keyboard.f4Key.wasPressedThisFrame)
        DestroyTimer();
    }

    /// <summary>Adds the configured amount of coins to the player's saved balance. Press F6 during a battle.</summary>
    public bool AddPlayerMoney()
    {
      if (_characterService == null || _coinsToAdd <= 0)
        return false;

      _characterService.AddCoins(_coinsToAdd);
      Debug.Log($"Added {_coinsToAdd} coins to the player.", this);
      return true;
    }

    /// <summary>Resets all saved player progression. Press F3 during a battle.</summary>
    public bool ResetProgress()
    {
      if (_characterService == null)
        return false;

      _characterService.ResetProgression();
      _sceneService?.LoadMainMenuScene();
      Debug.Log("All player progression reset.", this);
      return true;
    }

    /// <summary>Toggles the cheats info view. Press F2 during a battle.</summary>
    public bool ToggleCheatsInfo()
    {
      var cheatsInfoUi = FindFirstObjectByType<CheatsInfoUi>(FindObjectsInactive.Include);
      if (cheatsInfoUi == null)
        return false;

      cheatsInfoUi.Toggle();
      return true;
    }

    /// <summary>Enables the player invincibility cheat. Press F10 during a battle.</summary>
    public bool EnablePlayerInvincibility()
    {
      if (_characterService == null)
        return false;

      _characterService.DestroyHealth();
      Debug.Log("Player invincibility enabled.", this);
      return true;
    }

    /// <summary>Teleports the player to the active goal. Press F5 during a battle.</summary>
    public bool TeleportPlayerToGoal()
    {
      var goal = TheGoal.Current;
      if (goal == null || goal.IsDestroyed)
        return false;

      if (!TeleportPlayer(goal.transform.position))
        return false;

      Debug.Log("Player teleported to the goal (cheat).", this);
      return true;
    }

    /// <summary>Enables the infinite battle timer cheat. Press F11 during a battle.</summary>
    public bool EnableInfiniteTimer()
    {
      if (_battleService == null)
        return false;

      _battleService.EnableInfiniteTimer();
      Debug.Log("Infinite battle timer enabled.", this);
      return true;
    }

    /// <summary>
    /// Destroys the current battle timer as if the player had smashed every digit. Press F4 during
    /// a battle. Goes through <see cref="BattleTimerObject.CheatDestroyAll"/> rather than calling
    /// <see cref="BattleService.DestroyTimer"/> directly, so the standard respawn (and everything
    /// else driven off a real digit's <c>Destroyed</c> event) fires exactly as it would from play.
    /// </summary>
    public bool DestroyTimer()
    {
      var timer = FindFirstObjectByType<BattleTimerObject>();
      if (timer == null || timer.IsDead)
        return false;

      timer.CheatDestroyAll();
      Debug.Log("Battle timer destroyed (cheat).", this);
      return true;
    }

    /// <summary>
    /// Moves the tagged player immediately. Set <see cref="_teleportPosition"/> in the inspector
    /// and press F8 for a repeatable profiling location, or call this method from Unity's CLI/eval
    /// command with any world-space position. F9 returns to the map origin.
    /// </summary>
    public bool TeleportPlayer(Vector3 worldPosition)
    {
      var player = GameObject.FindGameObjectWithTag("Player");
      if (player == null)
        return false;

      var previousPosition = player.transform.position;
      var movementAgent = player.GetComponent<MovementAgent>();
      if (movementAgent != null)
        movementAgent.Teleport(worldPosition);
      else
        player.transform.position = worldPosition;

      var positionDelta = player.transform.position - previousPosition;
      if (positionDelta != Vector3.zero)
        CinemachineCore.OnTargetObjectWarped(player.transform, positionDelta);

      Debug.Log($"Player teleported to {worldPosition}.", player);
      return true;
    }
  }
}
