using UnityEngine;
using VContainer;

namespace App.TmpTest
{
  public class GoLogger : MonoBehaviour
  {
    [Inject] private ScopeLogger _logger;

    private void Awake()
    {
      this.AsInjected();

      if (_logger == null)
        Debug.LogError("Nothing injected");
      else
        Debug.Log("Injection - ok");
    }
  }
}