using App;
using UnityEngine;
using VContainer;

namespace Map
{
  public class EnvironmentSpawnPoint : MonoBehaviour
  {
    [SerializeField] private MapData mapData;
    [SerializeField] private HouseSet houseSet;
    [SerializeField] private RoadSet roadSet;
    [SerializeField] private SidewalkSet sidewalkSet;
    [SerializeField] private int seed;

    [Inject] private MapEnvironmentSpawner _spawner;

    private void Awake() => this.AsInjected();

    private void Start() => _spawner.Spawn(mapData, houseSet, roadSet, sidewalkSet, seed, transform);
  }
}
