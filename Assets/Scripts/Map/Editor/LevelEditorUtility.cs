using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Map.Editor
{
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

    public static void ApplyCellsToMapData(MapData mapData, IEnumerable<Vector2Int> cells, bool replaceExisting)
    {
      Undo.RecordObject(mapData, replaceExisting ? "Replace Map Cells From Material" : "Fill Map Cells From Material");

      if (replaceExisting)
      {
        foreach (var cell in new List<Vector2Int>(mapData.FilledCells))
          mapData.SetFilled(cell, false);
      }

      foreach (var cell in cells)
        mapData.SetFilled(cell, true);

      EditorUtility.SetDirty(mapData);
    }

    public static MapData CreateMapDataNextToScene(Scene scene)
    {
      var scenePath = scene.path;
      if (string.IsNullOrEmpty(scenePath))
      {
        Debug.LogWarning("Save the scene before creating a MapData asset.");
        return null;
      }

      var sceneDir = Path.GetDirectoryName(scenePath)?.Replace('\\', '/');
      var sceneName = Path.GetFileNameWithoutExtension(scenePath);
      var folder = $"{sceneDir}/{sceneName}";

      if (!AssetDatabase.IsValidFolder(folder))
        AssetDatabase.CreateFolder(sceneDir, sceneName);

      var assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/MapData.asset");
      var mapData = ScriptableObject.CreateInstance<MapData>();
      AssetDatabase.CreateAsset(mapData, assetPath);
      AssetDatabase.SaveAssets();
      return mapData;
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
