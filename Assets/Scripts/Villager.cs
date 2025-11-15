using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Villager : MonoBehaviour
{
    public PersonState currentState = PersonState.Working;

    [Header("Movement")]
    [Tooltip("Average movement speed; each villager will randomize around this.")]
    public float baseMoveSpeed = 1.5f;

    [Tooltip("How much individual move speed can vary (+/-).")]
    public float moveSpeedVariation = 0.75f;

    [Tooltip("Distance at which we consider the villager to be 'at' the building.")]
    public float closeEnoughDistance = 0.15f;

    [Header("Construction Contribution")]
    [Tooltip("How much construction effort this villager contributes per second when working.")]
    public float workEffortPerSecond = 10f;

    [Tooltip("Horizontal offset from the center of the building when working.")]
    public float workOffsetDistance = 0.4f;

    [Tooltip("Vertical jitter when choosing a work offset.")]
    public float workOffsetVerticalJitter = 0.1f;

    [Header("Visual Variety")]
    [Tooltip("How much villager height can vary (+/- as a fraction).")]
    public float heightVariation = 0.2f;

    [Header("References")]
    public Animator animator;              // optional
    public SpriteRenderer spriteRenderer;  // required
    public BuildingManager buildingManager;
    public ProductivityManager productivityManager;

    // --- private runtime fields ---
    private float actualMoveSpeed;
    private Building currentTargetBuilding;
    private Vector3 currentWorkOffset;

    private void Reset()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (buildingManager == null)
            buildingManager = FindObjectOfType<BuildingManager>();

        if (productivityManager == null)
            productivityManager = FindObjectOfType<ProductivityManager>();

        // Randomize movement speed per villager
        float randomized = baseMoveSpeed + Random.Range(-moveSpeedVariation, moveSpeedVariation);
        actualMoveSpeed = Mathf.Max(0.1f, randomized);  // avoid zero/negative

        // Randomize height (scale Y) per villager
        Vector3 scale = transform.localScale;
        float heightFactor = 1f + Random.Range(-heightVariation, heightVariation);
        scale.y *= Mathf.Max(0.1f, heightFactor);
        transform.localScale = scale;

        UpdateVisuals();
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        HandleWorkLogic(dt);
    }

    // ----------------- WORK & MOVEMENT LOGIC -----------------

    private void HandleWorkLogic(float dt)
    {
        if (buildingManager == null) return;

        // Make sure we have a valid building target
        if (currentTargetBuilding == null || currentTargetBuilding.currentState != BuildingState.UnderConstruction)
        {
            currentTargetBuilding = FindNearestUnderConstructionBuilding();
            AssignWorkOffsetForCurrentBuilding();
        }

        if (currentTargetBuilding == null)
        {
            // Nothing to build right now
            SetState(PersonState.Idle);
            return;
        }

        // Move toward the target building + our personal offset
        Vector3 targetPos = currentTargetBuilding.transform.position + currentWorkOffset;
        Vector3 delta = targetPos - transform.position;
        float dist = delta.magnitude;

        if (dist > closeEnoughDistance)
        {
            Vector3 dir = delta.normalized;
            transform.position += dir * actualMoveSpeed * dt;
            SetState(PersonState.ShiftingAttention);   // walking to work
        }
        else
        {
            // At the building: contribute construction effort
            float effort = workEffortPerSecond * dt;

            ProductivityBand band = ProductivityBand.Thriving;
            if (productivityManager != null)
            {
                band = productivityManager.GetBand();
            }

            // Optionally scale villager output by productivity band
            float bandMultiplier = 1f;
            switch (band)
            {
                case ProductivityBand.Thriving:
                    bandMultiplier = 1f;
                    break;
                case ProductivityBand.Declining:
                    bandMultiplier = 0.5f;
                    break;
                case ProductivityBand.Collapse:
                    bandMultiplier = 0.2f;
                    break;
            }

            effort *= bandMultiplier;

            // Directly tick the building with this villager's effort
            currentTargetBuilding.Tick(effort, 0f, dt, band);

            SetState(PersonState.Working);
        }
    }

    private Building FindNearestUnderConstructionBuilding()
    {
        if (buildingManager == null || buildingManager.buildings == null)
            return null;

        Building best = null;
        float bestDistSq = float.MaxValue;

        foreach (var b in buildingManager.buildings)
        {
            if (b == null) continue;
            if (b.currentState != BuildingState.UnderConstruction) continue;

            float dSq = (b.transform.position - transform.position).sqrMagnitude;
            if (dSq < bestDistSq)
            {
                bestDistSq = dSq;
                best = b;
            }
        }

        return best;
    }

    /// <summary>
    /// Pick a side & slight vertical jitter for where THIS villager stands relative to its current target.
    /// This makes them appear on both sides of the building instead of stacking perfectly.
    /// </summary>
    private void AssignWorkOffsetForCurrentBuilding()
    {
        if (currentTargetBuilding == null)
        {
            currentWorkOffset = Vector3.zero;
            return;
        }

        float side = Random.value < 0.5f ? -1f : 1f; // left or right
        float xOffset = side * workOffsetDistance;
        float yOffset = Random.Range(-workOffsetVerticalJitter, workOffsetVerticalJitter);

        currentWorkOffset = new Vector3(xOffset, yOffset, 0f);
    }

    // ----------------- STATE / VISUALS -----------------

    public void SetState(PersonState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        UpdateVisuals();
    }

    // Called by PopulationManager to react to overall productivity.
    public void UpdateStateFromProductivity(ProductivityBand band, float currentProductivity)
    {
        // Don't override strong states
        if (currentState == PersonState.PhoneAddiction || currentState == PersonState.Destructive)
            return;

        switch (band)
        {
            case ProductivityBand.Thriving:
                if (currentState == PersonState.Idle)
                    SetState(PersonState.Working);
                break;

            case ProductivityBand.Declining:
                SetState(PersonState.ShiftingAttention);
                break;

            case ProductivityBand.Collapse:
                SetState(PersonState.Idle);
                break;
        }
    }

    private void UpdateVisuals()
    {
        // Animator (if you have one)
        if (animator != null)
        {
            animator.SetInteger("State", (int)currentState);
        }

        // Color feedback
        if (spriteRenderer == null) return;

        switch (currentState)
        {
            case PersonState.Working:
                spriteRenderer.color = Color.red;   // working = red, as you asked earlier
                break;
            case PersonState.ShiftingAttention:
                spriteRenderer.color = Color.yellow;
                break;
            case PersonState.PhoneAddiction:
                spriteRenderer.color = new Color(1f, 0.5f, 0f); // orange
                break;
            case PersonState.Idle:
                spriteRenderer.color = Color.gray;
                break;
            case PersonState.Destructive:
                spriteRenderer.color = Color.black;
                break;
        }
    }
}
