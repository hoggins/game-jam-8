using UnityEngine;
using UnityEngine.InputSystem;

namespace App
{
  [DisallowMultipleComponent]
  public sealed class DebugCheatManager : MonoBehaviour
  {
    [Header("Player Teleport")]
    [SerializeField] private Vector3 _teleportPosition;

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
