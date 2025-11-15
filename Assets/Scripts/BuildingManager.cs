using System.Collections.Generic;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    [Header("Buildings")]
    public List<Building> buildings = new List<Building>();

    [Header("Productivity Hook")]
    [Tooltip("If assigned, construction effort comes from the Building activity pool.")]
    public ProductivityManager productivityManager;

    [Tooltip("Maximum construction progress we can consume per second from productivity.")]
    public float maxConsumePerSecond = 50f;

    [Header("Fallback Construction (if no ProductivityManager)")]
    [Tooltip("Used only if no ProductivityManager is present in the scene.")]
    public float constructionEffortPerSecond = 30f;

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
        // Auto-find buildings already in the scene
        if (buildings.Count == 0)
        {
            buildings.AddRange(FindObjectsOfType<Building>());
        }

        // Auto-find ProductivityManager if not wired in Inspector
        if (productivityManager == null)
        {
            productivityManager = FindObjectOfType<ProductivityManager>();
        }
    }

    private void Update()
    {
        // Optional: stop construction when game is over
        if (GameManager.Instance != null && GameManager.Instance.isGameOver)
            return;

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // --- 1. Figure out which buildings still need work ---
        var activeBuildings = new List<Building>();
        foreach (var b in buildings)
        {
            if (b == null) continue;
            if (b.currentState == BuildingState.UnderConstruction)
            {
                activeBuildings.Add(b);
            }
        }

        // --- 2. Get effort for this frame (from productivity or fallback) ---
        float totalEffortThisFrame = 0f;
        ProductivityBand band = ProductivityBand.Thriving;

        if (productivityManager != null)
        {
            float maxThisFrame = maxConsumePerSecond * dt;
            totalEffortThisFrame = productivityManager.ConsumeBuildingProgress(maxThisFrame);
            band = productivityManager.GetBand();
        }
        else
        {
            totalEffortThisFrame = constructionEffortPerSecond * dt;
        }

        // --- 3. Apply effort to all under-construction buildings ---
        if (activeBuildings.Count > 0 && totalEffortThisFrame > 0f)
        {
            float effortPerBuilding = totalEffortThisFrame / activeBuildings.Count;

            foreach (var b in activeBuildings)
            {
                b.Tick(effortPerBuilding, 0f, dt, band);
            }
        }

        // --- 4. Make sure we always have some buildings under construction ---
        EnsureActiveBuildings();
    }

    private void EnsureActiveBuildings()
    {
        // Count how many are still building
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

        // Cycle through spawn points: 0,1,2,... then wrap
        Transform spawn = spawnPoints[nextSpawnIndex % spawnPoints.Count];
        nextSpawnIndex++;

        Building newBuilding = Instantiate(buildingPrefab, spawn.position, Quaternion.identity);

        // Make sure its state is clean
        newBuilding.currentState = BuildingState.UnderConstruction;
        newBuilding.constructionProgress = 0f;

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
