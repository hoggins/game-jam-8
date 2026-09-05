using System;
using System.Collections.Generic;
using System.IO;
using Arrow;
using Balance;
using Movement;
using SceneHud;
using UnityEditor;
using UnityEngine;

namespace Destruction.Editor
{
  /// <summary>
  /// Creates the scene-authored final goal from an already configured destructible prefab and keeps
  /// the goal's HUD wiring in one editor action. The base prefab is intentionally an input rather
  /// than a hidden hard-coded dependency, so another house can become the goal later without
  /// changing runtime code.
  /// </summary>
  public sealed class TheGoalSetupWindow : EditorWindow
  {
    private const string DefaultSourcePath = "Assets/Resources/Descructable/House01.prefab";
    private const string DefaultGoalPrefabPath = "Assets/Resources/Descructable/TheGoal.prefab";
    private const string ArrowPrefabPath = "Assets/Resources/Descructable/BattleArrow.prefab";
    private const string GoalArrowPrefabPath = "Assets/Resources/Descructable/TheGoalArrow.prefab";
    private const string BattleHudPrefabPath = "Assets/Resources/Prefabs/UI/Battle/BattleHud.prefab";
    private const int DefaultGoalMaxHealth = 100;

    private GameObject _sourcePrefab;
    [SerializeField] private string _goalPrefabPath = DefaultGoalPrefabPath;
    [SerializeField] private int _goalMaxHealth = DefaultGoalMaxHealth;

    [MenuItem("Tools/Destruction/The Goal Setup")]
    private static void Open() =>
      GetWindow<TheGoalSetupWindow>("TheGoal Setup");

    [MenuItem("Tools/Destruction/Build The Goal Assets")]
    private static void BuildDefaultAssetsMenu() => Debug.Log(BuildDefaultAssets());

    public static string BuildDefaultAssets()
    {
      var source = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultSourcePath);
      if (source == null)
        return $"Source House01 prefab was not found at {DefaultSourcePath}.";

      var messages = new List<string>
      {
        CreateGoalVariant(source, DefaultGoalPrefabPath, DefaultGoalMaxHealth),
        CreateGoalArrowVariant(),
        EnsureGoalHudWidget(),
      };

      return string.Join("\n", messages);
    }

    private void OnGUI()
    {
      EditorGUILayout.HelpBox(
        "Creates a scene-authored final objective from an existing destructible prefab. Run "
        + "Destructible Object Setup on a new house first if its parts are not configured yet.",
        MessageType.Info);

      _sourcePrefab = (GameObject)EditorGUILayout.ObjectField(
        "Base house prefab", _sourcePrefab, typeof(GameObject), false);

      if (_sourcePrefab != null && !IsPrefabAsset(_sourcePrefab))
        EditorGUILayout.HelpBox("Select a prefab asset from the Project window.", MessageType.Warning);

      _goalPrefabPath = EditorGUILayout.TextField("Goal prefab path", _goalPrefabPath);
      _goalMaxHealth = EditorGUILayout.IntField("Goal max health", _goalMaxHealth);
      _goalMaxHealth = Mathf.Max(0, _goalMaxHealth);

      EditorGUILayout.Space();

      using (new EditorGUI.DisabledScope(!IsPrefabAsset(_sourcePrefab)))
      {
        if (GUILayout.Button("Create / Configure TheGoal Variant"))
          Debug.Log(CreateGoalVariant(_sourcePrefab, _goalPrefabPath, _goalMaxHealth));
      }

      if (GUILayout.Button("Create / Configure TheGoal Arrow Variant"))
        Debug.Log(CreateGoalArrowVariant());

      if (GUILayout.Button("Add TheGoal HUD Widget"))
        Debug.Log(EnsureGoalHudWidget());

      EditorGUILayout.Space();

      if (GUILayout.Button("Build Default Goal Assets"))
        Debug.Log(BuildDefaultAssets());
    }

    public static string CreateGoalVariant(GameObject sourcePrefab, string destinationPath, int goalMaxHealth)
    {
      if (!IsPrefabAsset(sourcePrefab))
        return "TheGoal source must be a prefab asset.";

      destinationPath = NormalizeAssetPath(destinationPath);
      if (!IsProjectAssetPath(destinationPath))
        return $"TheGoal destination must be under Assets/: {destinationPath}";

      EnsureGoalBalanceEntry(goalMaxHealth);

      if (AssetDatabase.LoadAssetAtPath<GameObject>(destinationPath) != null)
        return ConfigureGoalPrefab(destinationPath, goalMaxHealth);

      var instance = (GameObject)PrefabUtility.InstantiatePrefab(sourcePrefab);
      if (instance == null)
        return $"Could not instantiate TheGoal source prefab: {sourcePrefab.name}.";

      try
      {
        instance.name = Path.GetFileNameWithoutExtension(destinationPath);
        ConfigureGoalRoot(instance, goalMaxHealth);

        var saved = PrefabUtility.SaveAsPrefabAssetAndConnect(
          instance, destinationPath, InteractionMode.UserAction);
        if (saved == null)
          return $"Could not save TheGoal prefab: {destinationPath}";

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return $"TheGoal prefab variant saved: {destinationPath}";
      }
      finally
      {
        DestroyImmediate(instance);
      }
    }

