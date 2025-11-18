using System.Collections.Generic;
using UnityEngine;

public class PopulationManager : MonoBehaviour
{
    [Header("References")]
    public ProductivityManager productivityManager;
    public BuildingManager buildingManager;

    [Header("Villager Spawning")]
    public Villager villagerPrefab;
    public int initialVillagerCount = 20;
    public Vector2 spawnAreaCenter = Vector2.zero;
    public Vector2 spawnAreaSize = new Vector2(6f, 3f);

    [Header("Villagers (runtime)")]
    public List<Villager> villagers = new List<Villager>();

    [Header("Update Settings")]
    public float stateUpdateInterval = 1.5f;
    private float stateUpdateTimer = 0f;

    [Header("Global Effects")]
    [Range(0.2f, 2f)]
    public float globalSpeedMultiplier = 1f;

    [Header("Phone Effects")]
    public int socialMediaKillCount = 2;
    public int socialMediaRageCount = 2;
    public int streamingIdleCount = 3;
    public float movementSlowFactor = 0.8f;

    private void Start()
    {
        if (productivityManager == null)
            productivityManager = FindObjectOfType<ProductivityManager>();
        if (buildingManager == null)
            buildingManager = FindObjectOfType<BuildingManager>();

        InitializeVillagers();
    }

    private void Update()
    {
        if (productivityManager == null) return;
        if (GameManager.Instance != null && GameManager.Instance.isGameOver) return;

        stateUpdateTimer -= Time.deltaTime;
        if (stateUpdateTimer <= 0f)
        {
            stateUpdateTimer = stateUpdateInterval;
            UpdateVillagerStates();
        }

        CheckGameEndConditions();
    }

    // ---------- INIT / STATES ----------

    private void InitializeVillagers()
    {
        villagers.Clear();

        // Any pre-placed villagers
        Villager[] existing = FindObjectsOfType<Villager>();
        villagers.AddRange(existing);

        if (villagerPrefab == null)
        {
            if (villagers.Count == 0)
                Debug.LogWarning("PopulationManager: No villagerPrefab and no existing villagers.");
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

        foreach (var v in villagers)
        {
            if (v == null) continue;
            v.UpdateStateFromProductivity(band, currentProductivity);
        }
    }

    private void CleanupVillagers()
    {
        villagers.RemoveAll(v => v == null);
    }

    private void CheckGameEndConditions()
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameOver)
            return;

        CleanupVillagers();

        // Population is considered collapsed if there are NO villagers
        // OR all remaining villagers are Destructive.
        bool anyNonDestructive = false;
        foreach (var v in villagers)
        {
            if (v != null && v.currentState != PersonState.Destructive)
            {
                anyNonDestructive = true;
                break;
            }
        }
        bool populationCollapsed = !anyNonDestructive;  // 0 villagers OR all violent

        // Buildings destroyed?
        bool allBuildingsDestroyed = false;
        if (buildingManager != null && buildingManager.buildings != null && buildingManager.buildings.Count > 0)
        {
            allBuildingsDestroyed = true;
            foreach (var b in buildingManager.buildings)
            {
                if (b != null && b.currentState != BuildingState.Destroyed)
                {
                    allBuildingsDestroyed = false;
                    break;
                }
            }
        }

