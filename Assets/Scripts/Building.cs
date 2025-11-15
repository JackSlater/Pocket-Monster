using UnityEngine;

public class Building : MonoBehaviour
{
    public string buildingName = "Structure";
    public BuildingState currentState = BuildingState.UnderConstruction;

    [Header("Construction")]
    public float constructionRequirement = 100f;
    public float constructionProgress = 0f;

    [Header("Visuals")]
    public SpriteRenderer spriteRenderer;
    public Vector3 minBuiltScale = new Vector3(1f, 0.2f, 1f);
    public Vector3 maxBuiltScale = new Vector3(1f, 1f, 1f);

    public Color underConstructionColor = Color.gray;
    public Color finishedColor = Color.green;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        UpdateVisuals();
    }

    /// <summary>
    /// Called every frame by BuildingManager.
    /// </summary>
    public void Tick(float buildingEffort, float infrastructureSupport, float deltaTime, ProductivityBand band)
    {
        // Only care about construction for now.
        if (currentState == BuildingState.Destroyed)
        {
            UpdateVisuals();
            return;
        }

        if (currentState == BuildingState.UnderConstruction)
        {
            // Add effort – ignore band/decay/etc. for now
            constructionProgress += buildingEffort;
            constructionProgress = Mathf.Clamp(constructionProgress, 0f, constructionRequirement);

            if (constructionProgress >= constructionRequirement)
            {
                constructionProgress = constructionRequirement;
                currentState = BuildingState.Thriving; // treat as "finished"
            }
        }

        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        // 0–1 how far along construction is
        float build01 = constructionRequirement > 0f
            ? Mathf.Clamp01(constructionProgress / constructionRequirement)
            : 1f;

        // Scale based on progress
        transform.localScale = Vector3.Lerp(minBuiltScale, maxBuiltScale, build01);

        if (spriteRenderer == null) return;

        // Color: gray while building, green when done
        spriteRenderer.color = currentState == BuildingState.UnderConstruction
            ? underConstructionColor
            : finishedColor;
    }

    // NOTE: only ONE ForceCollapse definition here.
    public void ForceCollapse()
    {
        currentState = BuildingState.Destroyed;
        constructionProgress = 0f;
        UpdateVisuals();
    }
    private void Update()
    {
        // TEMP: ignore managers, just grow at a fixed rate
        float dt = Time.deltaTime;

        if (currentState == BuildingState.UnderConstruction)
        {
            constructionProgress += 30f * dt;   // 30 progress per second
            constructionProgress = Mathf.Clamp(constructionProgress, 0f, constructionRequirement);

            if (constructionProgress >= constructionRequirement)
            {
                constructionProgress = constructionRequirement;
                currentState = BuildingState.Thriving;
            }

            UpdateVisuals();
        }
    }
}
