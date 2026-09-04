using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Map.Editor
{
  public static class LevelEditorUtility
  {
    private const string ConstantHouseSetPath = "Assets/Resources/Map/HouseSet.asset";

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
