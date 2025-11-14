using System.Collections.Generic;
using UnityEngine;

public class PopulationManager : MonoBehaviour
{
    [Header("References")]
    public ProductivityManager productivityManager;

    [Header("Villagers")]
    public List<Villager> villagers = new List<Villager>();

    [Header("Update Settings")]
    public float stateUpdateInterval = 1.5f;
    private float stateUpdateTimer = 0f;

    private void Start()
    {
        if (productivityManager == null)
            productivityManager = FindObjectOfType<ProductivityManager>();

        if (villagers.Count == 0)
        {
            villagers.AddRange(FindObjectsOfType<Villager>());
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameOver)
            return;

        stateUpdateTimer += Time.deltaTime;
        if (stateUpdateTimer >= stateUpdateInterval)
        {
            stateUpdateTimer = 0f;
            UpdateVillagerStates();
        }
    }

    private void UpdateVillagerStates()
    {
        if (productivityManager == null) return;

        ProductivityBand band = productivityManager.GetBand();

        foreach (var villager in villagers)
        {
            villager.UpdateStateFromProductivity(band, productivityManager.CurrentProductivity);
        }
    }

    public void OnGameOver()
    {
        // Push everyone to destructive in end phase
        foreach (var villager in villagers)
        {
            villager.SetState(PersonState.Destructive);
        }
    }
}
