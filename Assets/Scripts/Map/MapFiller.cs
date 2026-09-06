using System.Collections.Generic;
using UnityEngine;

namespace Map
{
  public readonly struct HousePlacement
  {
    public readonly HouseObject House;
    public readonly Vector2Int Cell;
    public readonly int RotationDegrees;

    public HousePlacement(HouseObject house, Vector2Int cell, int rotationDegrees)
    {
      House = house;
      Cell = cell;
      RotationDegrees = rotationDegrees;
    }
  }

  public static class MapFiller
  {
    /// The house's footprint as occupied on the grid once rotated: a 90/270 rotation swaps which
    /// axis is which, so a 1x2 house rotated a quarter turn occupies a 2x1 area.
    public static Vector2Int RotatedSize(Vector2Int size, int rotationDegrees) =>
      rotationDegrees == 90 || rotationDegrees == 270 ? new Vector2Int(size.y, size.x) : size;

    public static List<HousePlacement> Fill(
      MapData mapData, HouseSet houseSet, Vector2 originCell, int seed = 0, TimerRoute route = null)
    {
      var placements = new List<HousePlacement>();
      if (mapData == null || houseSet == null)
        return placements;

      var free = new HashSet<Vector2Int>(mapData.FilledCells);

      var cells = new List<Vector2Int>(free);
      cells.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));

      var houses = new List<HouseObject>();
      foreach (var house in houseSet.Houses)
        if (house.enabled && house.prefab != null && house.size.x > 0 && house.size.y > 0)
          houses.Add(house);

      houses.Sort((a, b) => (b.size.x * b.size.y).CompareTo(a.size.x * a.size.y));

      if (houses.Count == 0)
        return placements;

      var housesByLevel = new Dictionary<int, List<HouseObject>>();
      foreach (var house in houses)
      {
        if (!housesByLevel.TryGetValue(house.difficultyLevel, out var list))
          housesByLevel[house.difficultyLevel] = list = new List<HouseObject>();
        list.Add(house);
      }

      var random = new System.Random(seed);

      foreach (var cell in cells)
      {
        if (!free.Contains(cell))
          continue;

        var level = route != null
          ? houseSet.PickDifficultyLevelByRouteProgress(route.ProjectNormalizedProgress(CellCenter(cell, mapData.CellSize)))
          : houseSet.PickDifficultyLevel(Vector2.Distance(cell, originCell), random);
        var candidates = housesByLevel.TryGetValue(level, out var levelHouses) ? levelHouses : houses;

        HouseObject chosen = null;
        var chosenRotation = 0;
        var chosenSize = Vector2Int.zero;
        var startIndex = random.Next(candidates.Count);
        for (var i = 0; i < candidates.Count; i++)
        {
          var house = candidates[(startIndex + i) % candidates.Count];
          var rotation = random.Next(4) * 90;
          var size = RotatedSize(house.size, rotation);
          if (CanPlace(free, cell, size))
          {
            chosen = house;
            chosenRotation = rotation;
            chosenSize = size;
            break;
          }
        }

        if (chosen == null)
          continue;

        Occupy(free, cell, chosenSize);
        placements.Add(new HousePlacement(chosen, cell, chosenRotation));
      }

      return placements;
    }

    private static bool CanPlace(HashSet<Vector2Int> free, Vector2Int origin, Vector2Int size)
    {
      for (var x = 0; x < size.x; x++)
      for (var y = 0; y < size.y; y++)
        if (!free.Contains(origin + new Vector2Int(x, y)))
          return false;
      return true;
    }

    private static void Occupy(HashSet<Vector2Int> free, Vector2Int origin, Vector2Int size)
    {
      for (var x = 0; x < size.x; x++)
      for (var y = 0; y < size.y; y++)
        free.Remove(origin + new Vector2Int(x, y));
    }

    private static Vector3 CellCenter(Vector2Int cell, int cellSize) =>
      new((cell.x + 0.5f) * cellSize, 0f, (cell.y + 0.5f) * cellSize);
  }
}
