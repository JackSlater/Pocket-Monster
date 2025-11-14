using System.Collections.Generic;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    [Header("References")]
    public ProductivityManager productivityManager;

    [Header("Buildings")]
    public List<Building> buildings = new List<Building>();

    private void Start()
    {
        if (productivityManager == null)
            productivityManager = FindObjectOfType<ProductivityManager>();

        if (buildings.Count == 0)
            buildings.AddRange(FindObjectsOfType<Building>());
    }

    private void Update()
    {
        // For now we do nothing here.
        // We’ll add more logic later once everything compiles.
    }
}
