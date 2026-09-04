using UnityEditor;
using UnityEngine;

namespace Map.Editor
{
  [CustomEditor(typeof(MapGridFiller))]
  public class MapGridFillerEditor : UnityEditor.Editor
  {
    public override void OnInspectorGUI()
    {
      DrawDefaultInspector();

      var filler = (MapGridFiller)target;

      EditorGUILayout.Space();

      using (new EditorGUI.DisabledScope(filler.MapData == null || filler.HouseSet == null))
      {
        if (GUILayout.Button("Simulate Placement"))
          filler.Fill();
      }

      if (GUILayout.Button("Clear Simulation"))
        filler.Clear();
    }
  }
}
