using System.IO;
using UnityEditor;
using UnityEngine;

namespace Map.Editor
{
  [CustomEditor(typeof(MapPainter))]
  public class MapPainterEditor : UnityEditor.Editor
  {
    private static readonly Plane GroundPlane = new(Vector3.up, Vector3.zero);

    private bool _erasing;

    public override void OnInspectorGUI()
    {
      DrawDefaultInspector();

      var painter = (MapPainter)target;

      EditorGUILayout.Space();

      if (painter.MapData == null)
      {
        EditorGUILayout.HelpBox("No MapData assigned.", MessageType.Info);
        if (GUILayout.Button("Create Map Data"))
          CreateMapData(painter);
        return;
      }

      var label = painter.isEditing ? "Stop Drawing" : "Start Drawing";
      if (GUILayout.Button(label))
      {
        painter.isEditing = !painter.isEditing;
        SceneView.RepaintAll();
      }

      if (painter.isEditing)
        EditorGUILayout.HelpBox("Click + drag in the Scene view to fill cells. Hold Shift to erase.", MessageType.None);
    }

    private void CreateMapData(MapPainter painter)
    {
      var scenePath = painter.gameObject.scene.path;
      if (string.IsNullOrEmpty(scenePath))
      {
        Debug.LogWarning("Save the scene before creating a MapData asset.", painter);
        return;
      }

      var sceneDir = Path.GetDirectoryName(scenePath)?.Replace('\\', '/');
      var sceneName = Path.GetFileNameWithoutExtension(scenePath);
      var folder = $"{sceneDir}/{sceneName}";

      if (!AssetDatabase.IsValidFolder(folder))
        AssetDatabase.CreateFolder(sceneDir, sceneName);

      var assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/MapData.asset");
      var mapData = CreateInstance<MapData>();
      AssetDatabase.CreateAsset(mapData, assetPath);
      AssetDatabase.SaveAssets();

      serializedObject.Update();
      serializedObject.FindProperty("mapData").objectReferenceValue = mapData;
      serializedObject.ApplyModifiedProperties();
    }

    private void OnSceneGUI()
    {
      var painter = (MapPainter)target;
      if (!painter.isEditing || painter.MapData == null)
        return;

      var controlId = GUIUtility.GetControlID(FocusType.Passive);
      var evt = Event.current;

      switch (evt.GetTypeForControl(controlId))
      {
        case EventType.Layout:
          HandleUtility.AddDefaultControl(controlId);
          break;

        case EventType.MouseDown when evt.button == 0:
          _erasing = evt.shift;
          PaintAtMouse(painter, evt.mousePosition);
          evt.Use();
          break;

        case EventType.MouseDrag when evt.button == 0:
          PaintAtMouse(painter, evt.mousePosition);
          evt.Use();
          break;

        case EventType.MouseUp when evt.button == 0:
          evt.Use();
          break;
      }
    }

    private void PaintAtMouse(MapPainter painter, Vector2 mousePosition)
    {
      var ray = HandleUtility.GUIPointToWorldRay(mousePosition);
      if (!GroundPlane.Raycast(ray, out var distance))
        return;

      var hit = ray.GetPoint(distance);
      var cellSize = painter.MapData.CellSize;
      var cell = new Vector2Int(
        Mathf.FloorToInt(hit.x / cellSize),
        Mathf.FloorToInt(hit.z / cellSize));

      if (painter.MapData.IsFilled(cell) == !_erasing)
        return;

      Undo.RecordObject(painter.MapData, _erasing ? "Erase Map Cell" : "Fill Map Cell");
      painter.MapData.SetFilled(cell, !_erasing);
      EditorUtility.SetDirty(painter.MapData);
      SceneView.RepaintAll();
    }
  }
}
