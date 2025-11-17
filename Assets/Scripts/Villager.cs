using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Villager : MonoBehaviour
{
    public PersonState currentState = PersonState.Working;

    [Header("Movement")]
    public float baseMoveSpeed = 1.5f;
    public float moveSpeedVariation = 0.75f;
    public float closeEnoughDistance = 0.2f;

    [Header("Construction Contribution")]
    public float workEffortPerSecond = 10f;

    [Header("Work Position Offsets")]
    public float workOffsetDistance = 0.4f;
    public float workOffsetVerticalJitter = 0.1f;

    [Header("Ground / Walk Line")]
    public bool lockToGround = true;
    public float groundY = -0.8f;

    [Header("References")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;
    public BuildingManager buildingManager;
    public ProductivityManager productivityManager;

    private float actualMoveSpeed;
    private Building currentTargetBuilding;
    private Vector3 currentWorkOffset;

    // Phone-related
    private bool frozenByPhone = false;
    private bool isPhoneChaser = false;
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

        float randomized = baseMoveSpeed + Random.Range(-moveSpeedVariation, moveSpeedVariation);
        actualMoveSpeed = Mathf.Max(0.1f, randomized);

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

        if (lockToGround)
        {
            Vector3 pos = transform.position;
            pos.y = groundY;
            transform.position = pos;
        }
    }

    // ---------- PHONE LOGIC ----------

    private void HandlePhoneChase(float dt)
    {
        if (targetPhone == null || !targetPhone.isActive)
        {
            isPhoneChaser = false;
            return;
        }

        Vector3 targetPos = targetPhone.transform.position;
        if (lockToGround)
            targetPos.y = groundY;

        Vector3 delta = targetPos - transform.position;
        float dist = delta.magnitude;

        if (dist > closeEnoughDistance)
        {
            Vector3 dir = delta.normalized;
            Vector3 newPos = transform.position + dir * actualMoveSpeed * dt;
            if (lockToGround) newPos.y = groundY;
            transform.position = newPos;

            SetState(PersonState.ShiftingAttention);
        }
        else
        {
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
        frozenByPhone = false;
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

    // ---------- BUILDING WORK LOGIC ----------

    private void HandleWorkLogic(float dt)
    {
        if (buildingManager == null) return;

        // Always pick the FIRST under-construction building in the list
        if (currentTargetBuilding == null ||
            currentTargetBuilding.currentState != BuildingState.UnderConstruction)
        {
            currentTargetBuilding = FindNextUnderConstructionBuildingInOrder();
            AssignWorkOffsetForCurrentBuilding();
        }

        if (currentTargetBuilding == null)
        {
            SetState(PersonState.Idle);
            return;
        }

        Vector3 targetPos = currentTargetBuilding.transform.position + currentWorkOffset;
        if (lockToGround)
            targetPos.y = groundY;

        Vector3 delta = targetPos - transform.position;
        float dist = delta.magnitude;

        if (dist > closeEnoughDistance)
        {
            Vector3 dir = delta.normalized;
            Vector3 newPos = transform.position + dir * actualMoveSpeed * dt;
            if (lockToGround) newPos.y = groundY;
            transform.position = newPos;

            SetState(PersonState.ShiftingAttention);
        }
        else
        {
            float effort = workEffortPerSecond * dt;

            ProductivityBand band = ProductivityBand.Thriving;
            if (productivityManager != null)
                band = productivityManager.GetBand();

            float bandMultiplier = 1f;
            switch (band)
            {
                case ProductivityBand.Thriving: bandMultiplier = 1f; break;
                case ProductivityBand.Declining: bandMultiplier = 0.5f; break;
                case ProductivityBand.Collapse:  bandMultiplier = 0.2f; break;
            }
            effort *= bandMultiplier;

            // Only this building is ticked
            currentTargetBuilding.Tick(effort, 0f, dt, band);

            SetState(PersonState.Working);
        }
    }

    private Building FindNextUnderConstructionBuildingInOrder()
    {
        if (buildingManager == null || buildingManager.buildings == null)
            return null;

        foreach (var b in buildingManager.buildings)
        {
            if (b == null) continue;
            if (b.currentState == BuildingState.UnderConstruction)
                return b;   // first under-construction in list
        }

        return null;
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

    // ---------- STATE / VISUALS ----------

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
            animator.SetInteger("State", (int)currentState);

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

    // ---------- DEBUG GIZMO ----------

    private void OnDrawGizmosSelected()
    {
        if (currentTargetBuilding != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, currentTargetBuilding.transform.position);
            Gizmos.DrawSphere(currentTargetBuilding.transform.position, 0.08f);
        }
    }
}
