using System.Collections.Generic;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    [Header("Buildings")]
    public List<Building> buildings = new List<Building>();

    [Header("Spawning")]
    [Tooltip("Prefab to spawn when a new building starts construction.")]
    public Building buildingPrefab;

    [Tooltip("Positions where new buildings can appear. Will cycle through this list.")]
    public List<Transform> spawnPoints = new List<Transform>();

    [Tooltip("How many buildings should be under construction at once.")]
    public int targetUnderConstructionCount = 1;

    private int nextSpawnIndex = 0;

    private void Start()
    {
        // Auto-collect any buildings already in the scene
        if (buildings.Count == 0)
        {
            buildings.AddRange(FindObjectsOfType<Building>());
        }
    }

    private void Update()
    {
        // Optional: stop spawning when game is over.
        if (GameManager.Instance != null && GameManager.Instance.isGameOver)
            return;

        // NO baseline progress here – buildings only advance when villagers tick them.
        EnsureActiveBuildings();
    }

    private void EnsureActiveBuildings()
    {
        // Count how many are still being built
        int underConstructionCount = 0;
        foreach (var b in buildings)
        {
            if (b != null && b.currentState == BuildingState.UnderConstruction)
            {
                underConstructionCount++;
            }
        }

        // While we have fewer than the target, spawn more
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

        // Use the helper so state + visuals match
        newBuilding.InitializeAsNewConstruction();

        RegisterBuilding(newBuilding);
    }

    // --- Public registration helpers ---

    public void RegisterBuilding(Building building)
    {
        if (building == null) return;
        if (!buildings.Contains(building))
        {
            buildings.Add(building);
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
    
}
