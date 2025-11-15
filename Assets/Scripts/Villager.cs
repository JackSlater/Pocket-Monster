using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Villager : MonoBehaviour
{
    public PersonState currentState = PersonState.Working;

    [Header("Movement")]
    [Tooltip("How fast the villager walks towards a building.")]
    public float moveSpeed = 1.5f;

    [Tooltip("Distance at which we consider the villager to be 'at' the building.")]
    public float closeEnoughDistance = 0.05f;

    [Header("Construction Contribution")]
    [Tooltip("How much extra construction effort this villager contributes per second when working.")]
    public float workEffortPerSecond = 10f;

    private Building currentTargetBuilding;

    [Header("References")]
    public Animator animator;              // optional
    public SpriteRenderer spriteRenderer;  // required
    public BuildingManager buildingManager;
    public ProductivityManager productivityManager;

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
        }

        if (currentTargetBuilding == null)
        {
            // Nothing to build right now
            SetState(PersonState.Idle);
            return;
        }

        // Move toward the target building
        Vector3 targetPos = currentTargetBuilding.transform.position;
        Vector3 delta = targetPos - transform.position;
        float dist = delta.magnitude;

        if (dist > closeEnoughDistance)
        {
            Vector3 dir = delta.normalized;
            transform.position += dir * moveSpeed * dt;
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
                // Let the movement/working logic handle exact state,
                // but nudge away from Idle.
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
                spriteRenderer.color = Color.green;
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
                spriteRenderer.color = Color.red;
                break;
        }
    }
}
