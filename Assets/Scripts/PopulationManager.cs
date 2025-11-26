using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles villager spawning, global movement modifiers,
/// and how villagers react to phones.
/// </summary>
public class PopulationManager : MonoBehaviour
{
    [Header("References")]
    public ProductivityManager productivityManager;
    public BuildingManager buildingManager;

    [Header("Villager Spawning")]
    public Villager villagerPrefab;
    public int initialVillagerCount = 20;
    public Vector2 spawnAreaCenter = Vector2.zero;
    public float spawnAreaWidth = 6f;

    [Header("Villagers (runtime)")]
    public List<Villager> villagers = new List<Villager>();

    [Header("Productivity → State")]
    public float stateUpdateInterval = 1.5f;
    private float stateUpdateTimer = 0f;

    [Header("Global Effects")]
    [Range(0.2f, 2f)]
    public float globalSpeedMultiplier = 1f;

    [Header("Phone Effects")]
    [Tooltip("How many extra villagers are pushed to Idle by a Streaming (yellow) phone.")]
    public int streamingIdleCount = 3;

    private void Awake()
    {
        if (productivityManager == null)
            productivityManager = FindObjectOfType<ProductivityManager>();

        if (buildingManager == null)
            buildingManager = FindObjectOfType<BuildingManager>();
    }

    private void Start()
    {
        SpawnInitialVillagers();
    }

    private void Update()
    {
        stateUpdateTimer -= Time.deltaTime;
        if (stateUpdateTimer <= 0f)
        {
            stateUpdateTimer = stateUpdateInterval;
            UpdateVillagersFromProductivity();
        }
    }

    // ---------------- SPAWNING ----------------

    private void SpawnInitialVillagers()
    {
        CleanupVillagers();

        if (villagerPrefab == null)
            return;

        for (int i = 0; i < initialVillagerCount; i++)
        {
            float xOffset = Random.Range(-spawnAreaWidth * 0.5f, spawnAreaWidth * 0.5f);
            Vector3 spawnPos = new Vector3(spawnAreaCenter.x + xOffset, spawnAreaCenter.y, 0f);

            Villager v = Object.Instantiate(villagerPrefab, spawnPos, Quaternion.identity);
            RegisterVillager(v);
        }
    }

    public void RegisterVillager(Villager v)
    {
        if (v == null) return;
        if (!villagers.Contains(v))
            villagers.Add(v);

        // Make sure the villager knows about its managers
        if (v.buildingManager == null) v.buildingManager = buildingManager;
        if (v.productivityManager == null) v.productivityManager = productivityManager;
        if (v.populationManager == null) v.populationManager = this;
    }

    private void CleanupVillagers()
    {
        for (int i = villagers.Count - 1; i >= 0; i--)
        {
            if (villagers[i] == null)
                villagers.RemoveAt(i);
        }
    }

    // ---------------- PRODUCTIVITY → STATE ----------------

    private void UpdateVillagersFromProductivity()
    {
        if (productivityManager == null)
            return;

        float current = productivityManager.CurrentProductivity;
        ProductivityBand band = productivityManager.GetBand();

        foreach (var v in villagers)
        {
            if (v == null) continue;
            v.UpdateStateFromProductivity(band, current);
        }
    }

    // ---------------- PHONE EVENTS ----------------

    public void OnPhoneDropped(Phone phone)
    {
        CleanupVillagers();
        if (phone == null || villagers.Count == 0) return;

        Vector3 phonePos = phone.transform.position;

        // Pick nearest villager who is not already lost
        Villager chaser = null;
        float bestDistSq = float.MaxValue;

        foreach (var v in villagers)
        {
            if (v == null) continue;

            if (v.currentState == PersonState.PhoneAddiction ||
                v.currentState == PersonState.Destructive)
                continue;

            float dSq = (v.transform.position - phonePos).sqrMagnitude;
            if (dSq < bestDistSq)
            {
                bestDistSq = dSq;
                chaser = v;
            }
        }

        if (chaser != null)
        {
            chaser.BecomePhoneChaser(phone);
            chaser.SetFrozenByPhone(false);
        }

        // If this is a blue phone, slow everyone down while it exists.
        if (phone.phoneType == PhoneType.MainstreamBlue)
        {
            globalSpeedMultiplier = 0.8f;
        }
        else
        {
            globalSpeedMultiplier = 1f;
        }
    }

    // Called when a phone times out / is disabled without pickup
    public void OnPhoneCleared()
    {
        CleanupVillagers();
        globalSpeedMultiplier = 1f;

        foreach (var v in villagers)
        {
            if (v == null) continue;
            v.SetFrozenByPhone(false);
        }
    }

    // Called when one villager successfully picks up a phone
    public void OnVillagerPickedUpPhone(Villager collector, Phone phone)
    {
        CleanupVillagers();
        if (collector == null) return;

        // Collector always becomes phone-addicted
        collector.SetPhoneAddicted();

        if (phone != null)
        {
            switch (phone.phoneType)
            {
                case PhoneType.SocialMediaRed:
                    // Red phone: villager becomes violent
                    collector.SetState(PersonState.Destructive);
                    break;

                case PhoneType.StreamingYellow:
                    // Yellow phone: make some nearby villagers idle
                    ApplyStreamingEffect(collector.transform.position);
                    break;

                case PhoneType.MainstreamBlue:
                    // Blue phone: remove slow effect when picked up
                    globalSpeedMultiplier = 1f;
                    break;

                case PhoneType.GamblingGreen:
                    // Green phone: villager starts destroying buildings
                    collector.BecomeBuildingDestroyer();
                    break;
            }
        }

        // Phone is gone, so unfreeze anyone who might have been paused earlier.
        foreach (var v in villagers)
        {
            if (v == null) continue;
            v.SetFrozenByPhone(false);
        }
    }

    private void ApplyStreamingEffect(Vector3 origin)
    {
        if (streamingIdleCount <= 0) return;

        List<Villager> candidates = new List<Villager>();
        foreach (var v in villagers)
        {
            if (v == null) continue;
            if (v.currentState == PersonState.PhoneAddiction ||
                v.currentState == PersonState.Destructive)
                continue;

            candidates.Add(v);
        }

        candidates.Sort((a, b) =>
        {
            float da = (a.transform.position - origin).sqrMagnitude;
            float db = (b.transform.position - origin).sqrMagnitude;
            return da.CompareTo(db);
        });

        int applied = 0;
        foreach (var v in candidates)
        {
            v.SetState(PersonState.Idle);
            applied++;
            if (applied >= streamingIdleCount)
                break;
        }
    }

    // ---------------- HUD HELPERS ----------------

    public int GetTotalVillagerCount()
    {
        CleanupVillagers();
        return villagers.Count;
    }

    public int GetPhoneAddictedCount()
    {
        CleanupVillagers();
        int count = 0;
        foreach (var v in villagers)
        {
            if (v != null && v.currentState == PersonState.PhoneAddiction)
                count++;
        }
        return count;
    }

    // Called by GameManager when the game is over
    public void OnGameOver()
    {
        CleanupVillagers();
        globalSpeedMultiplier = 0f;
    }
}
