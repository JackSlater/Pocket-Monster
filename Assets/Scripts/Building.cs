using UnityEngine;

public class Building : MonoBehaviour
{
    public string buildingName = "Structure";
    public BuildingState currentState = BuildingState.UnderConstruction;

    [Header("Construction")]
    public float constructionRequirement = 100f;
    public float constructionProgress = 0f;

    [Header("Visuals")]
    // Child sprite
    public SpriteRenderer spriteRenderer;

    [Tooltip("Child transform that gets scaled upward (usually same object as spriteRenderer).")]
    public Transform visualRoot;

    public Vector3 minBuiltScale = new Vector3(1f, 0.2f, 1f);
    public Vector3 maxBuiltScale = new Vector3(1f, 1f, 1f);

    [Header("Colors")]
    public Color underConstructionColor = Color.gray;
    public Color finishedColor = Color.green;

    [Header("Vertical Offset")]
    [Tooltip("Extra offset applied to the whole building visual (use this to nudge it up/down relative to the ground).")]
    public float baseOffsetY = 0f;

    // Sprite half-height in local units
    [SerializeField, HideInInspector]
    private float spriteHalfHeightLocal = 0f;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (visualRoot == null && spriteRenderer != null)
            visualRoot = spriteRenderer.transform;

        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            spriteHalfHeightLocal = spriteRenderer.sprite.bounds.extents.y;
        }

        UpdateVisuals();
    }

    private void Start()
    {
        // Register with manager
        BuildingManager manager = FindObjectOfType<BuildingManager>();
        if (manager != null)
        {
            manager.RegisterBuilding(this);
        }
    }

    public void Tick(float buildingEffort, float infrastructureSupport, float deltaTime, ProductivityBand band)
    {
        if (currentState == BuildingState.Destroyed)
        {
            UpdateVisuals();
            return;
        }

        if (currentState == BuildingState.UnderConstruction && buildingEffort > 0f)
        {
            constructionProgress += buildingEffort;
            constructionProgress = Mathf.Clamp(constructionProgress, 0f, constructionRequirement);

            if (constructionProgress >= constructionRequirement)
            {
                constructionProgress = constructionRequirement;
                currentState = BuildingState.Thriving;
            }
        }

        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        float build01 = constructionRequirement > 0f
            ? Mathf.Clamp01(constructionProgress / constructionRequirement)
            : 1f;

        if (visualRoot != null)
        {
            Vector3 visualScale = Vector3.Lerp(minBuiltScale, maxBuiltScale, build01);
            visualRoot.localScale = visualScale;

            if (spriteHalfHeightLocal > 0f)
            {
                // Keep base at parent Y, plus optional extra offset
                float offsetY = spriteHalfHeightLocal * visualScale.y + baseOffsetY;
                Vector3 lp = visualRoot.localPosition;
                lp.y = offsetY;
                visualRoot.localPosition = lp;
            }
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = (currentState == BuildingState.UnderConstruction)
                ? underConstructionColor
                : finishedColor;
        }
    }

    public void ForceCollapse()
    {
        currentState = BuildingState.Destroyed;
        constructionProgress = 0f;
        UpdateVisuals();
    }

    public void InitializeAsNewConstruction()
    {
        currentState = BuildingState.UnderConstruction;
        constructionProgress = 0f;
        UpdateVisuals();
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(transform.position, 0.1f);
    }
}
