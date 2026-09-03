using System;
using UnityEngine;
using VContainer.Unity;

namespace App.TmpTest
{
  [UnityEngine.Scripting.Preserve]
  internal class ScopeLogger : IInitializable, ITickable, IDisposable
  {
    void IInitializable.Initialize()
    {
      Debug.Log($"[{nameof(ScopeLogger)}] Initialized");
    }

    void ITickable.Tick()
    {
      Debug.Log($"[{nameof(ScopeLogger)}] Tick");
    }

    void IDisposable.Dispose()
    {
      Debug.Log($"[{nameof(ScopeLogger)}] Disposed");
    }
  }
}