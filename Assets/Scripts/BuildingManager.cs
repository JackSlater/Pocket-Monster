using System.Collections.Generic;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    [Header("Buildings")]
    public List<Building> buildings = new List<Building>();

    [Header("Debug Effort")]
    public float constructionEffortPerSecond = 30f;

    private void Start()
    {
        if (buildings.Count == 0)
        {
            buildings.AddRange(FindObjectsOfType<Building>());
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        foreach (var building in buildings)
        {
            if (building == null) continue;

            // always apply effort, ignore productivity for now
            float effortForThisFrame = constructionEffortPerSecond * dt;
            building.Tick(effortForThisFrame, 0f, dt, ProductivityBand.Thriving);
        }
    }
}
