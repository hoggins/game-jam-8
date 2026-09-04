using UnityEngine;
using UnityEngine.InputSystem;

namespace App
{
  [DisallowMultipleComponent]
  public sealed class DebugCheatManager : MonoBehaviour
  {
    private void Update()
    {
      var keyboard = Keyboard.current;
      if (keyboard == null)
        return;

      if (keyboard.equalsKey.wasPressedThisFrame || keyboard.numpadPlusKey.wasPressedThisFrame)
        Time.timeScale *= 2f;

      if (keyboard.minusKey.wasPressedThisFrame || keyboard.numpadMinusKey.wasPressedThisFrame)
        Time.timeScale *= 0.5f;
    }
  }
}
