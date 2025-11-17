using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    [Header("Buildings")]
    public List<Building> buildings = new List<Building>();

    [Header("Spawning")]
    public Building buildingPrefab;
    public List<Transform> spawnPoints = new List<Transform>();
    public int targetUnderConstructionCount = 1;

    private int nextSpawnIndex = 0;

    private void Start()
    {
        // Collect any buildings already in the scene
        var found = FindObjectsOfType<Building>().ToList();

        // Merge with any that might have been added in Inspector (we'll de-dup)
        buildings.AddRange(found);

        // Remove nulls and duplicates
        buildings = buildings
            .Where(b => b != null)
            .Distinct()
            .ToList();

        // Sort left-to-right so index order matches what you see on screen
        SortBuildingsByPosition();
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameOver)
            return;

        EnsureActiveBuildings();
    }

    private void EnsureActiveBuildings()
    {
        int underConstructionCount = 0;
        foreach (var b in buildings)
        {
            if (b != null && b.currentState == BuildingState.UnderConstruction)
            {
                underConstructionCount++;
            }
        }

        while (underConstructionCount < targetUnderConstructionCount)
        {
            if (!CanSpawnNewBuilding())
                break;

            SpawnNewBuilding();
            underConstructionCount++;
        }
    }

    private bool CanSpawnNewBuilding()
    {
        return buildingPrefab != null && spawnPoints.Count > 0;
    }

    private void SpawnNewBuilding()
    {
        if (!CanSpawnNewBuilding()) return;

        Transform spawn = spawnPoints[nextSpawnIndex % spawnPoints.Count];
        nextSpawnIndex++;

        Building newBuilding = Instantiate(buildingPrefab, spawn.position, Quaternion.identity);

        newBuilding.InitializeAsNewConstruction();
        RegisterBuilding(newBuilding);
    }

    public void RegisterBuilding(Building building)
    {
        if (building == null) return;

        if (!buildings.Contains(building))
        {
            buildings.Add(building);
            SortBuildingsByPosition();
        }
    }

    public void UnregisterBuilding(Building building)
    {
        if (building == null) return;

        if (buildings.Contains(building))
        {
            buildings.Remove(building);
        }
    }

    private void SortBuildingsByPosition()
    {
        buildings = buildings
            .Where(b => b != null)
            .OrderBy(b => b.transform.position.x)   // left → right
            .ToList();
    }
}
