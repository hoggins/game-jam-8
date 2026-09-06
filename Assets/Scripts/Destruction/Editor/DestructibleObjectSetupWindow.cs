using System.Collections.Generic;
using Map;
using Movement;
using UnityEditor;
using UnityEngine;

namespace Destruction.Editor
{
  public sealed class DestructibleObjectSetupWindow : EditorWindow
  {
    private const string PropTag = "Prop";

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

      EditorGUILayout.LabelField("Base Decay Settings (multiplier is derived per part from its volume)", EditorStyles.boldLabel);
      var so = new SerializedObject(this);
      so.Update();
      EditorGUILayout.PropertyField(
        so.FindProperty(nameof(baseDecaySettings)).FindPropertyRelative(nameof(PartDecaySettings.baseFallSpeed)),
        new GUIContent("Base Fall Speed"));
      EditorGUILayout.PropertyField(
        so.FindProperty(nameof(baseDecaySettings)).FindPropertyRelative(nameof(PartDecaySettings.timeoutMultiplier)),
        new GUIContent("Timeout Multiplier"));
      so.ApplyModifiedProperties();

      EditorGUILayout.Space();

      using (new EditorGUI.DisabledScope(!IsPrefabAsset(_prefab)))
      {
        if (GUILayout.Button("Configure Prefab"))
          ConfigurePrefab(_prefab);

        if (GUILayout.Button("Configure Prop"))
          ConfigureProp(_prefab);
      }

      if (GUILayout.Button("Apply to All"))
        ApplyToAllHouses();
    }

    private void ApplyToAllHouses()
    {
      var guids = AssetDatabase.FindAssets($"t:{nameof(HouseSet)}");
      if (guids.Length == 0)
      {
        Debug.LogError("No HouseSet asset found in the project.");
        return;
      }

      var configuredCount = 0;

      foreach (var guid in guids)
      {
        var houseSet = AssetDatabase.LoadAssetAtPath<HouseSet>(AssetDatabase.GUIDToAssetPath(guid));
        if (houseSet == null)
          continue;

        foreach (var house in houseSet.Houses)
        {
          if (house.prefab == null || !IsPrefabAsset(house.prefab))
            continue;

          ConfigurePrefab(house.prefab);
          configuredCount++;
        }
      }

      Debug.Log($"Applied destructible setup to {configuredCount} house entry(ies) from {guids.Length} HouseSet asset(s).");
    }

    private void ConfigurePrefab(GameObject prefab)
    {
      ConfigurePrefab(prefab, DestructibleObjectType.House, null, true);
    }

    private void ConfigureProp(GameObject prefab)
    {
      ConfigurePrefab(prefab, DestructibleObjectType.Prop, PropTag, false);
    }

