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
        workforceAllocation = 0.35f,
        efficiencyMultiplier = 0.6f,
        storageCap = 150f,
        startingFillPercent = 0.5f,
        passiveLossPerSecond = 1f
    };

    public ProductivityActivity infrastructureActivity = new ProductivityActivity
    {
        label = "Infrastructure",
        workforceAllocation = 0.25f,
        efficiencyMultiplier = 0.45f,
        storageCap = 100f,
        startingFillPercent = 0.25f,
        passiveLossPerSecond = 1.5f
    };

    [Header("Activity Feedback")]
    public float farmingBonusMultiplier = 0.25f;
    public float infrastructureBonusMultiplier = 0.25f;

    public float CurrentProductivity { get; private set; }
    public float ProductivityFactor  { get; private set; }

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
        buildingActivity.Initialize();
        farmingActivity.Initialize();
        infrastructureActivity.Initialize();
        UpdateProductivity();
    }

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

        CurrentProductivity = baseProductivity * ProductivityFactor * multiplier;
        CurrentProductivity = Mathf.Max(0f, CurrentProductivity);
    }

    private void SimulateActivities(float deltaTime)
    {
        if (deltaTime <= 0f)
            return;

        float totalAllocation = buildingActivity.workforceAllocation +
                                farmingActivity.workforceAllocation +
                                infrastructureActivity.workforceAllocation;

        if (totalAllocation <= 0f)
        {
            buildingActivity.Tick(0f, deltaTime);
            farmingActivity.Tick(0f, deltaTime);
            infrastructureActivity.Tick(0f, deltaTime);
            return;
        }

        float buildingShare = buildingActivity.workforceAllocation / totalAllocation;
        float farmingShare = farmingActivity.workforceAllocation / totalAllocation;
        float infrastructureShare = infrastructureActivity.workforceAllocation / totalAllocation;

        buildingActivity.Tick(CurrentProductivity * buildingShare, deltaTime);
        farmingActivity.Tick(CurrentProductivity * farmingShare, deltaTime);
        infrastructureActivity.Tick(CurrentProductivity * infrastructureShare, deltaTime);

        UpdateProductivity();
    }

    public float ConsumeBuildingProgress(float desiredAmount)
    {
        return buildingActivity.ConsumeProgress(desiredAmount);
    }

    public float ConsumeInfrastructureProgress(float desiredAmount)
    {
        return infrastructureActivity.ConsumeProgress(desiredAmount);
    }

    public float ConsumeFarmingProgress(float desiredAmount)
    {
        return farmingActivity.ConsumeProgress(desiredAmount);
    }

    public float GetFarmingNormalized() => farmingActivity.NormalizedProgress;
    public float GetInfrastructureNormalized() => infrastructureActivity.NormalizedProgress;
    public float GetBuildingNormalized() => buildingActivity.NormalizedProgress;

    public ProductivityBand GetBand()
    {
        if (CurrentProductivity >= 75f)
            return ProductivityBand.Thriving;
        if (CurrentProductivity > 0f)
            return ProductivityBand.Declining;
        return ProductivityBand.Collapse;
    }
}
