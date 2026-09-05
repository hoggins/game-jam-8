using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using Model;

namespace App
{
  [DisallowMultipleComponent]
  public sealed class DebugCheatManager : MonoBehaviour
  {
    [Header("Player Teleport")]
    [SerializeField] private Vector3 _teleportPosition;

    [Inject] private CharacterService _characterService;
    [Inject] private BattleService _battleService;

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

      if (keyboard.f8Key.wasPressedThisFrame)
        TeleportPlayer(_teleportPosition);

      if (keyboard.f9Key.wasPressedThisFrame)
        TeleportPlayer(Vector3.zero);

      if (keyboard.f10Key.wasPressedThisFrame)
        EnablePlayerInvincibility();

      if (keyboard.f11Key.wasPressedThisFrame)
        EnableInfiniteTimer();
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
    /// Moves the tagged player immediately. Set <see cref="_teleportPosition"/> in the inspector
    /// and press F8 for a repeatable profiling location, or call this method from Unity's CLI/eval
    /// command with any world-space position. F9 returns to the map origin.
    /// </summary>
    public bool TeleportPlayer(Vector3 worldPosition)
    {
      var player = GameObject.FindGameObjectWithTag("Player");
      if (player == null)
        return false;

      player.transform.position = worldPosition;
      Debug.Log($"Player teleported to {worldPosition}.", player);
      return true;
    }
  }
}
