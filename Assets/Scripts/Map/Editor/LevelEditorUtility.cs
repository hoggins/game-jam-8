using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Map.Editor
{
  public enum MapPaintZone
  {
    House,
    Road,
    Sidewalk
  }

  public static class LevelEditorUtility
  {
    private const string ConstantHouseSetPath = "Assets/Resources/Map/HouseSet.asset";

    /// <summary>
    /// Cells whose footprint is covered by the given material submesh, using the same
    /// world-to-cell convention as the paint tool (cell = floor(world / cellSize)).
    /// Each triangle's centroid (not its vertices) decides the cell, so it lands cleanly
    /// inside one cell even if the object was shifted off a perfect grid alignment.
    /// </summary>
    public static HashSet<Vector2Int> ComputeCellsForMaterialSlot(GameObject source, int materialSlot, int cellSize)
    {
      var cells = new HashSet<Vector2Int>();

      var meshFilter = source.GetComponent<MeshFilter>();
      var mesh = meshFilter != null ? meshFilter.sharedMesh : null;
      if (mesh == null || materialSlot < 0 || materialSlot >= mesh.subMeshCount)
        return cells;

      var vertices = mesh.vertices;
      var triangles = mesh.GetTriangles(materialSlot);
      var t = source.transform;

      for (var i = 0; i < triangles.Length; i += 3)
      {
        var a = t.TransformPoint(vertices[triangles[i]]);
        var b = t.TransformPoint(vertices[triangles[i + 1]]);
        var c = t.TransformPoint(vertices[triangles[i + 2]]);

        var centroidX = (a.x + b.x + c.x) / 3f;
        var centroidZ = (a.z + b.z + c.z) / 3f;

        var cell = new Vector2Int(
          Mathf.FloorToInt(centroidX / cellSize),
          Mathf.FloorToInt(centroidZ / cellSize));
        cells.Add(cell);
      }

      return cells;
    }

    // Source meshes come out of the DCC tool with generic "Material.NNN" names (Blender's
    // default when no one bothered to rename them), so the zone is keyed off that trailing
    // number rather than any descriptive text: .001 -> house, .002 -> road, .004 -> sidewalk.
    private static readonly Dictionary<int, MapPaintZone> ZoneByMaterialNumber = new()
    {
      { 1, MapPaintZone.House },
      { 2, MapPaintZone.Road },
      { 4, MapPaintZone.Sidewalk },
    };

    /// <summary>
    /// Detects a material slot's zone either from a descriptive name (containing "house",
    /// "road" or "sidewalk") or, failing that, from a trailing number like "Material.001"
    /// via <see cref="ZoneByMaterialNumber"/>.
    /// </summary>
    public static bool TryDetectZone(string materialName, out MapPaintZone zone)
    {
      if (!string.IsNullOrEmpty(materialName))
      {
        var name = materialName.ToLowerInvariant();

        if (name.Contains("sidewalk"))
        {
          zone = MapPaintZone.Sidewalk;
          return true;
        }

        if (name.Contains("road"))
        {
          zone = MapPaintZone.Road;
          return true;
        }

        if (name.Contains("house"))
        {
          zone = MapPaintZone.House;
          return true;
        }

        var match = Regex.Match(materialName, @"(\d+)\s*$");
        if (match.Success &&
            int.TryParse(match.Value, out var number) &&
            ZoneByMaterialNumber.TryGetValue(number, out zone))
          return true;
      }

      zone = default;
      return false;
    }

    /// <summary>
    /// Reads every material slot on the source mesh and buckets its cells under whichever
    /// zone its material name identifies, so all layers can be imported in a single pass.
    /// Slots whose material name matches no known zone are reported via unmatchedSlots
    /// instead of being silently dropped.
    /// </summary>
    public static Dictionary<MapPaintZone, HashSet<Vector2Int>> ComputeCellsForAllMaterialSlots(
      GameObject source, int cellSize, out List<string> unmatchedSlots)
    {
      var result = new Dictionary<MapPaintZone, HashSet<Vector2Int>>();
      unmatchedSlots = new List<string>();

      var renderer = source != null ? source.GetComponent<MeshRenderer>() : null;
      var meshFilter = source != null ? source.GetComponent<MeshFilter>() : null;
      var mesh = meshFilter != null ? meshFilter.sharedMesh : null;
      if (renderer == null || mesh == null)
        return result;

      var materials = renderer.sharedMaterials;
      var slotCount = Mathf.Min(materials.Length, mesh.subMeshCount);

      for (var slot = 0; slot < slotCount; slot++)
      {
        var materialName = materials[slot] != null ? materials[slot].name : null;

        if (!TryDetectZone(materialName, out var zone))
        {
          unmatchedSlots.Add($"Element {slot}: {materialName ?? "None"}");
          continue;
        }

        var cells = ComputeCellsForMaterialSlot(source, slot, cellSize);
        if (cells.Count == 0)
          continue;

        if (!result.TryGetValue(zone, out var zoneCells))
          result[zone] = zoneCells = new HashSet<Vector2Int>();

        zoneCells.UnionWith(cells);
      }

      return result;
    }

    public static void ApplyCellsToZone(
      MapData mapData, MapPaintZone zone, IEnumerable<Vector2Int> cells, bool replaceExisting)
    {
      Undo.RecordObject(mapData, replaceExisting ? $"Replace {zone} Cells From Material" : $"Fill {zone} Cells From Material");

      if (replaceExisting)
        switch (zone)
        {
          case MapPaintZone.House:
            foreach (var cell in new List<Vector2Int>(mapData.FilledCells))
              mapData.SetFilled(cell, false);
            break;

          case MapPaintZone.Road:
            foreach (var road in new List<RoadCellData>(mapData.RoadCells))
              mapData.SetRoad(road.cell, false);
            break;

          case MapPaintZone.Sidewalk:
            foreach (var cell in new List<Vector2Int>(mapData.SidewalkCells))
              mapData.SetSidewalk(cell, false);
            break;
        }

      foreach (var cell in cells)
        switch (zone)
        {
          case MapPaintZone.House:
            mapData.SetFilled(cell, true);
            break;

          case MapPaintZone.Road:
            mapData.SetRoad(cell, true);
            break;

          case MapPaintZone.Sidewalk:
            mapData.SetSidewalk(cell, true);
            break;
        }

      EditorUtility.SetDirty(mapData);
    }

    private static string GetOrCreateSceneFolder(Scene scene)
    {
      var scenePath = scene.path;
      if (string.IsNullOrEmpty(scenePath))
      {
        Debug.LogWarning("Save the scene before creating assets next to it.");
        return null;
      }

      var sceneDir = Path.GetDirectoryName(scenePath)?.Replace('\\', '/');
      var sceneName = Path.GetFileNameWithoutExtension(scenePath);
      var folder = $"{sceneDir}/{sceneName}";

      if (!AssetDatabase.IsValidFolder(folder))
        AssetDatabase.CreateFolder(sceneDir, sceneName);

      return folder;
    }

    public static MapData CreateMapDataNextToScene(Scene scene)
    {
      var folder = GetOrCreateSceneFolder(scene);
      if (folder == null)
        return null;

      var assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/MapData.asset");
      var mapData = ScriptableObject.CreateInstance<MapData>();
      AssetDatabase.CreateAsset(mapData, assetPath);
      AssetDatabase.SaveAssets();
      return mapData;
    }

    /// <summary>
    /// Saves the given scene GameObject as a prefab next to the scene and reconnects the scene
    /// copy to it, so the level's authored data (set references, seed, etc.) lives in an asset
    /// instead of the scene file - keeping scene diffs small and merge-friendly like any other
    /// prefab instance, instead of a raw GameObject whose fields conflict line-by-line.
    /// </summary>
    public static GameObject CreateLevelPrefabNextToScene(GameObject target, Scene scene)
    {
      var folder = GetOrCreateSceneFolder(scene);
      if (folder == null)
        return target;

      var assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/Level.prefab");
      return PrefabUtility.SaveAsPrefabAssetAndConnect(target, assetPath, InteractionMode.UserAction);
    }

    public static HouseSet GetOrCreateConstantHouseSet()
    {
      var houseSet = AssetDatabase.LoadAssetAtPath<HouseSet>(ConstantHouseSetPath);
      if (houseSet != null)
        return houseSet;

      if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        AssetDatabase.CreateFolder("Assets", "Resources");
      if (!AssetDatabase.IsValidFolder("Assets/Resources/Map"))
        AssetDatabase.CreateFolder("Assets/Resources", "Map");

      houseSet = ScriptableObject.CreateInstance<HouseSet>();
      AssetDatabase.CreateAsset(houseSet, ConstantHouseSetPath);
      AssetDatabase.SaveAssets();
      return houseSet;
    }
  }
}
