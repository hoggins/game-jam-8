using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Map.Editor
{
  public class LevelEditorWindow : EditorWindow
  {
    private static readonly Plane GroundPlane = new(Vector3.up, Vector3.zero);

    private enum PaintZone
    {
      House,
      Road,
      Sidewalk
    }

    private LevelData _levelData;
    private bool _erasing;
    private PaintZone _paintZone;
    private RoadWidth _roadWidth = RoadWidth.TwoWay;

    private GameObject _materialSourceObject;
    private bool _replaceExistingCells;

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
      DrawObjectField(so.FindProperty("roadSet"), "Road Set", typeof(RoadSet));

      using (new EditorGUI.DisabledScope(_levelData.RoadSet == null))
      {
        if (GUILayout.Button("Configure all"))
          LevelEditorUtility.ConfigureAllRoadPieces(_levelData.RoadSet);
      }

      DrawObjectField(so.FindProperty("sidewalkSet"), "Sidewalk Set", typeof(SidewalkSet));
      EditorGUILayout.PropertyField(so.FindProperty("seed"));
      EditorGUILayout.PropertyField(so.FindProperty("gridExtent"));
      EditorGUILayout.PropertyField(so.FindProperty("showGrid"));
      EditorGUILayout.PropertyField(so.FindProperty("showTimerRoute"));
      if (so.FindProperty("showTimerRoute").boolValue)
        EditorGUILayout.PropertyField(so.FindProperty("showTimerRouteLabels"));

      so.ApplyModifiedProperties();

      EditorGUILayout.Space();

      if (_levelData.MapData == null)
      {
        EditorGUILayout.HelpBox("No MapData assigned.", MessageType.Info);
        if (GUILayout.Button("Create Map Data"))
          CreateMapData();
        return;
      }

      _paintZone = (PaintZone)EditorGUILayout.EnumPopup("Paint Zone", _paintZone);

      if (_paintZone == PaintZone.Road)
        _roadWidth = (RoadWidth)EditorGUILayout.EnumPopup("Road Width", _roadWidth);

      var label = _levelData.isEditing ? "Stop Drawing" : "Start Drawing";
      if (GUILayout.Button(label))
      {
        _levelData.isEditing = !_levelData.isEditing;
        SceneView.RepaintAll();
      }

      if (_levelData.isEditing)
        EditorGUILayout.HelpBox(
          $"Click + drag in the Scene view to fill {_paintZone} cells. Hold Shift to erase.", MessageType.None);

      EditorGUILayout.Space();

      using (new EditorGUI.DisabledScope(_levelData.HouseSet == null))
      {
        if (GUILayout.Button("Simulate Placement"))
          _levelData.Fill();
      }

      if (GUILayout.Button("Clear Simulation"))
        _levelData.Clear();

      EditorGUILayout.Space();
      DrawFillFromMaterialSection();
    }

    private void DrawFillFromMaterialSection()
    {
      EditorGUILayout.LabelField("Fill Zones From Mesh Materials", EditorStyles.boldLabel);
      EditorGUILayout.HelpBox(
        "Reads every material slot on the source mesh at once and marks the cells its triangles " +
        "fall in as House/Road/Sidewalk. The zone comes from the material's name: a descriptive " +
        "name containing \"house\"/\"road\"/\"sidewalk\", or - for generic DCC exports - its " +
        "trailing number (Material.001 = house, .002 = road, .004 = sidewalk).",
        MessageType.None);

      _materialSourceObject = (GameObject)EditorGUILayout.ObjectField(
        "Source Object", _materialSourceObject, typeof(GameObject), true);

      var renderer = _materialSourceObject != null ? _materialSourceObject.GetComponent<MeshRenderer>() : null;
      var meshFilter = _materialSourceObject != null ? _materialSourceObject.GetComponent<MeshFilter>() : null;

      if (_materialSourceObject != null && (renderer == null || meshFilter == null || meshFilter.sharedMesh == null))
        EditorGUILayout.HelpBox("Source Object needs a MeshFilter with a mesh and a MeshRenderer.", MessageType.Warning);

      _replaceExistingCells = EditorGUILayout.ToggleLeft(
        "Replace existing cells per zone (instead of merging)", _replaceExistingCells);

      using (new EditorGUI.DisabledScope(renderer == null || meshFilter == null || meshFilter.sharedMesh == null))
      {
        if (GUILayout.Button("Compute && Fill All Layers"))
          FillAllLayersFromMaterial();
      }
    }

    private void FillAllLayersFromMaterial()
    {
      var cellSize = _levelData.MapData.CellSize;
      var cellsByZone = LevelEditorUtility.ComputeCellsForAllMaterialSlots(
        _materialSourceObject, cellSize, out var unmatchedSlots);

      if (cellsByZone.Count == 0)
      {
        Debug.LogWarning(
          "No material slot matched a known zone (name must contain \"house\", \"road\" or \"sidewalk\").");
        return;
      }

      if (_replaceExistingCells &&
          !EditorUtility.DisplayDialog(
            "Replace Map Cells",
            $"This will clear existing cells for {cellsByZone.Count} matched zone(s) and replace them " +
            $"with cells computed from '{_materialSourceObject.name}'. Continue?",
            "Replace", "Cancel"))
        return;

      foreach (var pair in cellsByZone)
        LevelEditorUtility.ApplyCellsToZone(_levelData.MapData, pair.Key, pair.Value, _replaceExistingCells);

      SceneView.RepaintAll();

      var summary = string.Join(", ", cellsByZone.Select(pair => $"{pair.Key}: {pair.Value.Count}"));
      Debug.Log($"Filled cells from '{_materialSourceObject.name}' - {summary}.");

      if (unmatchedSlots.Count > 0)
        Debug.LogWarning(
          $"Unmatched material slots (no house/road/sidewalk in name): {string.Join(", ", unmatchedSlots)}");
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

      if (PrefabUtility.GetPrefabInstanceStatus(target) == PrefabInstanceStatus.NotAPrefab)
        target = LevelEditorUtility.CreateLevelPrefabNextToScene(target, target.scene);

      Selection.activeGameObject = target;
      _levelData = target.GetComponent<LevelData>();
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

      var mapData = _levelData.MapData;
      var skip = _paintZone switch
      {
        PaintZone.Road => _erasing
          ? !mapData.IsRoad(cell)
          : mapData.IsRoad(cell) && mapData.GetRoadWidth(cell) == _roadWidth,
        PaintZone.Sidewalk => mapData.IsSidewalk(cell) == !_erasing,
        _ => mapData.IsFilled(cell) == !_erasing
      };

      if (skip)
        return;

      Undo.RecordObject(mapData, _erasing ? $"Erase {_paintZone} Cell" : $"Fill {_paintZone} Cell");

      switch (_paintZone)
      {
        case PaintZone.Road:
          mapData.SetRoad(cell, !_erasing, _roadWidth);
          break;
        case PaintZone.Sidewalk:
          mapData.SetSidewalk(cell, !_erasing);
          break;
        default:
          mapData.SetFilled(cell, !_erasing);
          break;
      }

      EditorUtility.SetDirty(mapData);
      SceneView.RepaintAll();
    }
  }
}
