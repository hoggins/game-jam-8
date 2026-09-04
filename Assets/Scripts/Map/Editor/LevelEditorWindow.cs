using UnityEditor;
using UnityEngine;

namespace Map.Editor
{
  public class LevelEditorWindow : EditorWindow
  {
    private static readonly Plane GroundPlane = new(Vector3.up, Vector3.zero);

    private LevelData _levelData;
    private bool _erasing;

    [MenuItem("Tools/Map/Level Editor")]
    private static void Open() => GetWindow<LevelEditorWindow>("Level Editor");

    private void OnEnable()
    {
      _levelData = FindFirstObjectByType<LevelData>();
      SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
      SceneView.duringSceneGui -= OnSceneGUI;

      if (_levelData != null)
        _levelData.isEditing = false;
    }

    private void OnGUI()
    {
      if (_levelData == null)
        _levelData = FindFirstObjectByType<LevelData>();

      if (_levelData == null)
      {
        EditorGUILayout.HelpBox("This scene has no level yet.", MessageType.Info);
        if (GUILayout.Button("Make As Level"))
          MakeAsLevel();
        return;
      }

      var so = new SerializedObject(_levelData);
      so.Update();

      DrawObjectField(so.FindProperty("mapData"), "Map Data", typeof(MapData));
      DrawObjectField(so.FindProperty("houseSet"), "House Set", typeof(HouseSet));
      EditorGUILayout.PropertyField(so.FindProperty("seed"));
      EditorGUILayout.PropertyField(so.FindProperty("gridExtent"));
      EditorGUILayout.PropertyField(so.FindProperty("showGrid"));

      so.ApplyModifiedProperties();

      EditorGUILayout.Space();

      if (_levelData.MapData == null)
      {
        EditorGUILayout.HelpBox("No MapData assigned.", MessageType.Info);
        if (GUILayout.Button("Create Map Data"))
          CreateMapData();
        return;
      }

      var label = _levelData.isEditing ? "Stop Drawing" : "Start Drawing";
      if (GUILayout.Button(label))
      {
        _levelData.isEditing = !_levelData.isEditing;
        SceneView.RepaintAll();
      }

      if (_levelData.isEditing)
        EditorGUILayout.HelpBox("Click + drag in the Scene view to fill cells. Hold Shift to erase.", MessageType.None);

      EditorGUILayout.Space();

      using (new EditorGUI.DisabledScope(_levelData.HouseSet == null))
      {
        if (GUILayout.Button("Simulate Placement"))
          _levelData.Fill();
      }

      if (GUILayout.Button("Clear Simulation"))
        _levelData.Clear();
    }

    private static void DrawObjectField(SerializedProperty property, string label, System.Type type)
    {
      EditorGUI.BeginChangeCheck();
      var value = EditorGUILayout.ObjectField(label, property.objectReferenceValue, type, false);
      if (EditorGUI.EndChangeCheck())
        property.objectReferenceValue = value;
    }

    private void MakeAsLevel()
    {
      var target = Selection.activeGameObject;
      if (target == null)
      {
        target = new GameObject("Level");
        Undo.RegisterCreatedObjectUndo(target, "Make As Level");
      }

      var levelData = target.GetComponent<LevelData>();
      if (levelData == null)
        levelData = Undo.AddComponent<LevelData>(target);

      var so = new SerializedObject(levelData);

      var mapDataProp = so.FindProperty("mapData");
      if (mapDataProp.objectReferenceValue == null)
        mapDataProp.objectReferenceValue = LevelEditorUtility.CreateMapDataNextToScene(target.scene);

      var houseSetProp = so.FindProperty("houseSet");
      if (houseSetProp.objectReferenceValue == null)
        houseSetProp.objectReferenceValue = LevelEditorUtility.GetOrCreateConstantHouseSet();

      so.ApplyModifiedProperties();

      Selection.activeGameObject = target;
      _levelData = levelData;
    }

    private void CreateMapData()
    {
      var mapData = LevelEditorUtility.CreateMapDataNextToScene(_levelData.gameObject.scene);
      if (mapData == null)
        return;

      var so = new SerializedObject(_levelData);
      so.FindProperty("mapData").objectReferenceValue = mapData;
      so.ApplyModifiedProperties();
    }

    private void OnSceneGUI(SceneView sceneView)
    {
      if (_levelData == null || !_levelData.isEditing || _levelData.MapData == null)
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
          PaintAtMouse(evt.mousePosition);
          evt.Use();
          break;

        case EventType.MouseDrag when evt.button == 0:
          PaintAtMouse(evt.mousePosition);
          evt.Use();
          break;

        case EventType.MouseUp when evt.button == 0:
          evt.Use();
          break;
      }
    }

    private void PaintAtMouse(Vector2 mousePosition)
    {
      var ray = HandleUtility.GUIPointToWorldRay(mousePosition);
      if (!GroundPlane.Raycast(ray, out var distance))
        return;

      var hit = ray.GetPoint(distance);
      var cellSize = _levelData.MapData.CellSize;
      var cell = new Vector2Int(
        Mathf.FloorToInt(hit.x / cellSize),
        Mathf.FloorToInt(hit.z / cellSize));

      if (_levelData.MapData.IsFilled(cell) == !_erasing)
        return;

      Undo.RecordObject(_levelData.MapData, _erasing ? "Erase Map Cell" : "Fill Map Cell");
      _levelData.MapData.SetFilled(cell, !_erasing);
      EditorUtility.SetDirty(_levelData.MapData);
      SceneView.RepaintAll();
    }
  }
}
