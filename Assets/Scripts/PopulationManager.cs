using System.Collections.Generic;
using UnityEngine;

public class PopulationManager : MonoBehaviour
{
    [Header("References")]
    public ProductivityManager productivityManager;

    [Header("Villager Spawning")]
    [Tooltip("Villager prefab to spawn.")]
    public Villager villagerPrefab;

    [Tooltip("How many villagers should exist in the scene.")]
    public int initialVillagerCount = 20;

    [Tooltip("Center of the area where villagers will spawn.")]
    public Vector2 spawnAreaCenter = Vector2.zero;

    [Tooltip("Size of the rectangle where villagers will spawn.")]
    public Vector2 spawnAreaSize = new Vector2(6f, 3f);

    [Header("Villagers (runtime list)")]
    public List<Villager> villagers = new List<Villager>();

    [Header("Update Settings")]
    public float stateUpdateInterval = 1.5f;
    private float stateUpdateTimer = 0f;

    private void Start()
    {
        if (productivityManager == null)
        {
            productivityManager = FindObjectOfType<ProductivityManager>();
        }

        SpawnInitialVillagers();
    }

    private void Update()
    {
        if (productivityManager == null) return;

        stateUpdateTimer -= Time.deltaTime;
        if (stateUpdateTimer <= 0f)
        {
            stateUpdateTimer = stateUpdateInterval;
            UpdateVillagerStates();
        }
    }

    private void SpawnInitialVillagers()
    {
        // Clear any existing list entries (in case of replays)
        villagers.Clear();

        if (villagerPrefab == null)
        {
            Debug.LogWarning("PopulationManager: No villagerPrefab assigned.");
            return;
        }

        for (int i = 0; i < initialVillagerCount; i++)
        {
            Vector2 offset = new Vector2(
                Random.Range(-spawnAreaSize.x * 0.5f, spawnAreaSize.x * 0.5f),
                Random.Range(-spawnAreaSize.y * 0.5f, spawnAreaSize.y * 0.5f)
            );

            Vector2 spawnPos = spawnAreaCenter + offset;
            Villager v = Instantiate(villagerPrefab, spawnPos, Quaternion.identity);
            villagers.Add(v);
        }
    }

    private void UpdateVillagerStates()
    {
        ProductivityBand band = productivityManager.GetBand();
        float currentProductivity = productivityManager.CurrentProductivity;

        foreach (var villager in villagers)
        {
            if (villager == null) continue;
            villager.UpdateStateFromProductivity(band, currentProductivity);
        }
    }

    public void OnGameOver()
    {
        // Push everyone to destructive in end phase
        foreach (var villager in villagers)
        {
            if (villager != null)
            {
                villager.SetState(PersonState.Destructive);
            }
        }
    }
}
