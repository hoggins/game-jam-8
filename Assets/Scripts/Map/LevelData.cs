using App;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using VContainer;

namespace Map
{
  public class LevelData : MonoBehaviour
  {
    private const string ContainerName = "SpawnedHouses";

    [SerializeField] private MapData mapData;
    [SerializeField] private HouseSet houseSet;
    [SerializeField] private int seed;
    [SerializeField] private int gridExtent = 10;
    [SerializeField] private bool showGrid = true;
    [HideInInspector] public bool isEditing;

    [Inject] private MapEnvironmentSpawner _spawner;

    public MapData MapData => mapData;
    public HouseSet HouseSet => houseSet;

    private void Awake() => this.AsInjected();

    private void Start() => _spawner.Spawn(mapData, houseSet, seed, transform);

    private void OnDrawGizmos()
    {
      if (!showGrid || mapData == null)
        return;

      var cellSize = mapData.CellSize;

      Gizmos.color = isEditing ? new Color(0.2f, 0.8f, 1f, 0.5f) : new Color(0.2f, 0.8f, 0.2f, 0.5f);
      foreach (var cell in mapData.FilledCells)
      {
        var center = new Vector3(
          cell.x * cellSize + cellSize * 0.5f,
          0f,
          cell.y * cellSize + cellSize * 0.5f);
        Gizmos.DrawCube(center, new Vector3(cellSize, 0.1f, cellSize));
      }

      var extent = gridExtent * cellSize;
      Gizmos.color = new Color(1f, 1f, 1f, 0.3f);
      for (var i = -gridExtent; i <= gridExtent; i++)
      {
        var offset = i * cellSize;
        Gizmos.DrawLine(new Vector3(offset, 0f, -extent), new Vector3(offset, 0f, extent));
        Gizmos.DrawLine(new Vector3(-extent, 0f, offset), new Vector3(extent, 0f, offset));
      }
    }

    public void Fill()
    {
      Clear();

      if (mapData == null || houseSet == null)
        return;

      var container = CreateContainer();
      var cellSize = mapData.CellSize;

      foreach (var placement in MapFiller.Fill(mapData, houseSet, seed))
      {
        var position = new Vector3(
          placement.Cell.x * cellSize + placement.House.size.x * cellSize * 0.5f,
          0f,
          placement.Cell.y * cellSize + placement.House.size.y * cellSize * 0.5f);

        var instance = Instantiate(placement.House.prefab, position, Quaternion.identity, container);
        instance.name = placement.House.name;

#if UNITY_EDITOR
        if (!Application.isPlaying)
          Undo.RegisterCreatedObjectUndo(instance, "Simulate Placement");
#endif
      }
    }

    public void Clear()
    {
      var container = transform.Find(ContainerName);
      if (container == null)
        return;

      if (Application.isPlaying)
      {
        Destroy(container.gameObject);
      }
      else
      {
#if UNITY_EDITOR
        Undo.DestroyObjectImmediate(container.gameObject);
#else
        DestroyImmediate(container.gameObject);
#endif
      }
    }

    private Transform CreateContainer()
    {
      var container = new GameObject(ContainerName).transform;
      container.SetParent(transform, false);

      var parentScale = transform.lossyScale;
      container.localScale = new Vector3(
        parentScale.x != 0f ? 1f / parentScale.x : 1f,
        parentScale.y != 0f ? 1f / parentScale.y : 1f,
        parentScale.z != 0f ? 1f / parentScale.z : 1f);

#if UNITY_EDITOR
      if (!Application.isPlaying)
        Undo.RegisterCreatedObjectUndo(container.gameObject, "Simulate Placement");
#endif

      return container;
    }
  }
}
