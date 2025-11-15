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

    private void Start()
    {
        // Auto-register with the BuildingManager at runtime
        BuildingManager manager = FindObjectOfType<BuildingManager>();
        if (manager != null)
        {
            manager.RegisterBuilding(this);
        }
    }

    /// <summary>
    /// Called once per frame by BuildingManager.
    /// buildingEffort is already scaled by deltaTime.
    /// </summary>
    public void Tick(float buildingEffort, float infrastructureSupport, float deltaTime, ProductivityBand band)
    {
        // If destroyed, it doesn’t build anymore
        if (currentState == BuildingState.Destroyed)
        {
            UpdateVisuals();
            return;
        }

        if (currentState == BuildingState.UnderConstruction)
        {
            if (buildingEffort > 0f)
            {
                constructionProgress += buildingEffort;
                constructionProgress = Mathf.Clamp(constructionProgress, 0f, constructionRequirement);

                if (constructionProgress >= constructionRequirement)
                {
                    constructionProgress = constructionRequirement;
                    currentState = BuildingState.Thriving;
                }
            }
        }

        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        // 0–1 progress
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

    public void ForceCollapse()
    {
        currentState = BuildingState.Destroyed;
        constructionProgress = 0f;
        UpdateVisuals();
    }
}
