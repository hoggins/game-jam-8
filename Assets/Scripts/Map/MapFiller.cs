using System.Collections.Generic;
using UnityEngine;

namespace Map
{
  public readonly struct HousePlacement
  {
    public readonly HouseObject House;
    public readonly Vector2Int Cell;

    public HousePlacement(HouseObject house, Vector2Int cell)
    {
      House = house;
      Cell = cell;
    }
  }

  public static class MapFiller
  {
    public static List<HousePlacement> Fill(MapData mapData, HouseSet houseSet, Vector2 originCell, int seed = 0)
    {
      var placements = new List<HousePlacement>();
      if (mapData == null || houseSet == null)
        return placements;

      var free = new HashSet<Vector2Int>(mapData.FilledCells);

      var cells = new List<Vector2Int>(free);
      cells.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));

      var houses = new List<HouseObject>();
      var uniqueHouses = new List<HouseObject>();
      foreach (var house in houseSet.Houses)
      {
        if (!house.enabled || house.prefab == null || house.size.x <= 0 || house.size.y <= 0)
          continue;

        if (house.unique)
          uniqueHouses.Add(house);
        else
          houses.Add(house);
      }

      houses.Sort((a, b) => (b.size.x * b.size.y).CompareTo(a.size.x * a.size.y));

      if (houses.Count == 0 && uniqueHouses.Count == 0)
        return placements;

      var housesByLevel = new Dictionary<int, List<HouseObject>>();
      foreach (var house in houses)
      {
        if (!housesByLevel.TryGetValue(house.difficultyLevel, out var list))
          housesByLevel[house.difficultyLevel] = list = new List<HouseObject>();
        list.Add(house);
      }

      var random = new System.Random(seed);

      PlaceUniqueHouses(uniqueHouses, cells, free, placements, random);

      if (houses.Count == 0)
        return placements;

      foreach (var cell in cells)
      {
        if (!free.Contains(cell))
          continue;

        var distance = Vector2.Distance(cell, originCell);
        var level = houseSet.PickDifficultyLevel(distance, random);
        var candidates = housesByLevel.TryGetValue(level, out var levelHouses) ? levelHouses : houses;

        HouseObject chosen = null;
        var startIndex = random.Next(candidates.Count);
        for (var i = 0; i < candidates.Count; i++)
        {
          var house = candidates[(startIndex + i) % candidates.Count];
          if (CanPlace(free, cell, house.size))
          {
            chosen = house;
            break;
          }
        }

        if (chosen == null)
          continue;

        Occupy(free, cell, chosen.size);
        placements.Add(new HousePlacement(chosen, cell));
      }

      return placements;
    }

    /// Unique houses go down first, each at a random cell that fits, so a single mandatory
    /// building is guaranteed to appear instead of depending on the random per-cell draw.
    private static void PlaceUniqueHouses(
      List<HouseObject> uniqueHouses,
      List<Vector2Int> cells,
      HashSet<Vector2Int> free,
      List<HousePlacement> placements,
      System.Random random)
    {
      foreach (var house in uniqueHouses)
      {
        var startIndex = random.Next(cells.Count);
        for (var i = 0; i < cells.Count; i++)
        {
          var cell = cells[(startIndex + i) % cells.Count];
          if (!free.Contains(cell) || !CanPlace(free, cell, house.size))
            continue;

          Occupy(free, cell, house.size);
          placements.Add(new HousePlacement(house, cell));
          break;
        }
      }
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
  }
}