    private void ConfigurePrefab(
      GameObject prefab,
      DestructibleObjectType objectType,
      string tag,
      bool spawnDestructionFx)
    {
      var prefabPath = AssetDatabase.GetAssetPath(prefab);
      var root = PrefabUtility.LoadPrefabContents(prefabPath);
      if (root == null)
      {
        Debug.LogError($"Could not load prefab contents: {prefabPath}");
        return;
      }

      var decaySettings = Resources.Load<EnvironmentDecaySettings>("EnvironmentDecaySettings");
      if (decaySettings == null)
      {
        Debug.LogError("EnvironmentDecaySettings asset was not found in Resources.");
        PrefabUtility.UnloadPrefabContents(root);
        return;
      }

      try
      {
        var meshCount = ConfigureMeshParts(
          root,
          baseDecaySettings,
          decaySettings.MaxFallSpeedMultiplier,
          out var replacedMeshColliderCount);
        ConfigureRoot(root, objectType, tag, spawnDestructionFx);

        EditorUtility.SetDirty(root);
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out var saved);
        if (!saved)
        {
          Debug.LogError($"Could not save configured prefab: {prefabPath}", root);
          return;
        }

        AssetDatabase.SaveAssets();

        Debug.Log(
          $"Configured {meshCount} destructible mesh part(s) on prefab '{prefabPath}' "
          + $"and replaced {replacedMeshColliderCount} MeshCollider(s) with BoxCollider(s).",
          root);
      }
      finally
      {
        PrefabUtility.UnloadPrefabContents(root);
      }
    }

    private static int ConfigureMeshParts(
      GameObject root,
      PartDecaySettings baseSettings,
      float maxSpeedMultiplier,
      out int replacedMeshColliderCount)
    {
      replacedMeshColliderCount = ReplaceMeshColliders(root);
      var meshFilters = root.GetComponentsInChildren<MeshFilter>(true);
      var configuredCount = 0;

      var partLayer = LayerMask.NameToLayer(DestructibleLayers.Parts);
      if (partLayer < 0)
        Debug.LogWarning($"Layer '{DestructibleLayers.Parts}' is not defined; leaving part layers unchanged.", root);

      foreach (var meshFilter in meshFilters)
      {
        if (meshFilter.sharedMesh == null)
          continue;

        var part = meshFilter.gameObject;
        if (partLayer >= 0)
          part.layer = partLayer;

        ConfigurePartCollider(part, meshFilter.sharedMesh.bounds);

        // Rigidbodies are added at runtime exactly when a part breaks off (DestructibleObject
        // .Impulse), not baked into the prefab: a Rigidbody on every intact part, times every
        // house on a big map, sits in the physics world doing nothing until it's needed. Strip
        // any left over from before this change, so older prefabs get cleaned up by re-patching.
        var body = part.GetComponent<Rigidbody>();
        if (body != null)
          Object.DestroyImmediate(body, true);

        var decayPart = part.GetComponent<DecayPart>();
        if (decayPart == null)
          decayPart = part.AddComponent<DecayPart>();

        decayPart.Configure(baseSettings.ForVolume(GetPartVolume(meshFilter), maxSpeedMultiplier));

        configuredCount++;
      }

      return configuredCount;
    }

    private static void ConfigurePartCollider(GameObject part, Bounds meshBounds)
    {
      var boxCollider = part.GetComponent<BoxCollider>();
      if (boxCollider == null)
        boxCollider = part.AddComponent<BoxCollider>();

      boxCollider.isTrigger = false;
      boxCollider.center = meshBounds.center;
      boxCollider.size = meshBounds.size;
    }

    private static int ReplaceMeshColliders(GameObject root)
    {
      var meshColliders = root.GetComponentsInChildren<MeshCollider>(true);
      var replacedCount = 0;

      foreach (var meshCollider in meshColliders)
      {
        if (meshCollider == null)
          continue;

        var part = meshCollider.gameObject;
        var bounds = meshCollider.sharedMesh != null
          ? meshCollider.sharedMesh.bounds
          : GetFallbackColliderBounds(meshCollider);

        ConfigurePartCollider(part, bounds);
        Object.DestroyImmediate(meshCollider, true);
        replacedCount++;
      }

      return replacedCount;
    }

    private static Bounds GetFallbackColliderBounds(MeshCollider meshCollider)
    {
      var renderer = meshCollider.GetComponent<Renderer>();
      if (renderer != null)
        return ToLocalBounds(renderer.bounds, meshCollider.transform);

      return ToLocalBounds(meshCollider.bounds, meshCollider.transform);
    }

    private static Bounds ToLocalBounds(Bounds worldBounds, Transform target)
    {
      var localBounds = default(Bounds);
      var hasBounds = false;

      foreach (var corner in GetCorners(worldBounds))
      {
        var localPoint = target.InverseTransformPoint(corner);
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

      return localBounds;
    }

    private static float GetPartVolume(MeshFilter meshFilter)
    {
      var localSize = meshFilter.sharedMesh.bounds.size;
      var worldSize = Vector3.Scale(localSize, meshFilter.transform.lossyScale);
      return worldSize.x * worldSize.y * worldSize.z;
    }

    private const string HitFxMaterialName = "GenericMaterial02";

    private static void ConfigureRoot(
      GameObject root,
      DestructibleObjectType objectType,
      string tag,
      bool spawnDestructionFx)
    {
      var boxCollider = root.GetComponent<BoxCollider>();
      if (boxCollider == null)
        boxCollider = root.AddComponent<BoxCollider>();

      boxCollider.isTrigger = false;
      SetBounds(boxCollider, root);

      var damagableLayer = LayerMask.NameToLayer(DestructibleLayers.Damagable);
      if (damagableLayer < 0)
        Debug.LogWarning($"Layer '{DestructibleLayers.Damagable}' is not defined; leaving root layer unchanged.", root);
      else
        root.layer = damagableLayer;

      if (root.GetComponent<FlowMapNoGoZone>() == null)
        root.AddComponent<FlowMapNoGoZone>();

      var destructibleObject = root.GetComponent<DestructibleObject>();
      if (destructibleObject == null)
        destructibleObject = root.AddComponent<DestructibleObject>();

      var destructibleObjectSo = new SerializedObject(destructibleObject);
      destructibleObjectSo.FindProperty("_spawnDestructionFx").boolValue = spawnDestructionFx;
      destructibleObjectSo.ApplyModifiedPropertiesWithoutUndo();

      var destructibleHealth = root.GetComponent<DestructibleHealth>();
      if (destructibleHealth == null)
        destructibleHealth = root.AddComponent<DestructibleHealth>();

      var destructibleHealthSo = new SerializedObject(destructibleHealth);
      destructibleHealthSo.FindProperty("_objectType").intValue = (int)objectType;
      destructibleHealthSo.ApplyModifiedPropertiesWithoutUndo();

      if (root.GetComponent<HitFx>() == null)
        root.AddComponent<HitFx>();

      if (!string.IsNullOrEmpty(tag))
        root.tag = tag;
    }

    private static bool UsesHitFxMaterial(GameObject root)
    {
      var renderers = root.GetComponentsInChildren<Renderer>(true);
      foreach (var renderer in renderers)
      {
        var materials = renderer.sharedMaterials;
        foreach (var material in materials)
        {
          if (material != null && material.name == HitFxMaterialName)
            return true;
        }
      }

      return false;
    }

    private static void SetBounds(BoxCollider collider, GameObject root)
    {
      var renderers = root.GetComponentsInChildren<Renderer>(true);
      var childColliders = root.GetComponentsInChildren<Collider>(true);
      var localBounds = default(Bounds);
      var hasBounds = false;

      foreach (var renderer in renderers)
        EncapsulateWorldBounds(ref localBounds, ref hasBounds, renderer.bounds, root.transform);

      foreach (var childCollider in childColliders)
      {
        if (childCollider.gameObject == root || childCollider == collider)
          continue;

        EncapsulateWorldBounds(
          ref localBounds,
          ref hasBounds,
          childCollider.bounds,
          root.transform);
      }

      if (!hasBounds)
      {
        Debug.LogWarning(
          $"Prefab '{root.name}' has no child renderers or colliders; using a unit root no-go collider.",
          root);
        return;
      }

      collider.center = localBounds.center;
      collider.size = localBounds.size;
    }

    private static void EncapsulateWorldBounds(
      ref Bounds localBounds,
      ref bool hasBounds,
      Bounds worldBounds,
      Transform rootTransform)
    {
      foreach (var corner in GetCorners(worldBounds))
      {
        var localPoint = rootTransform.InverseTransformPoint(corner);
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
