using System.Collections.Generic;
using Movement;
using UnityEditor;
using UnityEngine;

namespace Destruction.Editor
{
  public sealed class DestructibleObjectSetupWindow : EditorWindow
  {
    private GameObject _prefab;
    [SerializeField] private PartDecaySettings baseDecaySettings = new();

    [MenuItem("Tools/Destruction/Destructible Object Setup")]
    private static void Open() =>
      GetWindow<DestructibleObjectSetupWindow>("Destructible Setup");

    private void OnGUI()
    {
      EditorGUILayout.HelpBox(
        "Adds the components needed to break a prefab into independent physical parts.",
        MessageType.Info);

      _prefab = (GameObject)EditorGUILayout.ObjectField(
        "Prefab", _prefab, typeof(GameObject), false);

      if (_prefab != null && !IsPrefabAsset(_prefab))
        EditorGUILayout.HelpBox("Select a prefab asset from the Project window.", MessageType.Warning);

      EditorGUILayout.Space();

      EditorGUILayout.LabelField("Base Decay Settings (scaled per part by its volume)", EditorStyles.boldLabel);
      var so = new SerializedObject(this);
      so.Update();
      EditorGUILayout.PropertyField(so.FindProperty(nameof(baseDecaySettings)), true);
      so.ApplyModifiedProperties();

      EditorGUILayout.Space();

      using (new EditorGUI.DisabledScope(!IsPrefabAsset(_prefab)))
      {
        if (GUILayout.Button("Configure Prefab"))
          ConfigurePrefab();
      }
    }

    private void ConfigurePrefab()
    {
      var prefabPath = AssetDatabase.GetAssetPath(_prefab);
      var root = PrefabUtility.LoadPrefabContents(prefabPath);
      if (root == null)
      {
        Debug.LogError($"Could not load prefab contents: {prefabPath}");
        return;
      }

      try
      {
        var meshCount = ConfigureMeshParts(root, baseDecaySettings);
        ConfigureRoot(root);

        EditorUtility.SetDirty(root);
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out var saved);
        if (!saved)
        {
          Debug.LogError($"Could not save configured prefab: {prefabPath}", root);
          return;
        }

        AssetDatabase.SaveAssets();

        Debug.Log(
          $"Configured {meshCount} destructible mesh part(s) on prefab '{prefabPath}'.",
          root);
      }
      finally
      {
        PrefabUtility.UnloadPrefabContents(root);
      }
    }

    private static int ConfigureMeshParts(GameObject root, PartDecaySettings baseSettings)
    {
      var meshFilters = root.GetComponentsInChildren<MeshFilter>(true);
      var configuredCount = 0;

      foreach (var meshFilter in meshFilters)
      {
        if (meshFilter.sharedMesh == null)
          continue;

        var part = meshFilter.gameObject;
        var meshCollider = part.GetComponent<MeshCollider>();
        if (meshCollider == null && part.GetComponent<Collider>() == null)
          meshCollider = part.AddComponent<MeshCollider>();

        if (meshCollider != null)
        {
          meshCollider.sharedMesh = meshFilter.sharedMesh;
          meshCollider.convex = true;
        }

        var body = part.GetComponent<Rigidbody>();
        if (body == null)
          body = part.AddComponent<Rigidbody>();

        body.isKinematic = true;

        var decayPart = part.GetComponent<DecayPart>();
        if (decayPart == null)
          decayPart = part.AddComponent<DecayPart>();

        decayPart.Configure(baseSettings.ForVolume(GetPartVolume(meshFilter)));

        configuredCount++;
      }

      return configuredCount;
    }

    private static float GetPartVolume(MeshFilter meshFilter)
    {
      var localSize = meshFilter.sharedMesh.bounds.size;
      var worldSize = Vector3.Scale(localSize, meshFilter.transform.lossyScale);
      return worldSize.x * worldSize.y * worldSize.z;
    }

    private static void ConfigureRoot(GameObject root)
    {
      var rootCollider = root.GetComponent<Collider>();
      if (rootCollider == null)
      {
        var boxCollider = root.AddComponent<BoxCollider>();
        boxCollider.isTrigger = true;
        SetBounds(boxCollider, root);
      }

      if (root.GetComponent<FlowMapNoGoZone>() == null)
        root.AddComponent<FlowMapNoGoZone>();

      if (root.GetComponent<DestructibleObject>() == null)
        root.AddComponent<DestructibleObject>();
    }

    private static void SetBounds(BoxCollider collider, GameObject root)
    {
      var renderers = root.GetComponentsInChildren<Renderer>(true);
      if (renderers.Length == 0)
      {
        Debug.LogWarning(
          $"Prefab '{root.name}' has no Renderer components; using a unit root no-go collider.",
          root);
        return;
      }

      var localBounds = default(Bounds);
      var hasBounds = false;
      foreach (var renderer in renderers)
      {
        var worldBounds = renderer.bounds;
        foreach (var corner in GetCorners(worldBounds))
        {
          var localPoint = root.transform.InverseTransformPoint(corner);
          if (!hasBounds)
          {
            localBounds = new Bounds(localPoint, Vector3.zero);
            hasBounds = true;
          }
          else
          {
            localBounds.Encapsulate(localPoint);
          }
        }
      }

      collider.center = localBounds.center;
      collider.size = localBounds.size;
    }

    private static IEnumerable<Vector3> GetCorners(Bounds bounds)
    {
      var min = bounds.min;
      var max = bounds.max;
      yield return new Vector3(min.x, min.y, min.z);
      yield return new Vector3(min.x, min.y, max.z);
      yield return new Vector3(min.x, max.y, min.z);
      yield return new Vector3(min.x, max.y, max.z);
      yield return new Vector3(max.x, min.y, min.z);
      yield return new Vector3(max.x, min.y, max.z);
      yield return new Vector3(max.x, max.y, min.z);
      yield return new Vector3(max.x, max.y, max.z);
    }

    private static bool IsPrefabAsset(GameObject target) =>
      target != null
      && PrefabUtility.IsPartOfPrefabAsset(target)
      && PrefabUtility.GetPrefabAssetType(target) != PrefabAssetType.NotAPrefab;
  }
}
