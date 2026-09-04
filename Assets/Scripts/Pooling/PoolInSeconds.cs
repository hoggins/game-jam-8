using System.Collections;
using UnityEngine;

namespace Pooling
{
  public sealed class PoolInSeconds : MonoBehaviour
  {
    [SerializeField, Min(0.01f)] private float _seconds = 0.5f;

    private Pool _pool;
    private Coroutine _returnCoroutine;

    internal void SetPool(Pool pool)
    {
      _pool = pool;
    }

    private void OnEnable()
    {
      if (_pool != null)
        _returnCoroutine = StartCoroutine(ReturnAfterSeconds());
    }

    private void OnDisable()
    {
      if (_returnCoroutine == null)
        return;

      StopCoroutine(_returnCoroutine);
      _returnCoroutine = null;
    }

    private IEnumerator ReturnAfterSeconds()
    {
      yield return new WaitForSeconds(_seconds);
      _returnCoroutine = null;
      _pool.Release(gameObject);
    }

    private void OnValidate()
    {
      _seconds = Mathf.Max(0.01f, _seconds);
    }
  }
}
