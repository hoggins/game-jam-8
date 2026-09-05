using Destruction;
using Movement;
using SceneHud;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Health.Editor
{
  /// <summary>
  /// Generates the BattleHealth prefab: 24 pixel boxes in two rows of twelve, as one destructible
  /// group, plus the HUD camera that mirrors them onto the battle HUD.
  ///
  /// Same pixel size and pitch as the battle timer, deliberately — the two objects are speaking the
  /// same visual language, and a health bar built from different-sized blocks would read as a
  /// different kind of thing. The face lies flat pointing at the sky so it reads from the top-down
  /// battle camera: columns run along +X, rows along +Z.
  ///
  /// Rerun from the menu after changing the constants below.
  /// </summary>
  public static class BattleHealthPrefabBuilder
  {
    private const string PrefabPath = "Assets/Resources/Descructable/BattleHealth.prefab";
    private const string MaterialPath = "Assets/Resources/Material/Generic/GenericMaterial01.mat";

    private const float PixelSize = 0.9f;
    private const float Pitch = 0.95f;

    /// How high above the face the HUD camera hangs. Doubles as its far clip distance, putting the
    /// far plane exactly on the ground plane — the camera culls Ground, and culled geometry does not
    /// occlude, so anything below the face has to be clipped rather than hidden.
    private const float HudCameraHeight = 10f;

    /// Slack around the bar inside the HUD frame, in world units.
    private const float HudCameraMargin = 0.5f;

    /// Vertical resolution of the HUD render texture; the horizontal one follows the framing. The
    /// bar is only two pixels deep, so it needs far less height than the timer.
    private const int HudTextureHeight = 128;

    private const float BreakMagnitude = 6f;

    /// What the HUD camera is allowed to see. Ground is deliberately absent, which is what keeps the
    /// HUD background transparent.
    private static readonly string[] HudCameraLayers = { "Default", "Actors", DestructibleLayers.Parts };

    [MenuItem("Tools/Destruction/Rebuild Battle Health Prefab")]
    private static void RebuildMenu() => Debug.Log(Rebuild());

    public static string Rebuild()
    {
      var decaySettings = Resources.Load<EnvironmentDecaySettings>("EnvironmentDecaySettings");
      if (decaySettings == null)
        return "EnvironmentDecaySettings asset was not found in Resources.";

      var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
      var partLayer = LayerMask.NameToLayer(DestructibleLayers.Parts);
      var damagableLayer = LayerMask.NameToLayer(DestructibleLayers.Damagable);

      var root = new GameObject("BattleHealth");

      var body = BuildBar(root, material, partLayer, damagableLayer, decaySettings);
      BuildHudCamera(root);

      var healthObject = root.AddComponent<BattleHealthObject>();
      Apply(healthObject, so => so.FindProperty("_body").objectReferenceValue = body);

      PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out var saved);
      UnityEngine.Object.DestroyImmediate(root);
      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();

      return $"BattleHealth prefab saved={saved} path={PrefabPath} pixels={BattleHealthBar.PixelCount}"
             + $" footprint={Width():0.00}x{Depth():0.00}";
    }

    /// <summary>
    /// The bar as a single destructible group. One group rather than 24: the pixels are knocked off
    /// individually by <see cref="BattleHealthBar"/> through
    /// <see cref="DestructibleObject.FallOutPart"/>, which is what keeps them emptying from one end,
    /// and giving each pixel its own group would put 24 trigger volumes and 24 flow-map no-go zones
    /// in the street to achieve the same thing.
    /// </summary>
    private static DestructibleObject BuildBar(
      GameObject root,
      Material material,
      int partLayer,
      int damagableLayer,
      EnvironmentDecaySettings decaySettings)
    {
      var barRoot = new GameObject("Bar");
      barRoot.transform.SetParent(root.transform, false);
      barRoot.layer = damagableLayer;

      // Column-major, so index order runs along the bar: the last index is the far end, which is the
      // end BattleHealthBar empties from.
      var pixels = new DecayPart[BattleHealthBar.PixelCount];
      for (var column = 0; column < BattleHealthBar.Columns; column++)
      for (var row = 0; row < BattleHealthBar.Rows; row++)
      {
        var pixel = CreatePixel($"Pixel_{column}_{row}", material, partLayer, decaySettings);
        pixel.transform.SetParent(barRoot.transform, false);
        pixel.transform.localPosition = new Vector3(ColumnX(column), PixelSize * 0.5f, RowZ(row));
        pixels[column * BattleHealthBar.Rows + row] = pixel.GetComponent<DecayPart>();
      }

      // Trigger volume over the whole bar: melee damage query + flow-map no-go footprint. It stays
      // the full length even as the bar empties, so the player can keep hitting what is left of it
      // from where he was already standing.
      var box = barRoot.AddComponent<BoxCollider>();
      box.isTrigger = true;
      box.center = new Vector3(0f, PixelSize * 0.5f, 0f);
      box.size = new Vector3(Width(), PixelSize, Depth());

      barRoot.AddComponent<FlowMapNoGoZone>();

      var destructible = barRoot.AddComponent<DestructibleObject>();
      Apply(destructible, so => so.FindProperty("_breakMagnitude").floatValue = BreakMagnitude);

      // No DestructibleHealth: BattleHealthBar is the IImpactDamageable here, because the bar's hit
      // points have to turn into the player's lost health rather than into its own destruction.
      var bar = barRoot.AddComponent<BattleHealthBar>();
      Apply(bar, so =>
      {
        var array = so.FindProperty("_pixels");
        array.arraySize = pixels.Length;
        for (var i = 0; i < pixels.Length; i++)
          array.GetArrayElementAtIndex(i).objectReferenceValue = pixels[i];
      });

      return destructible;
    }

    /// <summary>
    /// The orthographic camera that mirrors the bar onto the battle HUD, looking straight down with
    /// its up vector along +Z so the rows read the way they lie. Unlike the arrow's, this camera
    /// never rolls, so the frame is a plain oblong around the bar and the render texture matches that
    /// aspect — mismatch it and the HUD image stretches.
    /// </summary>
    private static void BuildHudCamera(GameObject root)
    {
      var width = Width() + HudCameraMargin;
      var depth = Depth() + HudCameraMargin;

      var cameraRoot = new GameObject("SceneHudCamera");
      cameraRoot.transform.SetParent(root.transform, false);
      cameraRoot.transform.localPosition = new Vector3(0f, HudCameraHeight, 0f);
      cameraRoot.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

      var camera = cameraRoot.AddComponent<Camera>();
      camera.orthographic = true;
      camera.orthographicSize = depth * 0.5f;
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
        so.FindProperty("_id").intValue = (int)SceneHudElementId.Hp;
        so.FindProperty("_resolution").vector2IntValue = new Vector2Int(
          Mathf.RoundToInt(HudTextureHeight * width / depth),
          HudTextureHeight);
      });
    }

    private static GameObject CreatePixel(
      string name,
      Material material,
      int layer,
      EnvironmentDecaySettings decaySettings)
    {
      var pixel = GameObject.CreatePrimitive(PrimitiveType.Cube);
      pixel.name = name;
      pixel.layer = layer;
      pixel.transform.localScale = Vector3.one * PixelSize;

      if (material != null)
        pixel.GetComponent<MeshRenderer>().sharedMaterial = material;

      pixel.AddComponent<Rigidbody>().isKinematic = true;

      var decay = pixel.AddComponent<DecayPart>();
      decay.Configure(new PartDecaySettings().ForVolume(
        PixelSize * PixelSize * PixelSize,
        decaySettings.MaxFallSpeedMultiplier));

      return pixel;
    }

    private static float Width() => (BattleHealthBar.Columns - 1) * Pitch + PixelSize;

    private static float Depth() => (BattleHealthBar.Rows - 1) * Pitch + PixelSize;

    private static float ColumnX(int column) => (column - (BattleHealthBar.Columns - 1) * 0.5f) * Pitch;

    private static float RowZ(int row) => ((BattleHealthBar.Rows - 1) * 0.5f - row) * Pitch;

    private static void Apply(UnityEngine.Object target, System.Action<SerializedObject> apply)
    {
      var serialized = new SerializedObject(target);
      serialized.Update();
      apply(serialized);
      serialized.ApplyModifiedPropertiesWithoutUndo();
    }
  }
}
