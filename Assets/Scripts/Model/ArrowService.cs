using System;
using UnityEngine.Scripting;

namespace Model
{
  [Preserve]
  public sealed class ArrowService : IBattleStarted
  {
    public event Action ArrowDestroyed;

    public bool IsDestroyed { get; private set; }

    public void DestroyArrow()
    {
      if (IsDestroyed)
        return;

      IsDestroyed = true;
      ArrowDestroyed?.Invoke();
    }

    void IBattleStarted.OnBattleStarted()
    {
      IsDestroyed = false;
    }
  }
}
