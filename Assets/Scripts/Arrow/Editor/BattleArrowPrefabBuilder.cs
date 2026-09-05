using Destruction;
using Movement;
using SceneHud;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Arrow.Editor
{
  /// <summary>
  /// Generates the BattleArrow prefab: the modelled arrow from <see cref="SourcePrefabPath"/> turned
  /// into a destructible scene HUD object — its segments become the breakable parts, and a HUD
  /// camera is hung above it.
  ///
  /// The art is copied in rather than nested, so this prefab is self-contained: **rerun the menu item
  /// after the artist changes Arrow.prefab**, or the battle keeps using the old shape.
  /// </summary>
  public static class BattleArrowPrefabBuilder
  {
    /// The modelled arrow: three chevron segments, each mirrored in X, six renderers in all.
    private const string SourcePrefabPath = "Assets/Resources/Prefabs/Interface/Arrow.prefab";
    private const string PrefabPath = "Assets/Resources/Descructable/BattleArrow.prefab";

    /// <summary>
    /// Yaw applied to the art so it matches the runtime convention that the glyph points along the
    /// prefab root's +Z (see <see cref="BattleArrowObject"/>, which measures the root's facing to
    /// roll the HUD camera). The model as authored points along -Z, hence the half turn. If a new
    /// export points somewhere else, this is the constant to change.
    /// </summary>
    private const float SourceYawCorrection = 180f;

    /// How far above the arrow's highest point the HUD camera hangs.
    private const float HudCameraClearance = 6f;

    /// Slack around the arrow's turning circle inside the HUD frame, in world units.
    private const float HudCameraMargin = 1f;

    /// Square, because the camera rolls: an oblong frame would crop the needle differently depending
    /// on which way it happened to be pointing.
    private const int HudTextureSize = 256;

    private const float BreakMagnitude = 6f;

    /// What the HUD camera is allowed to see. Ground is deliberately absent, which is what keeps the
    /// HUD background transparent.
    private static readonly string[] HudCameraLayers = { "Default", "Actors", DestructibleLayers.Parts };

    [MenuItem("Tools/Destruction/Rebuild Battle Arrow Prefab")]
    private static void RebuildMenu() => Debug.Log(Rebuild());

    public static string Rebuild()
    {
      var decaySettings = Resources.Load<EnvironmentDecaySettings>("EnvironmentDecaySettings");
      if (decaySettings == null)
        return "EnvironmentDecaySettings asset was not found in Resources.";

      var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath);
      if (source == null)
        return $"Source arrow art was not found at {SourcePrefabPath}.";

      var root = new GameObject("BattleArrow");

      var glyph = BuildGlyph(root, source, decaySettings);
      var worldBounds = MeshBounds(glyph.transform, root.transform);
      var turningRadius = TurningRadiusOf(worldBounds);

      BuildHudCamera(root, worldBounds, turningRadius);

      var body = glyph.GetComponent<DestructibleObject>();
      var arrowObject = root.AddComponent<BattleArrowObject>();
      Apply(arrowObject, so => so.FindProperty("_body").objectReferenceValue = body);

      PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out var saved);
      var segments = root.GetComponentsInChildren<DecayPart>(true).Length;
      UnityEngine.Object.DestroyImmediate(root);
      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();

      return $"BattleArrow prefab saved={saved} path={PrefabPath} segments={segments}"
             + $" footprint={worldBounds.size.x:0.00}x{worldBounds.size.z:0.00}"
             + $" height={worldBounds.max.y:0.00} turningRadius={turningRadius:0.00}";
    }

    /// <summary>
    /// The art as one destructible group. One group rather than six: the arrow says one thing, so
    /// there is nothing for a half-smashed arrow to mean — it dies in one piece and takes the
    /// player's navigation with it. The segments are its debris.
    /// </summary>
    private static GameObject BuildGlyph(GameObject root, GameObject source, EnvironmentDecaySettings decaySettings)
    {
      var partLayer = LayerMask.NameToLayer(DestructibleLayers.Parts);
      var damagableLayer = LayerMask.NameToLayer(DestructibleLayers.Damagable);

      // A plain copy, not a nested prefab instance: the components below would otherwise be stored as
      // overrides on someone else's asset, and a restructured re-export would strand them.
      var glyph = UnityEngine.Object.Instantiate(source);
      glyph.name = "Glyph";
      glyph.transform.SetParent(root.transform, false);

      // The art's own scale is kept, and so is its authored y nudge, which sits the arrow just into
      // the ground rather than z-fighting with it.
      glyph.transform.localPosition = new Vector3(0f, source.transform.localPosition.y, 0f);
      glyph.transform.localRotation = Quaternion.Euler(0f, SourceYawCorrection, 0f);
      glyph.transform.localScale = source.transform.localScale;
      glyph.layer = damagableLayer;

      foreach (var filter in glyph.GetComponentsInChildren<MeshFilter>(true))
      {
        if (filter.sharedMesh == null)
          continue;

        var segment = filter.gameObject;
        segment.layer = partLayer;

        // Convex hull rather than a box: a chevron's bounding box is mostly empty air, and these
        // parts are metres across, so the debris would visibly rest on nothing.
        var collider = segment.AddComponent<MeshCollider>();
        collider.sharedMesh = filter.sharedMesh;
        collider.convex = true;

        segment.AddComponent<Rigidbody>().isKinematic = true;

        var size = MeshWorldSize(filter);
        var decay = segment.AddComponent<DecayPart>();
        decay.Configure(new PartDecaySettings().ForVolume(
          size.x * size.y * size.z,
          decaySettings.MaxFallSpeedMultiplier));
      }

      // Trigger volume over the whole silhouette: melee damage query + flow-map no-go footprint.
      var local = MeshBounds(glyph.transform, glyph.transform);
      var box = glyph.AddComponent<BoxCollider>();
      box.isTrigger = true;
      box.center = local.center;
      box.size = local.size;

      glyph.AddComponent<FlowMapNoGoZone>();

      var destructible = glyph.AddComponent<DestructibleObject>();
      Apply(destructible, so => so.FindProperty("_breakMagnitude").floatValue = BreakMagnitude);

      var health = glyph.AddComponent<DestructibleHealth>();
      Apply(health, so =>
        so.FindProperty("_objectType").intValue = (int)DestructibleObjectType.Arrow);

      return glyph;
    }

    /// <summary>
    /// The orthographic camera that mirrors the arrow onto the battle HUD, looking straight down.
    ///
    /// <see cref="BattleArrowObject"/> rolls it about the vertical axis every frame, so the frame is
    /// sized by the arrow's turning circle rather than its footprint: a frame that merely fits the
    /// arrow square-on would clip its corners at 45 degrees.
    ///
    /// It hangs clear of the arrow's own height and clips at the ground plane, which is what keeps
    /// the HUD background transparent — Ground is culled, and culled geometry does not occlude, so
    /// anything below has to be clipped rather than hidden.
    /// </summary>
    private static void BuildHudCamera(GameObject root, Bounds worldBounds, float turningRadius)
    {
      var height = worldBounds.max.y + HudCameraClearance;

      var cameraRoot = new GameObject("SceneHudCamera");
      cameraRoot.transform.SetParent(root.transform, false);
      cameraRoot.transform.localPosition = new Vector3(0f, height, 0f);
      cameraRoot.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

      var camera = cameraRoot.AddComponent<Camera>();
      camera.orthographic = true;
      camera.orthographicSize = turningRadius + HudCameraMargin;
      camera.cullingMask = LayerMask.GetMask(HudCameraLayers);
      camera.clearFlags = CameraClearFlags.SolidColor;
      camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
      camera.nearClipPlane = 0.1f;
      camera.farClipPlane = height;
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
        so.FindProperty("_id").intValue = (int)SceneHudElementId.Arrow;
        so.FindProperty("_resolution").vector2IntValue = new Vector2Int(HudTextureSize, HudTextureSize);
      });
    }

    /// <summary>
    /// Bounds of every mesh under <paramref name="glyph"/>, expressed in <paramref name="space"/>.
    /// Built from mesh bounds corners rather than <see cref="Renderer.bounds"/>, which is an
    /// axis-aligned world box and would inflate the result the moment anything is rotated.
    /// </summary>
    private static Bounds MeshBounds(Transform glyph, Transform space)
    {
      var toSpace = space.worldToLocalMatrix;
      var bounds = new Bounds();
      var started = false;

      foreach (var filter in glyph.GetComponentsInChildren<MeshFilter>(true))
      {
        if (filter.sharedMesh == null)
          continue;

        var meshBounds = filter.sharedMesh.bounds;
        var toWorld = filter.transform.localToWorldMatrix;

        for (var corner = 0; corner < 8; corner++)
        {
          var offset = new Vector3(
            (corner & 1) == 0 ? meshBounds.min.x : meshBounds.max.x,
            (corner & 2) == 0 ? meshBounds.min.y : meshBounds.max.y,
            (corner & 4) == 0 ? meshBounds.min.z : meshBounds.max.z);

          var point = toSpace.MultiplyPoint3x4(toWorld.MultiplyPoint3x4(offset));
          if (started)
          {
            bounds.Encapsulate(point);
          }
          else
          {
            bounds = new Bounds(point, Vector3.zero);
            started = true;
          }
        }
      }

      return bounds;
    }

    /// Distance from the root's vertical axis to the furthest corner of the art: the radius the HUD
    /// frame has to cover for the arrow to stay whole through a full turn.
    private static float TurningRadiusOf(Bounds bounds)
    {
      var x = Mathf.Max(Mathf.Abs(bounds.min.x), Mathf.Abs(bounds.max.x));
      var z = Mathf.Max(Mathf.Abs(bounds.min.z), Mathf.Abs(bounds.max.z));
      return Mathf.Sqrt(x * x + z * z);
    }

    /// A segment's mesh bounds in world units, which is what the decay speed is derived from.
    private static Vector3 MeshWorldSize(MeshFilter filter)
    {
      var size = filter.sharedMesh.bounds.size;
      var scale = filter.transform.lossyScale;
      return new Vector3(
        Mathf.Abs(size.x * scale.x),
        Mathf.Abs(size.y * scale.y),
        Mathf.Abs(size.z * scale.z));
    }

    private static void Apply(UnityEngine.Object target, System.Action<SerializedObject> apply)
    {
      var serialized = new SerializedObject(target);
      serialized.Update();
      apply(serialized);
      serialized.ApplyModifiedPropertiesWithoutUndo();
    }
  }
}
