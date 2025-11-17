using System.Collections.Generic;
using UnityEngine;

public class PopulationManager : MonoBehaviour
{
    [Header("References")]
    public ProductivityManager productivityManager;

    [Header("Villager Spawning")]
    [Tooltip("Villager prefab to spawn.")]
    public Villager villagerPrefab;

    [Tooltip("How many villagers should exist in the scene total.")]
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

        InitializeVillagers();
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

    // ----------------- SPAWNING / SETUP -----------------

    private void InitializeVillagers()
    {
        villagers.Clear();

        // 1) Grab any pre-placed villagers in the scene
        Villager[] existing = FindObjectsOfType<Villager>();
        villagers.AddRange(existing);

        // 2) Spawn extra ones from prefab until we hit initialVillagerCount
        if (villagerPrefab == null)
        {
            if (villagers.Count == 0)
            {
                Debug.LogWarning("PopulationManager: No villagerPrefab assigned and no existing villagers found.");
            }
            return;
        }

        int toSpawn = Mathf.Max(0, initialVillagerCount - villagers.Count);

        for (int i = 0; i < toSpawn; i++)
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

    // ----------------- PHONE INTERACTION -----------------

    /// <summary>
    /// Called when a phone is dropped.
    /// - Villagers who are currently WORKING keep working (ignore the phone)
    /// - Non-working villagers freeze, and ONE of them (nearest) goes for the phone
    /// </summary>
    public void OnPhoneDropped(Phone phone)
    {
        if (phone == null || villagers.Count == 0) return;

        // Pick ONE villager (nearest) who is still "healthy"
        Villager chaser = null;
        float bestDistSq = float.MaxValue;

        foreach (var v in villagers)
        {
            if (v == null) continue;

            // Ignore villagers already lost or destroying stuff
            if (v.currentState == PersonState.PhoneAddiction ||
                v.currentState == PersonState.Destructive)
            {
                continue;
            }

            float dSq = (v.transform.position - phone.transform.position).sqrMagnitude;
            if (dSq < bestDistSq)
            {
                bestDistSq = dSq;
                chaser = v;
            }
        }

        if (chaser == null) return;

        // Make sure nobody else is frozen because of previous phones
        foreach (var v in villagers)
        {
            if (v == null) continue;
            v.SetFrozenByPhone(false);
        }

        // This one villager will chase the phone and become addicted
        chaser.BecomePhoneChaser(phone);
    }


    /// <summary>
    /// Called by a villager when they actually reach and pick up the phone.
    /// That villager becomes addicted and stops working; others unfreeze.
    /// </summary>
    public void OnVillagerPickedUpPhone(Villager collector, Phone phone)
    {
        foreach (var v in villagers)
        {
            if (v == null) continue;

            if (v == collector)
            {
                v.SetPhoneAddicted();
            }
            else
            {
                v.SetFrozenByPhone(false);
            }
        }
    }

    /// <summary>
    /// Called when a phone disappears WITHOUT being picked up by a villager
    /// (tapped by player or expired lifetime).
    /// Non-addicted, non-destructive villagers are unfrozen.
    /// </summary>
    public void OnPhoneCleared()
    {
        foreach (var v in villagers)
        {
            if (v == null) continue;

            if (v.currentState == PersonState.PhoneAddiction ||
                v.currentState == PersonState.Destructive)
                continue;

            v.SetFrozenByPhone(false);
        }
    }

    // ----------------- STATS HELPERS FOR UI -----------------

    public int GetTotalVillagerCount()
    {
        int count = 0;
        foreach (var v in villagers)
        {
            if (v != null)
            {
                count++;
            }
        }
        return count;
    }

    public int GetPhoneAddictedCount()
    {
        int count = 0;
        foreach (var v in villagers)
        {
            if (v != null && v.currentState == PersonState.PhoneAddiction)
            {
                count++;
            }
        }
        return count;
    }
    
}
