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

            Villager v = Instantiate(villagerPrefab, spawnPos, Quaternion.identity);
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

    /// <summary>
    /// Called by PhoneDropManager when a phone lands on the ground.
    /// Chooses which villager will chase it.
    /// </summary>
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

            // Never let idle, addicted, or destructive villagers chase phones
            if (v.currentState == PersonState.PhoneAddiction ||
                v.currentState == PersonState.Destructive   ||
                v.currentState == PersonState.Idle)
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
    }

    /// <summary>
    /// Called by PhoneDropManager when a phone is removed
    /// (timeout, tapped, or villager pickup).
    /// </summary>
    public void OnPhoneCleared()
    {
        CleanupVillagers();

        // Reset any global slow-down that might be in effect.
        globalSpeedMultiplier = 1f;

        // Make sure nobody stays frozen because of a phone that no longer exists.
        foreach (var v in villagers)
        {
            if (v == null) continue;
            v.SetFrozenByPhone(false);
        }
    }

    /// <summary>
    /// Called by Villager when they successfully pick up a phone.
    /// </summary>
    public void OnVillagerPickedUpPhone(Villager collector, Phone phone)
    {
        CleanupVillagers();
        if (collector == null) return;

        if (phone == null)
        {
            // Fallback: no specific type, just become phone-addicted
            collector.SetPhoneAddicted();
        }
        else
        {
            switch (phone.phoneType)
            {
                case PhoneType.SocialMediaRed:
                    // 🔴 Social: villager becomes destructive (violent) instead of phone-addicted
                    collector.SetState(PersonState.Destructive);
                    break;

                case PhoneType.StreamingYellow:
                    // 🟡 Streaming: phone-addicted + nearby villagers idle
                    collector.SetPhoneAddicted();
                    ApplyStreamingEffect(collector.transform.position);
                    break;

                case PhoneType.MainstreamBlue:
                    // 🔵 Mainstream: phone-addicted + reset any speed penalty
                    collector.SetPhoneAddicted();
                    globalSpeedMultiplier = 1f;
                    break;

                case PhoneType.GamblingGreen:
                    // 🟢 Gambling: phone-addicted + starts destroying buildings
                    collector.SetPhoneAddicted();
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

        // Closest villagers to the phone become Idle
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

    // NEW: number of villagers still actively working
    public int GetWorkingVillagerCount()
    {
        CleanupVillagers();
        int count = 0;
        foreach (var v in villagers)
        {
            if (v != null && v.currentState == PersonState.Working)
                count++;
        }
        return count;
    }
    public int GetActiveVillagerCount()
    {
        CleanupVillagers();
        int count = 0;
        foreach (var v in villagers)
        {
            if (v == null) continue;

            if (v.currentState == PersonState.Working ||
                v.currentState == PersonState.ShiftingAttention)
            {
                count++;
            }
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
