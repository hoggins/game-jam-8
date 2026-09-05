using Destruction;
using SceneHud;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Upgrade.Editor
{
  /// <summary>
  /// Builds the upgrade house from the existing House01 art, retaining its destruction and decay
  /// setup while adding a static scene-HUD camera.
  /// </summary>
  public static class BattleUpgradePrefabBuilder
  {
    private const string SourcePrefabPath = "Assets/Resources/Descructable/House01.prefab";
    private const string PrefabPath = "Assets/Resources/Descructable/BattleUpgrade.prefab";

    private const float HudCameraHeight = 10f;
    private const float HudCameraSize = 3.75f;
    private const int HudTextureSize = 256;

    // The ground is intentionally absent so the render texture keeps a transparent background.
    private static readonly string[] HudCameraLayers = { DestructibleLayers.Parts };

    [MenuItem("Tools/Destruction/Rebuild Battle Upgrade Prefab")]
    private static void RebuildMenu() => Debug.Log(Rebuild());

    public static string Rebuild()
    {
      var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath);
      if (source == null)
        return $"Source House01 prefab was not found at {SourcePrefabPath}.";

      var root = new GameObject("BattleUpgrade");
      var house = (GameObject)PrefabUtility.InstantiatePrefab(source, root.transform);
      house.name = "House01";

      var body = house.GetComponentInChildren<DestructibleObject>(true);
      if (body == null)
      {
        Object.DestroyImmediate(root);
        return "House01 has no DestructibleObject.";
      }

      var health = body.GetComponent<DestructibleHealth>();
      if (health == null)
      {
        Object.DestroyImmediate(root);
        return "House01 has no DestructibleHealth.";
      }

      Apply(health, so =>
        so.FindProperty("_objectType").intValue = (int)DestructibleObjectType.Upgrade);

      BuildHudCamera(root);

      var upgrade = root.AddComponent<BattleUpgradeObject>();
      Apply(upgrade, so => so.FindProperty("_body").objectReferenceValue = body);
      Apply(upgrade, so => so.FindProperty("_hudCamera").objectReferenceValue = root.GetComponentInChildren<Camera>(true));

      PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out var saved);
      Object.DestroyImmediate(root);
      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();

      return $"BattleUpgrade prefab saved={saved} path={PrefabPath}";
    }

    private static void BuildHudCamera(GameObject root)
    {
      var cameraRoot = new GameObject("SceneHudCamera");
      cameraRoot.transform.SetParent(root.transform, false);
      cameraRoot.transform.localPosition = new Vector3(0f, HudCameraHeight, 0f);
      cameraRoot.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

      var camera = cameraRoot.AddComponent<Camera>();
      camera.orthographic = true;
      camera.orthographicSize = HudCameraSize;
      camera.cullingMask = LayerMask.GetMask(HudCameraLayers);
      camera.clearFlags = CameraClearFlags.SolidColor;
      camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
      camera.nearClipPlane = 0.1f;
      camera.farClipPlane = HudCameraHeight;
      camera.useOcclusionCulling = false;
      camera.allowHDR = false;
      camera.allowMSAA = false;

      var cameraData = cameraRoot.AddComponent<UniversalAdditionalCameraData>();
      cameraData.renderType = CameraRenderType.Base;
      cameraData.renderPostProcessing = false;
      cameraData.renderShadows = false;
      cameraData.requiresColorOption = CameraOverrideOption.Off;
      cameraData.requiresDepthOption = CameraOverrideOption.Off;

      var element = cameraRoot.AddComponent<SceneHudElement>();
      Apply(element, so =>
      {
        so.FindProperty("_id").intValue = (int)SceneHudElementId.Upgrade;
        so.FindProperty("_resolution").vector2IntValue = new Vector2Int(HudTextureSize, HudTextureSize);
      });
    }

    private static void Apply(Object target, System.Action<SerializedObject> apply)
    {
      var serialized = new SerializedObject(target);
      serialized.Update();
      apply(serialized);
      serialized.ApplyModifiedPropertiesWithoutUndo();
    }
  }
}
