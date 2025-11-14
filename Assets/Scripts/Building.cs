using UnityEngine;

public class Building : MonoBehaviour
{
    public string buildingName = "Structure";
    public BuildingState currentState = BuildingState.UnderConstruction;

    [Header("Construction")]
    public float constructionRequirement = 100f;
    public float constructionProgress = 0f;
    public float constructionDecayPerSecond = 0.5f;

    [Header("Maintenance")]
    public float maxHealth = 100f;
    public float currentHealth = 50f;
    public float passiveDecayPerSecond = 1f;
    public float infrastructureSupportMultiplier = 0.5f;

    [Header("Productivity Influence")]
    public float thrivingBonusPerSecond = 2f;
    public float decliningPenaltyPerSecond = 1f;
    public float collapsePenaltyPerSecond = 4f;

    public bool IsConstructed => currentState != BuildingState.UnderConstruction;

    public void Tick(float buildingEffort, float infrastructureSupport, float deltaTime, ProductivityBand productivityBand)
    {
        if (currentState == BuildingState.Destroyed)
            return;

        ApplyProductivityInfluence(productivityBand, deltaTime);

        if (currentState == BuildingState.UnderConstruction)
        {
            ApplyConstruction(buildingEffort, deltaTime);
        }
        else
        {
            MaintainStructure(buildingEffort, infrastructureSupport, deltaTime);
        }
    }

    private void ApplyConstruction(float buildingEffort, float deltaTime)
    {
        if (constructionRequirement <= 0f)
        {
            CompleteConstruction();
            return;
        }

        if (constructionDecayPerSecond > 0f)
        {
            constructionProgress = Mathf.Max(0f, constructionProgress - constructionDecayPerSecond * deltaTime);
        }

        constructionProgress += buildingEffort;
        if (constructionProgress >= constructionRequirement)
        {
            CompleteConstruction();
        }
    }

    private void CompleteConstruction()
    {
        currentState = BuildingState.Thriving;
        constructionProgress = constructionRequirement;
        currentHealth = Mathf.Clamp(maxHealth * 0.75f, 0f, maxHealth);
    }

    private void MaintainStructure(float buildingEffort, float infrastructureSupport, float deltaTime)
    {
        float totalSupport = buildingEffort + (infrastructureSupport * infrastructureSupportMultiplier);

        if (passiveDecayPerSecond > 0f)
        {
            currentHealth = Mathf.Max(0f, currentHealth - passiveDecayPerSecond * deltaTime);
        }

        currentHealth = Mathf.Clamp(currentHealth + totalSupport, 0f, maxHealth);
        UpdateStateFromHealth();
    }

    private void ApplyProductivityInfluence(ProductivityBand band, float deltaTime)
    {
        float delta = 0f;

        switch (band)
        {
            case ProductivityBand.Thriving:
                delta = thrivingBonusPerSecond;
                break;
            case ProductivityBand.Declining:
                delta = -decliningPenaltyPerSecond;
                break;
            case ProductivityBand.Collapse:
                delta = -collapsePenaltyPerSecond;
                break;
        }

        delta *= deltaTime;

        if (currentState == BuildingState.UnderConstruction)
        {
            constructionProgress = Mathf.Clamp(constructionProgress + delta, 0f, constructionRequirement);
            if (constructionProgress >= constructionRequirement)
                CompleteConstruction();
        }
        else
        {
            currentHealth = Mathf.Clamp(currentHealth + delta, 0f, maxHealth);
            UpdateStateFromHealth();
        }
    }

    private void UpdateStateFromHealth()
    {
        if (currentState == BuildingState.Destroyed)
            return;

        if (currentHealth >= maxHealth * 0.75f)
        {
            currentState = BuildingState.Thriving;
        }
        else if (currentHealth >= maxHealth * 0.4f)
        {
            currentState = BuildingState.Declining;
        }
        else if (currentHealth > 0f)
        {
            currentState = BuildingState.Ruined;
        }
        else
        {
            ForceCollapse();
        }
    }

    public void UpdateStateFromProductivity(ProductivityBand band)
    {
        ApplyProductivityInfluence(band, Time.deltaTime);
    }

    public void ForceCollapse()
    {
        currentState = BuildingState.Destroyed;
        currentHealth = 0f;
        Debug.Log($"{buildingName} has collapsed.");
    }
}
