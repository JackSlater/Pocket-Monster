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
        // Try to auto-find sprite/visual on first use
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (visualRoot == null && spriteRenderer != null)
        {
            visualRoot = spriteRenderer.transform;
        }

        CacheSpriteHalfHeight();
        UpdateVisuals();
    }

    private void CacheSpriteHalfHeight()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            spriteHalfHeightLocal = 0f;
            return;
        }

        var sprite = spriteRenderer.sprite;
        float pixelsPerUnit = sprite.pixelsPerUnit;
        float halfHeightPixels = sprite.rect.height * 0.5f;
        spriteHalfHeightLocal = halfHeightPixels / pixelsPerUnit;
    }

    private void OnValidate()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (visualRoot == null && spriteRenderer != null)
        {
            visualRoot = spriteRenderer.transform;
        }

        CacheSpriteHalfHeight();
        UpdateVisuals();
    }

    /// <summary>
    /// Tick-based construction logic driven by ProductivityManager.
    /// </summary>
    public void Tick(float buildingEffort, float infrastructureSupport, float deltaTime, ProductivityBand band)
    {
        if (currentState == BuildingState.Destroyed)
        {
            UpdateVisuals();
            return;
        }

        float bandMultiplier = 1f;
        switch (band)
        {
            case ProductivityBand.Thriving:
                bandMultiplier = 1.2f;
                break;

            case ProductivityBand.Declining:
                bandMultiplier = 0.8f;
                break;

            case ProductivityBand.Collapse:
                bandMultiplier = 0.4f;
                break;
        }

        float effectiveEffort = buildingEffort * bandMultiplier * infrastructureSupport;
        constructionProgress += effectiveEffort * deltaTime;
        constructionProgress = Mathf.Clamp(constructionProgress, 0f, constructionRequirement);

        if (constructionProgress >= constructionRequirement &&
            currentState == BuildingState.UnderConstruction)
        {
            currentState = BuildingState.Completed;
        }

        UpdateVisuals();
    }

    /// <summary>
    /// Direct contribution from nearby villagers.
    /// </summary>
    public void AddConstructionProgress(float amount)
    {
        if (currentState != BuildingState.UnderConstruction)
            return;

        constructionProgress += amount;
        constructionProgress = Mathf.Clamp(constructionProgress, 0f, constructionRequirement);

        if (constructionProgress >= constructionRequirement)
        {
            currentState = BuildingState.Completed;
        }

        UpdateVisuals();
    }

    /// <summary>
    /// Called by destructive villagers (green phone behavior).
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (currentState != BuildingState.Completed)
            return;

        // Simple destroy-on-hit model for now
        currentState = BuildingState.Destroyed;
        constructionProgress = 0f;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (visualRoot == null && spriteRenderer != null)
        {
            visualRoot = spriteRenderer.transform;
        }

        if (visualRoot != null)
        {
            float t = Mathf.Clamp01(constructionProgress / Mathf.Max(1f, constructionRequirement));
            Vector3 targetScale = Vector3.Lerp(minBuiltScale, maxBuiltScale, t);
            visualRoot.localScale = targetScale;

            // Keep base of building on the ground
            Vector3 pos = visualRoot.localPosition;
            pos.y = baseOffsetY + spriteHalfHeightLocal * targetScale.y;
            visualRoot.localPosition = pos;
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
