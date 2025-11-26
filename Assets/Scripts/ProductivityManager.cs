using UnityEngine;

[System.Serializable]
public class ProductivityActivity
{
    public string label = "Activity";

    [Range(0f, 1f)]
    public float workforceAllocation = 0.33f;

    public float efficiencyMultiplier = 1f;
    public float storageCap = 100f;

    [Range(0f, 1f)]
    public float startingFillPercent = 0.25f;

    [Tooltip("How much is lost per second regardless of input productivity.")]
    public float passiveLossPerSecond = 1f;

    [SerializeField]
    private float storedProgress = 0f;

    public float StoredProgress => storedProgress;
    public float NormalizedProgress => storageCap <= 0f ? 0f : storedProgress / storageCap;

    public void Initialize()
    {
        storedProgress = Mathf.Clamp01(startingFillPercent) * Mathf.Max(0f, storageCap);
    }

    public void Tick(float productivity, float deltaTime)
    {
        if (storageCap <= 0f)
            return;

        if (passiveLossPerSecond > 0f && deltaTime > 0f)
        {
            storedProgress = Mathf.Max(0f, storedProgress - passiveLossPerSecond * deltaTime);
        }

        if (productivity <= 0f || deltaTime <= 0f)
            return;

        float gained = productivity * efficiencyMultiplier * deltaTime;
        storedProgress = Mathf.Clamp(storedProgress + gained, 0f, storageCap);
    }

    public float ConsumeProgress(float desiredAmount)
    {
        if (desiredAmount <= 0f || storedProgress <= 0f)
            return 0f;

        float consumed = Mathf.Min(storedProgress, desiredAmount);
        storedProgress -= consumed;
        return consumed;
    }
}

public class ProductivityManager : MonoBehaviour
{
    [Header("Productivity Settings")]
    public float baseProductivity = 100f;

    [Range(0f, 1f)]
    public float startingFactor = 1f;     // starts full strength

    [Range(0f, 1f)]
    public float phoneDecayFactor = 0.9f; // multiply per phone drop

    [Header("Work Allocation")]
    public ProductivityActivity buildingActivity = new ProductivityActivity
    {
        label = "Construction",
        workforceAllocation = 0.4f,
        efficiencyMultiplier = 0.5f,
        storageCap = 120f,
        startingFillPercent = 0.3f,
        passiveLossPerSecond = 2f
    };

    public ProductivityActivity farmingActivity = new ProductivityActivity
    {
        label = "Farming",
        workforceAllocation = 0.3f,
        efficiencyMultiplier = 0.7f,
        storageCap = 100f,
        startingFillPercent = 0.4f,
        passiveLossPerSecond = 1.5f
    };

    public ProductivityActivity infrastructureActivity = new ProductivityActivity
    {
        label = "Infrastructure",
        workforceAllocation = 0.3f,
        efficiencyMultiplier = 0.6f,
        storageCap = 100f,
        startingFillPercent = 0.5f,
        passiveLossPerSecond = 1f
    };

    [Header("Synergy Bonuses")]
    [Tooltip("How strongly farming reserves boost overall productivity.")]
    public float farmingBonusMultiplier = 0.5f;

    [Tooltip("How strongly infrastructure reserves boost overall productivity.")]
    public float infrastructureBonusMultiplier = 0.5f;

    [Header("Debug / Readonly")]
    [SerializeField]
    private float currentProductivity = 100f;
    public float CurrentProductivity => currentProductivity;

    [SerializeField]
    private float productivityFactor = 1f;
    public float ProductivityFactor
    {
        get => productivityFactor;
        private set => productivityFactor = Mathf.Clamp01(value);
    }

    private void Awake()
    {
        Initialize();
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameOver)
            return;

        SimulateActivities(Time.deltaTime);
    }

    public void Initialize()
    {
        ProductivityFactor = startingFactor;

        if (buildingActivity != null) buildingActivity.Initialize();
        if (farmingActivity != null) farmingActivity.Initialize();
        if (infrastructureActivity != null) infrastructureActivity.Initialize();

        UpdateProductivity();
    }

    /// <summary>
    /// Called by PhoneDropManager when a phone lands.
    /// </summary>
    public void ApplyPhoneDrop()
    {
        ProductivityFactor *= phoneDecayFactor;
        UpdateProductivity();
    }

    private void UpdateProductivity()
    {
        float multiplier = 1f;

        if (farmingActivity != null)
            multiplier += farmingBonusMultiplier * farmingActivity.NormalizedProgress;

        if (infrastructureActivity != null)
            multiplier += infrastructureBonusMultiplier * infrastructureActivity.NormalizedProgress;

        currentProductivity = baseProductivity * ProductivityFactor * multiplier;
        currentProductivity = Mathf.Max(0f, currentProductivity);
    }

    private void SimulateActivities(float deltaTime)
    {
        if (deltaTime <= 0f)
            return;

        float totalAllocation = 0f;
        if (buildingActivity != null) totalAllocation += buildingActivity.workforceAllocation;
        if (farmingActivity != null) totalAllocation += farmingActivity.workforceAllocation;
        if (infrastructureActivity != null) totalAllocation += infrastructureActivity.workforceAllocation;

        if (totalAllocation <= 0f || CurrentProductivity <= 0f)
        {
            // Only passive loss applies if there's no productivity to distribute
            buildingActivity?.Tick(0f, deltaTime);
            farmingActivity?.Tick(0f, deltaTime);
            infrastructureActivity?.Tick(0f, deltaTime);
            return;
        }

        float buildingShare = (buildingActivity != null ? buildingActivity.workforceAllocation : 0f) / totalAllocation;
        float farmingShare = (farmingActivity != null ? farmingActivity.workforceAllocation : 0f) / totalAllocation;
        float infrastructureShare = (infrastructureActivity != null ? infrastructureActivity.workforceAllocation : 0f) / totalAllocation;

        buildingActivity?.Tick(CurrentProductivity * buildingShare, deltaTime);
        farmingActivity?.Tick(CurrentProductivity * farmingShare, deltaTime);
        infrastructureActivity?.Tick(CurrentProductivity * infrastructureShare, deltaTime);

        UpdateProductivity();
    }

    // -------------------------------------------------------------------------
    // Normalized getters for UI
    // -------------------------------------------------------------------------

    public float GetFarmingNormalized() => farmingActivity != null ? farmingActivity.NormalizedProgress : 0f;
    public float GetInfrastructureNormalized() => infrastructureActivity != null ? infrastructureActivity.NormalizedProgress : 0f;
    public float GetBuildingNormalized() => buildingActivity != null ? buildingActivity.NormalizedProgress : 0f;

    // -------------------------------------------------------------------------
    // Band mapping (Option 1: Thriving / Declining / Collapse)
    // -------------------------------------------------------------------------

    public ProductivityBand GetBand()
    {
        if (CurrentProductivity >= 75f)
            return ProductivityBand.Thriving;

        if (CurrentProductivity > 0f)
            return ProductivityBand.Declining;

        return ProductivityBand.Collapse;
    }
}