        if (populationCollapsed || allBuildingsDestroyed)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerGameOver();
            }
        }
    }

    public void OnGameOver()
    {
        foreach (var v in villagers)
        {
            if (v != null)
                v.SetState(PersonState.Destructive);
        }
    }

    // ---------- PHONE INTERACTION (SHARED) ----------

    // A phone just spawned / is falling
    public void OnPhoneDropped(Phone phone)
    {
        CleanupVillagers();
        if (phone == null || villagers.Count == 0) return;

        Villager chaser = null;
        float bestDistSq = float.MaxValue;

        foreach (var v in villagers)
        {
            if (v == null) continue;

            // Ignore already lost ones
            if (v.currentState == PersonState.PhoneAddiction ||
                v.currentState == PersonState.Destructive)
                continue;

            float dSq = (v.transform.position - phone.transform.position).sqrMagnitude;
            if (dSq < bestDistSq)
            {
                bestDistSq = dSq;
                chaser = v;
            }
        }

        foreach (var v in villagers)
        {
            if (v == null) continue;

            // Workers keep working; everyone else freezes
            if (v == chaser)
            {
                v.SetFrozenByPhone(false);
                v.BecomePhoneChaser(phone);
            }
            else if (v.currentState != PersonState.Working &&
                     v.currentState != PersonState.PhoneAddiction &&
                     v.currentState != PersonState.Destructive)
            {
                v.SetFrozenByPhone(true);
            }
        }
    }

    // Phone vanished without pickup
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

    // Villager successfully grabbed the phone
    public void OnVillagerPickedUpPhone(Villager collector, Phone phone)
    {
        foreach (var v in villagers)
        {
            if (v == null) continue;

            if (v == collector)
            {
                v.SetPhoneAddicted();
            }
            else if (v.currentState != PersonState.PhoneAddiction &&
                     v.currentState != PersonState.Destructive)
            {
                v.SetFrozenByPhone(false);
            }
        }
    }

    // ---------- PHONE TYPE–SPECIFIC EFFECTS ----------

    // Red phone – some die, some go violent
    public void ApplySocialMediaPhone(Phone phone)
    {
        CleanupVillagers();
        if (villagers.Count == 0) return;

        List<Villager> pool = new List<Villager>(villagers);
        for (int i = 0; i < pool.Count - 1; i++)
        {
            int j = Random.Range(i, pool.Count);
            var tmp = pool[i];
            pool[i] = pool[j];
            pool[j] = tmp;
        }

        int kill = Mathf.Min(socialMediaKillCount, pool.Count);
        for (int i = 0; i < kill; i++)
        {
            Villager v = pool[i];
            if (v == null) continue;
            villagers.Remove(v);
            Destroy(v.gameObject);
        }

        int startRageIndex = kill;
        int possibleRagers = pool.Count - kill;
        int rage = Mathf.Min(socialMediaRageCount, possibleRagers);
        for (int i = 0; i < rage; i++)
        {
            Villager v = pool[startRageIndex + i];
            if (v == null) continue;
            v.SetState(PersonState.Destructive);
        }
    }

    // Yellow phone – nearest few become idle/binging
    public void ApplyStreamingPhone(Phone phone)
    {
        CleanupVillagers();
        if (villagers.Count == 0) return;

        Vector3 phonePos = phone != null ? phone.transform.position : Vector3.zero;
        List<Villager> candidates = new List<Villager>();

        foreach (var v in villagers)
        {
            if (v == null) continue;
            if (v.currentState == PersonState.PhoneAddiction ||
                v.currentState == PersonState.Destructive)
                continue;
            candidates.Add(v);
        }

        if (candidates.Count == 0) return;

        candidates.Sort((a, b) =>
        {
            float da = (a.transform.position - phonePos).sqrMagnitude;
            float db = (b.transform.position - phonePos).sqrMagnitude;
            return da.CompareTo(db);
        });

        int count = Mathf.Min(streamingIdleCount, candidates.Count);
        for (int i = 0; i < count; i++)
        {
            candidates[i].SetState(PersonState.Idle);
        }
    }

    // Blue phone – everyone slows down
    public void ApplyMainstreamMediaPhone()
    {
        globalSpeedMultiplier *= movementSlowFactor;
        globalSpeedMultiplier = Mathf.Clamp(globalSpeedMultiplier, 0.2f, 1f);
    }

    // Green phone – one villager becomes a building destroyer
    public void ApplyGamblingPhone(Phone phone)
    {
        CleanupVillagers();
        if (villagers.Count == 0) return;

        Vector3 phonePos = phone != null ? phone.transform.position : Vector3.zero;
        Villager chosen = null;
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
                chosen = v;
            }
        }

        if (chosen != null)
            chosen.BecomeBuildingDestroyer();
    }

    // ---------- STATS ----------

    public int GetTotalVillagerCount()
    {
        int count = 0;
        foreach (var v in villagers)
        {
            if (v != null) count++;
        }
        return count;
    }

    public int GetPhoneAddictedCount()
    {
        int count = 0;
        foreach (var v in villagers)
        {
            if (v != null && v.currentState == PersonState.PhoneAddiction)
                count++;
        }
        return count;
    }
}
