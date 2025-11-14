using System.Collections.Generic;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    [Header("References")]
    public ProductivityManager productivityManager;

    [Header("Buildings")]
    public List<Building> buildings = new List<Building>();

    [Header("Consumption")]
    public float buildingEffortPerBuilding = 2f;
    public float infrastructureSupportPerBuilding = 1.25f;

    private void Start()
    {
        if (productivityManager == null)
            productivityManager = FindObjectOfType<ProductivityManager>();

        if (buildings.Count == 0)
            buildings.AddRange(FindObjectsOfType<Building>());
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameOver)
            return;

        if (productivityManager == null || buildings.Count == 0)
            return;

        float deltaTime = Time.deltaTime;
        float totalBuildingRequest = buildingEffortPerBuilding * buildings.Count * deltaTime;
        float totalInfrastructureRequest = infrastructureSupportPerBuilding * buildings.Count * deltaTime;

        float availableBuildingEffort = productivityManager.ConsumeBuildingProgress(totalBuildingRequest);
        float availableInfrastructureSupport = productivityManager.ConsumeInfrastructureProgress(totalInfrastructureRequest);

        float perBuildingEffort = buildings.Count > 0 ? availableBuildingEffort / buildings.Count : 0f;
        float perBuildingInfrastructure = buildings.Count > 0 ? availableInfrastructureSupport / buildings.Count : 0f;

        ProductivityBand band = productivityManager.GetBand();

        foreach (var building in buildings)
        {
            if (building == null)
                continue;

            building.Tick(perBuildingEffort, perBuildingInfrastructure, deltaTime, band);
        }
    }
}