    public static string CreateGoalArrowVariant()
    {
      var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArrowPrefabPath);
      if (sourcePrefab == null)
        return $"BattleArrow prefab was not found at {ArrowPrefabPath}.";

      if (AssetDatabase.LoadAssetAtPath<GameObject>(GoalArrowPrefabPath) != null)
        return ConfigureGoalArrowPrefab(GoalArrowPrefabPath);

      var instance = (GameObject)PrefabUtility.InstantiatePrefab(sourcePrefab);
      if (instance == null)
        return "Could not instantiate the BattleArrow prefab.";

      try
      {
        instance.name = Path.GetFileNameWithoutExtension(GoalArrowPrefabPath);
        ConfigureGoalArrowRoot(instance);

        var saved = PrefabUtility.SaveAsPrefabAssetAndConnect(
          instance, GoalArrowPrefabPath, InteractionMode.UserAction);
        if (saved == null)
          return $"Could not save TheGoal arrow prefab: {GoalArrowPrefabPath}";

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return $"TheGoal arrow prefab variant saved: {GoalArrowPrefabPath}";
      }
      finally
      {
        DestroyImmediate(instance);
      }
    }

    public static string EnsureGoalHudWidget()
    {
      var root = PrefabUtility.LoadPrefabContents(BattleHudPrefabPath);
      if (root == null)
        return $"Could not load Battle HUD prefab: {BattleHudPrefabPath}";

      try
      {
        var goalWidget = FindChild(root.transform, "TheGoalArrowWidget");
        var arrowWidget = FindChild(root.transform, "BattleArrowWidget");
        if (goalWidget == null && arrowWidget == null)
          return "BattleArrowWidget was not found in the Battle HUD prefab.";

        if (goalWidget == null)
        {
          goalWidget = Instantiate(arrowWidget.gameObject, arrowWidget.parent).transform;
          goalWidget.name = "TheGoalArrowWidget";

          var sourceRect = arrowWidget.GetComponent<RectTransform>();
          var goalRect = goalWidget.GetComponent<RectTransform>();
          if (sourceRect != null && goalRect != null)
          {
            goalRect.anchoredPosition = sourceRect.anchoredPosition
              + Vector2.down * (goalRect.sizeDelta.y + 16f);
            goalRect.SetSiblingIndex(arrowWidget.GetSiblingIndex() + 1);
          }
        }

        var view = goalWidget.GetComponent<SceneHudElementView>();
        if (view == null)
          return "TheGoal HUD widget has no SceneHudElementView component.";

        Apply(view, so => so.FindProperty("_id").intValue = (int)SceneHudElementId.GoalArrow);

        EditorUtility.SetDirty(root);
        PrefabUtility.SaveAsPrefabAsset(root, BattleHudPrefabPath, out var saved);
        if (!saved)
          return $"Could not save the Battle HUD prefab: {BattleHudPrefabPath}";

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return "TheGoal HUD widget added to the Battle HUD prefab.";
      }
      finally
      {
        PrefabUtility.UnloadPrefabContents(root);
      }
    }

    private static string ConfigureGoalPrefab(string prefabPath, int goalMaxHealth)
    {
      var root = PrefabUtility.LoadPrefabContents(prefabPath);
      if (root == null)
        return $"Could not load TheGoal prefab: {prefabPath}";

      try
      {
        ConfigureGoalRoot(root, goalMaxHealth);
        EditorUtility.SetDirty(root);
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out var saved);
        if (!saved)
          return $"Could not save TheGoal prefab: {prefabPath}";

        AssetDatabase.SaveAssets();
        return $"TheGoal prefab configured: {prefabPath}";
      }
      finally
      {
        PrefabUtility.UnloadPrefabContents(root);
      }
    }

    private static string ConfigureGoalArrowPrefab(string prefabPath)
    {
      var root = PrefabUtility.LoadPrefabContents(prefabPath);
      if (root == null)
        return $"Could not load TheGoal arrow prefab: {prefabPath}";

      try
      {
        ConfigureGoalArrowRoot(root);
        EditorUtility.SetDirty(root);
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out var saved);
        if (!saved)
          return $"Could not save TheGoal arrow prefab: {prefabPath}";

        AssetDatabase.SaveAssets();
        return $"TheGoal arrow prefab configured: {prefabPath}";
      }
      finally
      {
        PrefabUtility.UnloadPrefabContents(root);
      }
    }

    private static void ConfigureGoalRoot(GameObject root, int goalMaxHealth)
    {
      var boxCollider = root.GetComponent<BoxCollider>();
      if (boxCollider == null)
        boxCollider = root.AddComponent<BoxCollider>();

      boxCollider.isTrigger = false;
      SetBounds(boxCollider, root);

      var damagableLayer = LayerMask.NameToLayer(DestructibleLayers.Damagable);
      if (damagableLayer >= 0)
        root.layer = damagableLayer;
      else
        Debug.LogWarning($"Layer '{DestructibleLayers.Damagable}' is not defined; leaving goal layer unchanged.", root);

      if (root.GetComponent<FlowMapNoGoZone>() == null)
        root.AddComponent<FlowMapNoGoZone>();

      var body = root.GetComponent<DestructibleObject>();
      if (body == null)
        body = root.AddComponent<DestructibleObject>();

      var health = root.GetComponent<DestructibleHealth>();
      if (health == null)
        health = root.AddComponent<DestructibleHealth>();

      Apply(health, so =>
        so.FindProperty("_objectType").intValue = (int)DestructibleObjectType.Goal);

      var goal = root.GetComponent<TheGoal>();
      if (goal == null)
        goal = root.AddComponent<TheGoal>();

      Apply(goal, so => so.FindProperty("_body").objectReferenceValue = body);

      if (UsesHitFxMaterial(root) && root.GetComponent<HitFx>() == null)
        root.AddComponent<HitFx>();

      if (root.GetComponentsInChildren<DecayPart>(true).Length == 0)
        Debug.LogWarning(
          $"TheGoal prefab '{root.name}' has no DecayPart children. Configure its source with "
          + "Tools > Destruction > Destructible Object Setup first.", root);

      EnsureGoalBalanceEntry(goalMaxHealth);
    }

    private static void ConfigureGoalArrowRoot(GameObject root)
    {
      var arrow = root.GetComponent<BattleArrowObject>();
      if (arrow == null)
      {
        Debug.LogError("TheGoal arrow source has no BattleArrowObject component.", root);
        return;
      }

      Apply(arrow, so =>
      {
        var target = so.FindProperty("_target");
        if (target != null)
          target.intValue = 1;
      });

      var elements = root.GetComponentsInChildren<SceneHudElement>(true);
      if (elements.Length == 0)
      {
        Debug.LogError("TheGoal arrow source has no SceneHudElement camera.", root);
        return;
      }

      Apply(elements[0], so =>
        so.FindProperty("_id").intValue = (int)SceneHudElementId.GoalArrow);
    }

    private static void EnsureGoalBalanceEntry(int goalMaxHealth)
    {
      var config = Resources.Load<BattleBalanceConfig>("BattleBalanceConfig");
      if (config == null)
      {
        Debug.LogError("BattleBalanceConfig asset was not found in Resources.");
        return;
      }

      var serialized = new SerializedObject(config);
      serialized.Update();
      var entries = serialized.FindProperty("_destructibleMaxHealth");
      var goalType = (int)DestructibleObjectType.Goal;
      var found = false;

      for (var i = 0; i < entries.arraySize; i++)
      {
        var entry = entries.GetArrayElementAtIndex(i);
        if (entry.FindPropertyRelative("type").intValue != goalType)
          continue;

        entry.FindPropertyRelative("maxHealth").intValue = Mathf.Max(0, goalMaxHealth);
        found = true;
        break;
      }

      if (!found)
      {
        entries.InsertArrayElementAtIndex(entries.arraySize);
        var entry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
        entry.FindPropertyRelative("type").intValue = goalType;
        entry.FindPropertyRelative("maxHealth").intValue = Mathf.Max(0, goalMaxHealth);
      }

      serialized.ApplyModifiedPropertiesWithoutUndo();
      EditorUtility.SetDirty(config);
      AssetDatabase.SaveAssets();
    }

    private static Transform FindChild(Transform root, string name)
    {
      if (root.name == name)
        return root;

      for (var i = 0; i < root.childCount; i++)
      {
        var found = FindChild(root.GetChild(i), name);
        if (found != null)
          return found;
      }

      return null;
    }

    private const string HitFxMaterialName = "GenericMaterial02";

    private static bool UsesHitFxMaterial(GameObject root)
    {
      foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        foreach (var material in renderer.sharedMaterials)
          if (material != null && material.name == HitFxMaterialName)
            return true;

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
          ref localBounds, ref hasBounds, childCollider.bounds, root.transform);
      }

      if (hasBounds)
      {
        collider.center = localBounds.center;
        collider.size = localBounds.size;
      }
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

    private static void Apply(UnityEngine.Object target, Action<SerializedObject> apply)
    {
      var serialized = new SerializedObject(target);
      serialized.Update();
      apply(serialized);
      serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static string NormalizeAssetPath(string path) =>
      string.IsNullOrWhiteSpace(path) ? DefaultGoalPrefabPath : path.Replace('\\', '/');

    private static bool IsProjectAssetPath(string path) =>
      path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)
      && !Path.IsPathRooted(path);

    private static bool IsPrefabAsset(GameObject target) =>
      target != null
      && PrefabUtility.IsPartOfPrefabAsset(target)
      && PrefabUtility.GetPrefabAssetType(target) != PrefabAssetType.NotAPrefab;
  }
}
