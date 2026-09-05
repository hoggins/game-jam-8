using System;
using System.Collections.Generic;
using System.Linq;
using Destruction;
using Map;
using Movement;
using UnityEditor;
using UnityEngine;

namespace Balance.Editor
{
  /// <summary>
  /// One place to find and tweak every gameplay-balance ScriptableObject in the project (hero
  /// progression, mob/battle stats, special-object spawn ranges, and the systemic tuning assets),
  /// instead of hunting through Assets/Resources. Purely a viewer/editor over existing assets —
  /// it does not add any new data, it just surfaces what <see cref="BattleBalanceConfig"/>,
  /// <see cref="ProgressionBalanceConfig"/> and friends already hold.
  /// </summary>
  public sealed class GameBalanceWindow : EditorWindow
  {
    private sealed class Section
    {
      public readonly string Title;
      public readonly Type AssetType;
      public UnityEngine.Object Asset;
      public UnityEditor.Editor CachedEditor;
      public bool Expanded = true;

      public Section(string title, Type assetType)
      {
        Title = title;
        AssetType = assetType;
      }
    }

    private readonly List<Section> _heroAndBattle = new()
    {
      new Section("Hero Progression", typeof(ProgressionBalanceConfig)),
      new Section("Battle & Mobs", typeof(BattleBalanceConfig)),
    };

    private readonly List<Section> _mapAndSpawning = new()
    {
      new Section("Special Object Spawn Ranges", typeof(SpecialSpawnSettings)),
      new Section("House Difficulty Selection", typeof(HouseSet)),
    };

    private readonly List<Section> _systemic = new()
    {
      new Section("Movement Tuning", typeof(MovementSettings)),
      new Section("Environment Decay", typeof(EnvironmentDecaySettings)),
      new Section("Environment Visibility", typeof(EnvironmentVisibilitySettings)),
      new Section("Hit FX", typeof(HitFxSettings)),
    };

    private Vector2 _scroll;

    [MenuItem("Game/Balance Window")]
    private static void Open()
    {
      var window = GetWindow<GameBalanceWindow>("Game Balance");
      window.minSize = new Vector2(420f, 400f);
      window.RefreshAssets();
    }

    private void OnEnable() =>
      RefreshAssets();

    private void OnDisable()
    {
      foreach (var section in AllSections())
        if (section.CachedEditor != null)
          DestroyImmediate(section.CachedEditor);
    }

    private IEnumerable<Section> AllSections() =>
      _heroAndBattle.Concat(_mapAndSpawning).Concat(_systemic);

    private void RefreshAssets()
    {
      foreach (var section in AllSections())
      {
        var guid = AssetDatabase.FindAssets($"t:{section.AssetType.Name}").FirstOrDefault();
        section.Asset = guid != null
          ? AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), section.AssetType)
          : null;

        if (section.CachedEditor != null)
          DestroyImmediate(section.CachedEditor);
        section.CachedEditor = section.Asset != null ? UnityEditor.Editor.CreateEditor(section.Asset) : null;
      }
    }

    private void OnGUI()
    {
      using (var scroll = new EditorGUILayout.ScrollViewScope(_scroll))
      {
        _scroll = scroll.scrollPosition;

        if (GUILayout.Button("Refresh", GUILayout.Width(80f)))
          RefreshAssets();

        EditorGUILayout.Space(4f);
        DrawGroup("Hero", _heroAndBattle);
        DrawGroup("Map / Spawning", _mapAndSpawning);
        DrawGroup("Systemic Tuning", _systemic);
      }
    }

    private void DrawGroup(string groupTitle, List<Section> sections)
    {
      EditorGUILayout.LabelField(groupTitle, EditorStyles.boldLabel);

      foreach (var section in sections)
        DrawSection(section);

      EditorGUILayout.Space(8f);
    }

    private void DrawSection(Section section)
    {
      EditorGUILayout.BeginVertical(EditorStyles.helpBox);

      EditorGUILayout.BeginHorizontal();
      section.Expanded = EditorGUILayout.Foldout(section.Expanded, section.Title, true);

      using (new EditorGUI.DisabledScope(section.Asset == null))
        if (GUILayout.Button("Ping", GUILayout.Width(50f)))
          EditorGUIUtility.PingObject(section.Asset);

      EditorGUILayout.EndHorizontal();

      if (section.Asset == null)
      {
        EditorGUILayout.HelpBox($"No {section.AssetType.Name} asset found in the project.", MessageType.Warning);
        EditorGUILayout.EndVertical();
        return;
      }

      if (section.Expanded)
      {
        EditorGUI.BeginChangeCheck();
        section.CachedEditor.OnInspectorGUI();
        if (EditorGUI.EndChangeCheck())
          EditorUtility.SetDirty(section.Asset);
      }

      EditorGUILayout.EndVertical();
    }
  }
}
