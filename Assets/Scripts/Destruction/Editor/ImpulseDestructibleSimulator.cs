using UnityEditor;
using UnityEngine;

namespace Destruction.Editor
{
  public class ImpulseDestructibleSimulator : EditorWindow
  {
    private GameObject _prefab;
    private float _magnitude = 5f;
    private GameObject _spawned;
    private GameObject _origin;


    [MenuItem("Tools/Destruction/Impulse Simulator")]
    private static void Open() => GetWindow<ImpulseDestructibleSimulator>("Impulse Simulator");

    private void OnGUI()
    {
      _prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", _prefab, typeof(GameObject), false);
      _magnitude = EditorGUILayout.FloatField("Magnitude", _magnitude);

      EditorGUILayout.Space();

      using (new EditorGUI.DisabledScope(_prefab == null))
      {
        if (GUILayout.Button("Spawn"))
          Spawn();
      }

      using (new EditorGUI.DisabledScope(_origin != null))
        if (GUILayout.Button("Spawn Force Origin"))
          SpawnOrigin();

      using (new EditorGUI.DisabledScope(_spawned == null || _origin == null))
      {
        if (GUILayout.Button("Pulse"))
          Pulse();
      }

      using (new EditorGUI.DisabledScope(_spawned == null))
      {
        if (GUILayout.Button("Clear"))
          Clear();
      }
    }

    private void Spawn()
    {
      if (_spawned != null)
        DestroyImmediate(_spawned);

      _spawned = (GameObject)PrefabUtility.InstantiatePrefab(_prefab);
      _spawned.transform.position = Vector3.zero;
    }

    private void SpawnOrigin()
    {
      if (_origin != null)
        DestroyImmediate(_origin);

      _origin = new GameObject("ForceOrigin");
      _origin.AddComponent<ForceOriginMarker>();
      _origin.transform.position = _spawned != null ? _spawned.transform.position : Vector3.zero;
    }

    private void Pulse()
    {
      var destructible = _spawned.GetComponent<DestructibleObject>();
      if (destructible == null)
      {
        Debug.LogWarning($"{_spawned.name} has no ImpulseDestructible component.", _spawned);
        return;
      }

      destructible.Impulse(_origin.transform.position, _magnitude);
    }

    private void Clear()
    {
      if (_spawned != null)
        DestroyImmediate(_spawned);

      _spawned = null;
    }
  }
}
