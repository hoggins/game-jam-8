using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Map
{
  public class MapGridFiller : MonoBehaviour
  {
    private const string ContainerName = "SpawnedHouses";

    [SerializeField] private MapData mapData;
    [SerializeField] private HouseSet houseSet;
    [SerializeField] private int seed;

    public MapData MapData => mapData;
    public HouseSet HouseSet => houseSet;

    private void Start() => Fill();

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

#if UNITY_EDITOR
      if (!Application.isPlaying)
        Undo.RegisterCreatedObjectUndo(container.gameObject, "Simulate Placement");
#endif

      return container;
    }
  }
}
