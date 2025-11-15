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

    [Tooltip("Distance at which we consider the villager to be 'at' a target (building/phone).")]
    public float closeEnoughDistance = 0.15f;

    [Header("Construction Contribution")]
    [Tooltip("How much construction effort this villager contributes per second when working.")]
    public float workEffortPerSecond = 10f;

    [Header("Work Position Offsets")]
    [Tooltip("Horizontal offset from the center of the building when working.")]
    public float workOffsetDistance = 0.4f;

    [Tooltip("Vertical jitter when choosing a work offset.")]
    public float workOffsetVerticalJitter = 0.1f;

    [Header("Visual Variety")]
    [Tooltip("How much villager height can vary (+/- as a fraction).")]
    public float heightVariation = 0.2f;

    [Header("Ground")]
    [Tooltip("If true, villager will always stay on this Y position (walks along the ground).")]
    public bool lockToGround = true;

    [Tooltip("Y position of the ground line.")]
    public float groundY = -0.5f;

    [Header("References")]
    public Animator animator;              // optional
    public SpriteRenderer spriteRenderer;  // required
    public BuildingManager buildingManager;
    public ProductivityManager productivityManager;

    // --- private runtime fields ---
    private float actualMoveSpeed;
    private Building currentTargetBuilding;
    private Vector3 currentWorkOffset;

    // Phone-related
    private bool frozenByPhone = false;    // true: stop moving/working while a phone event is active
    private bool isPhoneChaser = false;    // true: this villager is the one going for the phone
    private Phone targetPhone = null;

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

        // If we lock to ground and no explicit groundY set, use our starting Y
        if (lockToGround)
        {
            // You can comment this out if you ALWAYS want -0.5
            // groundY = transform.position.y;
        }

        // Randomize movement speed per villager
        float randomized = baseMoveSpeed + Random.Range(-moveSpeedVariation, moveSpeedVariation);
        actualMoveSpeed = Mathf.Max(0.1f, randomized);  // avoid zero/negative

        // Randomize height (scale Y) per villager
        Vector3 scale = transform.localScale;
        float heightFactor = 1f + Random.Range(-heightVariation, heightVariation);
        scale.y *= Mathf.Max(0.1f, heightFactor);
        transform.localScale = scale;

        // Snap to ground at start if needed
        if (lockToGround)
        {
            Vector3 pos = transform.position;
            pos.y = groundY;
            transform.position = pos;
        }

        UpdateVisuals();
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        if (currentState == PersonState.Destructive)
            return;

        if (currentState == PersonState.PhoneAddiction)
            return;

        // Phone chaser logic first
        if (isPhoneChaser && targetPhone != null && targetPhone.isActive)
        {
            HandlePhoneChase(dt);
        }
        else if (frozenByPhone)
        {
            SetState(PersonState.ShiftingAttention);
        }
        else
        {
            HandleWorkLogic(dt);
        }

        // Final safety: keep them on the ground after any movement
        if (lockToGround)
        {
            Vector3 pos = transform.position;
            pos.y = groundY;
            transform.position = pos;
        }
    }

    // ----------------- PHONE LOGIC -----------------

    private void HandlePhoneChase(float dt)
    {
        if (targetPhone == null || !targetPhone.isActive)
        {
            isPhoneChaser = false;
            return;
        }

        Vector3 targetPos = targetPhone.transform.position;

        // Force phone target to be on ground line for movement
        if (lockToGround)
        {
            targetPos.y = groundY;
        }

        Vector3 delta = targetPos - transform.position;
        float dist = delta.magnitude;

        if (dist > closeEnoughDistance)
        {
            Vector3 dir = delta.normalized;
            Vector3 newPos = transform.position + dir * actualMoveSpeed * dt;

            if (lockToGround)
                newPos.y = groundY;

            transform.position = newPos;
            SetState(PersonState.ShiftingAttention);
        }
        else
        {
            // Reached the phone: pick it up and become addicted
            targetPhone.DisablePhone();

            PopulationManager pop = FindObjectOfType<PopulationManager>();
            if (pop != null)
            {
                pop.OnVillagerPickedUpPhone(this, targetPhone);
            }

            isPhoneChaser = false;
            targetPhone = null;
        }
    }

    public void BecomePhoneChaser(Phone phone)
    {
        targetPhone = phone;
        isPhoneChaser = true;
        frozenByPhone = false;  // chaser is allowed to move
    }

    public void SetFrozenByPhone(bool frozen)
    {
        frozenByPhone = frozen;
    }

    public void SetPhoneAddicted()
    {
        currentState = PersonState.PhoneAddiction;
        UpdateVisuals();
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
            SetState(PersonState.Idle);
            return;
        }

        Vector3 targetPos = currentTargetBuilding.transform.position + currentWorkOffset;

        // Keep target on ground line
        if (lockToGround)
        {
            targetPos.y = groundY;
        }

        Vector3 delta = targetPos - transform.position;
        float dist = delta.magnitude;

        if (dist > closeEnoughDistance)
        {
            Vector3 dir = delta.normalized;
            Vector3 newPos = transform.position + dir * actualMoveSpeed * dt;

            if (lockToGround)
                newPos.y = groundY;

            transform.position = newPos;
            SetState(PersonState.ShiftingAttention);
        }
        else
        {
            float effort = workEffortPerSecond * dt;

            ProductivityBand band = ProductivityBand.Thriving;
            if (productivityManager != null)
            {
                band = productivityManager.GetBand();
            }

            float bandMultiplier = 1f;
            switch (band)
            {
                case ProductivityBand.Thriving: bandMultiplier = 1f; break;
                case ProductivityBand.Declining: bandMultiplier = 0.5f; break;
                case ProductivityBand.Collapse: bandMultiplier = 0.2f; break;
            }

            effort *= bandMultiplier;
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

    private void AssignWorkOffsetForCurrentBuilding()
    {
        if (currentTargetBuilding == null)
        {
            currentWorkOffset = Vector3.zero;
            return;
        }

        float side = Random.value < 0.5f ? -1f : 1f;
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

    public void UpdateStateFromProductivity(ProductivityBand band, float currentProductivity)
    {
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
        if (animator != null)
        {
            animator.SetInteger("State", (int)currentState);
        }

        if (spriteRenderer == null) return;

        switch (currentState)
        {
            case PersonState.Working:
                spriteRenderer.color = Color.red;
                break;
            case PersonState.ShiftingAttention:
                spriteRenderer.color = Color.yellow;
                break;
            case PersonState.PhoneAddiction:
                spriteRenderer.color = new Color(1f, 0.5f, 0f);
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
