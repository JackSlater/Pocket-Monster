using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns and tracks all buildings in the level.
/// Keeps at least targetUnderConstructionCount buildings under construction.
/// </summary>
public class BuildingManager : MonoBehaviour
{
    [Header("Buildings (runtime)")]
    public List<Building> buildings = new List<Building>();

    [Header("Spawning")]
    public Building buildingPrefab;
    public List<Transform> spawnPoints = new List<Transform>();
    public int targetUnderConstructionCount = 1;

    private int nextSpawnIndex = 0;

    private void Awake()
    {
        // Pick up any buildings that already exist in the scene
        buildings.Clear();
        Building[] existing = FindObjectsOfType<Building>();
        foreach (var b in existing)
        {
            if (!buildings.Contains(b))
                buildings.Add(b);
        }

        SortBuildingsByPosition();
    }

    private void Start()
    {
        EnsureActiveBuildings();
    }

    private void Update()
    {
        EnsureActiveBuildings();
    }

    /// <summary>
    /// Make sure we always have at least targetUnderConstructionCount
    /// buildings in the UnderConstruction state.
    /// </summary>
    private void EnsureActiveBuildings()
    {
        CleanupNullEntries();

        int underConstruction = 0;
        foreach (var b in buildings)
        {
            if (b != null && b.currentState == BuildingState.UnderConstruction)
                underConstruction++;
        }

        while (underConstruction < targetUnderConstructionCount)
        {
            Building spawned = SpawnNewBuilding();
            if (spawned == null)
                break;

            underConstruction++;
        }
    }

    private Building SpawnNewBuilding()
    {
        if (buildingPrefab == null || spawnPoints == null || spawnPoints.Count == 0)
            return null;

        Transform spawn = spawnPoints[nextSpawnIndex % spawnPoints.Count];
        nextSpawnIndex++;

        Building newBuilding = Instantiate(buildingPrefab, spawn.position, Quaternion.identity);
        newBuilding.InitializeAsNewConstruction();

        buildings.Add(newBuilding);
        SortBuildingsByPosition();

        return newBuilding;
    }

    private void CleanupNullEntries()
    {
        for (int i = buildings.Count - 1; i >= 0; i--)
        {
            if (buildings[i] == null)
                buildings.RemoveAt(i);
        }
    }

    private void SortBuildingsByPosition()
    {
        buildings.Sort((a, b) =>
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;
            return a.transform.position.x.CompareTo(b.transform.position.x);
        });
    }
}
