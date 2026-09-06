using App;
using Combat;
using Model;
using Movement;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace Battle
{
  /// <summary>
  /// Lightweight runtime diagnostics for the battle HUD. The panel is compiled out of the player
  /// by the preprocessor, can be hidden in the editor with <see cref="_showInEditor"/>, and can be
  /// toggled with F2. The toggle is remembered between Play Mode sessions.
  /// </summary>
  [DisallowMultipleComponent]
  public sealed class DebugInfoHud : MonoBehaviour
  {
    private const string ShowInEditorPreferenceKey = "GameJam8.DebugInfoHud.ShowInEditor";

    [SerializeField] private bool _showInEditor = true;
    [SerializeField] private Vector2 _position = new(16f, 16f);
    [SerializeField] private Vector2 _size = new(320f, 250f);
    [SerializeField, Min(10)] private int _fontSize = 16;

    [Inject] private BattleService _battleService;
    [Inject] private CharacterService _characterService;
    [Inject] private Storage _storage;
    [Inject] private MovementUpdater _movementUpdater;

    private GUIStyle _boxStyle;
    private GUIStyle _labelStyle;
    private int _lastToggleFrame = -1;

    private void Awake()
    {
#if UNITY_EDITOR
      if (UnityEditor.EditorPrefs.HasKey(ShowInEditorPreferenceKey))
        _showInEditor = UnityEditor.EditorPrefs.GetBool(
          ShowInEditorPreferenceKey,
          _showInEditor);
      else
        UnityEditor.EditorPrefs.SetBool(ShowInEditorPreferenceKey, _showInEditor);

      this.AsInjected();
#else
      enabled = false;
#endif
    }

#if UNITY_EDITOR
    private readonly GUIContent[] _labelContents =
    {
      new("DEBUG INFO"),
      new(),
      new(),
      new(),
      new(),
      new(),
      new(),
      new(),
    };

    private int _lastContentFrame = -1;

    private void Update()
    {
      if (Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame)
        ToggleVisibility();
    }

    private void ToggleVisibility()
    {
      if (_lastToggleFrame == Time.frameCount)
        return;

      _lastToggleFrame = Time.frameCount;
      _showInEditor = !_showInEditor;
      UnityEditor.EditorPrefs.SetBool(ShowInEditorPreferenceKey, _showInEditor);
    }

    private void OnGUI()
    {
      if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.F2)
      {
        ToggleVisibility();
        Event.current.Use();
      }

      if (!_showInEditor)
        return;

      EnsureStyles();

      var panelRect = new Rect(
        _position.x,
        _position.y,
        Mathf.Max(1f, _size.x),
        Mathf.Max(1f, _size.y));

      GUI.Box(panelRect, GUIContent.none, _boxStyle);
      RefreshLabelContents();

      var labelRect = new Rect(
        panelRect.x + 10f,
        panelRect.y + 8f,
        Mathf.Max(1f, panelRect.width - 20f),
        Mathf.Max(1f, _labelStyle.lineHeight));
      var labelHeight = Mathf.Max(1f, _labelStyle.lineHeight);
      for (var i = 0; i < _labelContents.Length; i++)
      {
        labelRect.y = panelRect.y + 8f + i * labelHeight;
        GUI.Label(labelRect, _labelContents[i], _labelStyle);
      }
    }

    private void RefreshLabelContents()
    {
      if (_lastContentFrame == Time.frameCount)
        return;

      _lastContentFrame = Time.frameCount;
      _labelContents[1].text = $"Timer: {FormatTime(_battleService?.Timer ?? 0f)}";
      _labelContents[2].text =
        $"Player HP: {_characterService?.CurrentHealth ?? 0}/{_characterService?.MaxHealth ?? 0}";
      _labelContents[3].text =
        $"Battle: {GetBattleState()} | Timer destroyed: {_battleService?.IsTimerDestroyed ?? false}";
      _labelContents[4].text =
        $"Coins: {_characterService?.CurrentCoins ?? 0} | Attack: {_characterService?.AttackPower ?? 0}";
      _labelContents[5].text =
        $"Ducks killed: {_storage?.DucksKilled ?? 0} | Buildings: {_storage?.BuildingsDestroyed ?? 0}";
      _labelContents[6].text =
        $"Active agents: {GetActiveAgentCount()} | Live ducks: {GetLiveDuckCount()}";
      _labelContents[7].text = $"Time scale: {Time.timeScale:0.##}";
    }

    private void EnsureStyles()
    {
      if (_boxStyle != null)
        return;

      _boxStyle = new GUIStyle(GUI.skin.box)
      {
        padding = new RectOffset(10, 10, 8, 8)
      };
      _labelStyle = new GUIStyle(GUI.skin.label)
      {
        fontSize = Mathf.Max(10, _fontSize),
        normal = { textColor = Color.white }
      };
    }

    private string GetBattleState()
    {
      if (_battleService == null)
        return "Unavailable";

      if (!_battleService.IsBattleActive)
        return "Inactive";

      return _battleService.IsTimingOut ? "Timing Out" : "Active";
    }

    private int GetActiveAgentCount() =>
      _movementUpdater?.ActiveAgents?.Count ?? 0;

    private int GetLiveDuckCount()
    {
      if (_movementUpdater?.ActiveAgents == null)
        return 0;

      var count = 0;
      var agents = _movementUpdater.ActiveAgents;
      for (var i = 0; i < agents.Count; i++)
      {
        var agent = agents[i];
        if (agent == null || !agent.isActiveAndEnabled)
          continue;

        var mob = agent.GetComponent<Mob>();
        if (mob != null && mob.IsAlive)
          count++;
      }

      return count;
    }

    private static string FormatTime(float seconds)
    {
      var totalSeconds = Mathf.Max(0, Mathf.CeilToInt(seconds));
      return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
    }
#endif
  }
}
