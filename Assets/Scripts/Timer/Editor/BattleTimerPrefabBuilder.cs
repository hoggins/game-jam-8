using Destruction;
using Movement;
using SceneHud;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Timer.Editor
{
  /// <summary>
  /// Generates the BattleTimer prefab: four 3x5 destructible digits plus a destructible colon
  /// divider, 62 boxes in all. The face lies flat on the ground pointing at the sky, so it reads from the top-down battle
  /// camera: glyph columns run along +X, glyph rows along +Z (row 0 furthest away), and unlit pixels
  /// sink straight down through the ground. Rerun from the menu after changing the constants below.
  /// </summary>
  public static class BattleTimerPrefabBuilder
  {
    private const string PrefabPath = "Assets/Resources/Descructable/BattleTimer.prefab";
    private const string MaterialPath = "Assets/Resources/Material/Generic/GenericMaterial01.mat";

    private const float PixelSize = 0.9f;
    private const float Pitch = 0.95f;

    // 15 columns: [d0 0-2] [gap 3] [d1 4-6] [colon 7] [d2 8-10] [gap 11] [d3 12-14]
    private const int ColumnCount = 15;
    private const int ColonColumn = 7;
    private static readonly int[] DigitStartColumns = { 0, 4, 8, 12 };

    /// Glyph rows the two colon dots sit on, top first.
    private static readonly int[] DividerRows = { 1, 3 };

    /// How high above the face the HUD camera hangs. Doubles as its far clip distance, which puts
    /// the far plane exactly on the ground plane: lit pixels are fully in frame and the unlit ones
    /// parked underground are clipped away. They would otherwise show through, because the camera
    /// culls the ground and culled geometry does not occlude.
    private const float HudCameraHeight = 10f;

    /// Slack around the face inside the HUD frame, in world units.
    private const float HudCameraMargin = 0.5f;

    /// Vertical resolution of the HUD render texture; the horizontal one follows the framing.
    private const int HudTextureHeight = 256;

    /// What the HUD camera is allowed to see. The timer's own pixels are destructible parts; actors
    /// and the default layer bring the player, the mobs and their effects in with them. Ground is
    /// deliberately absent, which is what keeps the HUD background transparent.
    private static readonly string[] HudCameraLayers = { "Default", "Actors", DestructibleLayers.Parts };

    [MenuItem("Tools/Destruction/Rebuild Battle Timer Prefab")]
    private static void RebuildMenu() => Debug.Log(Rebuild());

    public static string Rebuild()
    {
      var decaySettings = Resources.Load<EnvironmentDecaySettings>("EnvironmentDecaySettings");
      if (decaySettings == null)
        return "EnvironmentDecaySettings asset was not found in Resources.";

      var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
      var partLayer = LayerMask.NameToLayer(DestructibleLayers.Parts);
      var damagableLayer = LayerMask.NameToLayer(DestructibleLayers.Damagable);

      var root = new GameObject("BattleTimer");
      var digits = new TimerDigit[DigitStartColumns.Length];

      for (var d = 0; d < DigitStartColumns.Length; d++)
        digits[d] = BuildDigit(root, d, material, partLayer, damagableLayer, decaySettings);

      BuildDivider(root, material, partLayer, damagableLayer, decaySettings);

      BuildHudCamera(root);

      var timerObject = root.AddComponent<BattleTimerObject>();
      Apply(timerObject, so =>
      {
        var array = so.FindProperty("_digits");
        array.arraySize = digits.Length;
        for (var i = 0; i < digits.Length; i++)
          array.GetArrayElementAtIndex(i).objectReferenceValue = digits[i];
      });

      PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out var saved);
      UnityEngine.Object.DestroyImmediate(root);
      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();

      var width = (ColumnCount - 1) * Pitch + PixelSize;
      var depth = (TimerDigit.Rows - 1) * Pitch + PixelSize;
      return $"BattleTimer prefab saved={saved} path={PrefabPath} footprint={width:0.00}x{depth:0.00}";
    }

    private static TimerDigit BuildDigit(
      GameObject root,
      int digitIndex,
      Material material,
      int partLayer,
      int damagableLayer,
      EnvironmentDecaySettings decaySettings)
    {
      var centerColumn = DigitStartColumns[digitIndex] + 1;

      var digitRoot = new GameObject($"Digit{digitIndex}");
      digitRoot.transform.SetParent(root.transform, false);
      digitRoot.transform.localPosition = new Vector3(ColumnX(centerColumn), 0f, 0f);
      digitRoot.layer = damagableLayer;

      var pixels = new Transform[TimerDigit.PixelCount];
      for (var row = 0; row < TimerDigit.Rows; row++)
      for (var column = 0; column < TimerDigit.Columns; column++)
      {
        var pixel = CreatePixel($"Pixel_{row}_{column}", material, partLayer, decaySettings);
        pixel.transform.SetParent(digitRoot.transform, false);
        pixel.transform.localPosition = new Vector3((column - 1) * Pitch, PixelSize * 0.5f, RowZ(row));
        pixels[row * TimerDigit.Columns + column] = pixel.transform;
      }

      // Trigger volume covering the lit 3x5 block: melee damage query + flow-map no-go footprint.
      // Lying flat, the footprint is the whole glyph area rather than a thin edge-on slab.
      var box = digitRoot.AddComponent<BoxCollider>();
      box.isTrigger = true;
      box.center = new Vector3(0f, PixelSize * 0.5f, 0f);
      box.size = new Vector3(
        (TimerDigit.Columns - 1) * Pitch + PixelSize,
        PixelSize,
        (TimerDigit.Rows - 1) * Pitch + PixelSize);

      digitRoot.AddComponent<FlowMapNoGoZone>();

      var destructible = digitRoot.AddComponent<DestructibleObject>();
      Apply(destructible, so => so.FindProperty("_breakMagnitude").floatValue = 6f);

      // Health comes from BattleBalance via the type; TimerDigit also keeps digits out of the
      // buildings-destroyed statistic.
      var health = digitRoot.AddComponent<DestructibleHealth>();
      Apply(health, so =>
        so.FindProperty("_objectType").intValue = (int)DestructibleObjectType.TimerDigit);

      var digit = digitRoot.AddComponent<TimerDigit>();
      Apply(digit, so =>
      {
        var array = so.FindProperty("_pixels");
        array.arraySize = TimerDigit.PixelCount;
        for (var i = 0; i < TimerDigit.PixelCount; i++)
          array.GetArrayElementAtIndex(i).objectReferenceValue = pixels[i];

        so.FindProperty("_sinkDepth").floatValue = PixelSize * 2f;
        so.FindProperty("_moveSpeed").floatValue = 12f;
      });

      return digit;
    }

    /// The colon is its own destructible unit: breakable like a digit, but carrying no place value,
    /// so smashing it never changes the remaining time. Both dots are always lit, so unlike a digit
    /// it never has pixels parked underground.
    private static void BuildDivider(
      GameObject root,
      Material material,
      int partLayer,
      int damagableLayer,
      EnvironmentDecaySettings decaySettings)
    {
      var dividerRoot = new GameObject("Divider");
      dividerRoot.transform.SetParent(root.transform, false);
      dividerRoot.transform.localPosition = new Vector3(ColumnX(ColonColumn), 0f, 0f);
      dividerRoot.layer = damagableLayer;

      foreach (var row in DividerRows)
      {
        var dot = CreatePixel($"Dot_{row}", material, partLayer, decaySettings);
        dot.transform.SetParent(dividerRoot.transform, false);
        dot.transform.localPosition = new Vector3(0f, PixelSize * 0.5f, RowZ(row));
      }

      var box = dividerRoot.AddComponent<BoxCollider>();
      box.isTrigger = true;
      box.center = new Vector3(0f, PixelSize * 0.5f, 0f);
      box.size = new Vector3(
        PixelSize,
        PixelSize,
        RowZ(DividerRows[0]) - RowZ(DividerRows[DividerRows.Length - 1]) + PixelSize);

      dividerRoot.AddComponent<FlowMapNoGoZone>();

      var destructible = dividerRoot.AddComponent<DestructibleObject>();
      Apply(destructible, so => so.FindProperty("_breakMagnitude").floatValue = 6f);

      var health = dividerRoot.AddComponent<DestructibleHealth>();
      Apply(health, so =>
        so.FindProperty("_objectType").intValue = (int)DestructibleObjectType.TimerDivider);
    }

    /// <summary>
    /// The orthographic camera that mirrors the face onto the battle HUD, looking straight down with
    /// its up vector along +Z so the glyph rows read top to bottom. The frame is only a hair wider
    /// than the timer itself, so culling to the layers below picks up the digits plus whoever is
    /// standing on them — fighting on top of the clock reads on the HUD.
    /// </summary>
    private static void BuildHudCamera(GameObject root)
    {
      var width = (ColumnCount - 1) * Pitch + PixelSize + HudCameraMargin;
      var depth = (TimerDigit.Rows - 1) * Pitch + PixelSize + HudCameraMargin;

      var cameraRoot = new GameObject("SceneHudCamera");
      cameraRoot.transform.SetParent(root.transform, false);
      cameraRoot.transform.localPosition = new Vector3(0f, HudCameraHeight, 0f);
      cameraRoot.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

      var camera = cameraRoot.AddComponent<Camera>();
      camera.orthographic = true;
      // The render texture's aspect drives the horizontal framing, so half the depth is the whole
      // of the framing decision here; the texture below is sized to match.
      camera.orthographicSize = depth * 0.5f;
      camera.cullingMask = LayerMask.GetMask(HudCameraLayers);
      camera.clearFlags = CameraClearFlags.SolidColor;
      camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
      camera.nearClipPlane = 0.1f;
      camera.farClipPlane = HudCameraHeight;
      camera.useOcclusionCulling = false;
      camera.allowHDR = false;
      camera.allowMSAA = false;

      // URP would add this with defaults on first render anyway; pinning it here keeps the HUD pass
      // a plain unlit base render instead of inheriting whatever the project defaults become.
      var cameraData = cameraRoot.AddComponent<UniversalAdditionalCameraData>();
      cameraData.renderType = CameraRenderType.Base;
      cameraData.renderPostProcessing = false;
      cameraData.renderShadows = false;
      cameraData.requiresColorOption = CameraOverrideOption.Off;
      cameraData.requiresDepthOption = CameraOverrideOption.Off;

      var element = cameraRoot.AddComponent<SceneHudElement>();
      Apply(element, so =>
      {
        so.FindProperty("_id").intValue = (int)SceneHudElementId.BattleTimer;

        var resolution = so.FindProperty("_resolution");
        resolution.vector2IntValue = new Vector2Int(
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

    private static float ColumnX(int column) => (column - ColonColumn) * Pitch;

    /// Row 0 is the top of the glyph, placed at the largest +Z so it reads correctly from the
    /// top-down camera (whose up vector points along +Z). Centred on the digit origin.
    private static float RowZ(int row) => ((TimerDigit.Rows - 1) * 0.5f - row) * Pitch;

    private static void Apply(UnityEngine.Object target, System.Action<SerializedObject> apply)
    {
      var serialized = new SerializedObject(target);
      serialized.Update();
      apply(serialized);
      serialized.ApplyModifiedPropertiesWithoutUndo();
    }
  }
}
