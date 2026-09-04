using System.Collections.Generic;
using UnityEngine;

namespace Pooling
{
  public sealed class Pool : System.IDisposable
  {
    private readonly Dictionary<GameObject, Queue<GameObject>> _availableByPrefab = new();
    private readonly Dictionary<GameObject, GameObject> _prefabByInstance = new();
    private readonly Dictionary<GameObject, int> _prewarmCountByPrefab = new();
    private readonly HashSet<GameObject> _availableInstances = new();
    private readonly HashSet<GameObject> _instances = new();

    public void Prewarm(GameObject prefab, int count)
    {
      if (prefab == null)
        return;

      count = Mathf.Max(0, count);
      _prewarmCountByPrefab.TryGetValue(prefab, out var prewarmedCount);
      if (count <= prewarmedCount)
        return;

      _prewarmCountByPrefab[prefab] = count;
      var available = GetAvailable(prefab);
      for (var i = prewarmedCount; i < count; i++)
      {
        var instance = CreateInstance(prefab);
        available.Enqueue(instance);
        _availableInstances.Add(instance);
      }
    }

    public GameObject Get(
      GameObject prefab,
      Vector3 position,
      Quaternion rotation)
    {
      if (prefab == null)
        return null;

      var available = GetAvailable(prefab);
      while (available.Count > 0)
      {
        var instance = available.Dequeue();
        _availableInstances.Remove(instance);
        if (instance == null)
          continue;

        instance.transform.SetPositionAndRotation(position, rotation);
        instance.gameObject.SetActive(true);
        return instance;
      }

      var created = CreateInstance(prefab);
      created.transform.SetPositionAndRotation(position, rotation);
      created.gameObject.SetActive(true);
      return created;
    }

    public void Dispose()
    {
      foreach (var instance in _instances)
      {
        if (instance != null)
          Object.Destroy(instance.gameObject);
      }

      _availableByPrefab.Clear();
      _prefabByInstance.Clear();
      _prewarmCountByPrefab.Clear();
      _availableInstances.Clear();
      _instances.Clear();
    }

    public void Release(GameObject instance)
    {
      if (instance == null
          || !_instances.Contains(instance)
          || !_prefabByInstance.TryGetValue(instance, out var prefab)
          || !_availableInstances.Add(instance))
        return;

      instance.gameObject.SetActive(false);
      GetAvailable(prefab).Enqueue(instance);
    }

    private GameObject CreateInstance(GameObject prefab)
    {
      var instance = Object.Instantiate(prefab);
      instance.gameObject.SetActive(false);
      instance.GetComponent<PoolInSeconds>()?.SetPool(this);
      _instances.Add(instance);
      _prefabByInstance.Add(instance, prefab);
      return instance;
    }

    private Queue<GameObject> GetAvailable(GameObject prefab)
    {
      if (_availableByPrefab.TryGetValue(prefab, out var available))
        return available;

      available = new Queue<GameObject>();
      _availableByPrefab.Add(prefab, available);
      return available;
    }
  }
}
