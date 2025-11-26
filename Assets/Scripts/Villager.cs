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

    [Header("References (auto-filled at runtime)")]
    public SpriteRenderer spriteRenderer;
    public BuildingManager buildingManager;
    public ProductivityManager productivityManager;
    public PopulationManager populationManager;

    // Internal movement
    private float actualMoveSpeed;
    private Building currentTargetBuilding;
    private Vector3 currentWorkOffset;

    // Phone-related
    private bool isPhoneChaser = false;
    private Phone targetPhone;
    private float phoneChaseStopDistance = 0.15f;
    private bool frozenByPhone = false;   // kept for compatibility but we won't set it anymore

    // Destruction / violence
    private bool isBuildingDestroyer = false;
    private float destructionTimer = 0f;
    private bool hasKilledSomeone = false;

    private void Reset()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (buildingManager == null)
            buildingManager = FindObjectOfType<BuildingManager>();

        if (productivityManager == null)
            productivityManager = FindObjectOfType<ProductivityManager>();

        if (populationManager == null)
            populationManager = FindObjectOfType<PopulationManager>();
    }

    private void Start()
    {
        float variation = Random.Range(-moveSpeedVariation, moveSpeedVariation);
        actualMoveSpeed = Mathf.Max(0.1f, baseMoveSpeed + variation);
        UpdateVisuals();
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameOver)
            return;

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        if (isPhoneChaser && targetPhone != null)
        {
            HandlePhoneChase(dt);
        }
        else if (currentState == PersonState.Destructive)
        {
            if (isBuildingDestroyer)
                HandleBuildingDestruction(dt);
            else
                HandleViolentBehaviour(dt);
        }
        else if (currentState == PersonState.PhoneAddiction)
        {
            // do nothing – phone addicted
        }
        else if (frozenByPhone)
        {
            // we won't actually set this anymore, so this should never run
        }
        else
        {
            HandleWorkLogic(dt);
        }

        if (lockToGround)
        {
            Vector3 p = transform.position;
            p.y = groundY;
            transform.position = p;
        }
    }

    // ---------------- WORK / BUILDINGS ----------------

    private void HandleWorkLogic(float dt)
    {
        if (buildingManager == null || buildingManager.buildings == null || buildingManager.buildings.Count == 0)
            return;

        if (currentTargetBuilding == null ||
            currentTargetBuilding.currentState == BuildingState.Destroyed)
        {
            currentTargetBuilding = SelectBestBuilding();
            PickNewWorkOffset();
        }

        if (currentTargetBuilding == null)
            return;

        Vector3 targetPos = currentTargetBuilding.transform.position + currentWorkOffset;
        float moveSpeed = GetCurrentSpeed();

        Vector3 delta = targetPos - transform.position;
        float dist = delta.magnitude;

        if (dist > closeEnoughDistance)
        {
            Vector3 step = delta.normalized * moveSpeed * dt;
            if (step.magnitude > dist)
                step = delta;

            transform.position += step;
            SetWalkingAnimation(true, step.x);
        }
        else
        {
            SetWalkingAnimation(false, 0f);

            if (currentTargetBuilding.currentState == BuildingState.UnderConstruction)
            {
                currentTargetBuilding.AddConstructionProgress(workEffortPerSecond * dt);
            }
        }
    }

    private float GetCurrentSpeed()
    {
        float mult = 1f;
        if (populationManager != null)
            mult = populationManager.globalSpeedMultiplier;

        return actualMoveSpeed * mult;
    }

    private Building SelectBestBuilding()
    {
        Building best = null;
        float bestDist = float.MaxValue;

        if (buildingManager == null || buildingManager.buildings == null)
            return null;

        foreach (var b in buildingManager.buildings)
        {
            if (b == null) continue;
            if (b.currentState == BuildingState.Destroyed) continue;

            float d = Vector2.Distance(transform.position, b.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = b;
            }
        }

        return best;
    }

    private void PickNewWorkOffset()
    {
        if (currentTargetBuilding == null)
        {
            currentWorkOffset = Vector3.zero;
            return;
        }

        if (workOffsetDistance <= 0f)
        {
            currentWorkOffset = Vector3.zero;
            return;
        }

        float angle = Random.Range(0f, Mathf.PI * 2f);
        float radius = workOffsetDistance;
        float jitterY = Random.Range(-workOffsetVerticalJitter, workOffsetVerticalJitter);

        currentWorkOffset = new Vector3(Mathf.Cos(angle) * radius, jitterY, 0f);
    }

    // ---------------- PHONE CHASING ----------------

    private void HandlePhoneChase(float dt)
    {
        if (targetPhone == null || !targetPhone.isActive)
        {
            isPhoneChaser = false;
            targetPhone = null;
            SetWalkingAnimation(false, 0f);
            return;
        }

        Vector3 targetPos = targetPhone.transform.position;
        if (lockToGround) targetPos.y = groundY;

        Vector3 delta = targetPos - transform.position;
        float dist = delta.magnitude;
        float moveSpeed = GetCurrentSpeed();

        if (dist > closeEnoughDistance)
        {
            Vector3 step = delta.normalized * moveSpeed * dt;
            if (step.magnitude > dist)
                step = delta;

            transform.position += step;
            SetWalkingAnimation(true, step.x);
        }
        else
        {
            SetWalkingAnimation(false, 0f);

            if (targetPhone.hasLanded && targetPhone.isActive)
            {
                if (populationManager != null)
                    populationManager.OnVillagerPickedUpPhone(this, targetPhone);

                targetPhone.DisablePhone();
                isPhoneChaser = false;
                targetPhone = null;
            }
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
        // kept for compatibility with PopulationManager but we don't really use it now
        frozenByPhone = frozen;
        if (!frozen)
        {
            SetWalkingAnimation(false, 0f);
        }
    }

    // ---------------- VIOLENT / DESTRUCTIVE ----------------

    private void HandleViolentBehaviour(float dt)
    {
        if (hasKilledSomeone)
        {
            SetState(PersonState.Idle);
            return;
        }

        if (populationManager == null || populationManager.villagers == null)
            return;

        Villager target = null;
        float bestDistSq = 1.5f * 1.5f;

        foreach (var v in populationManager.villagers)
        {
            if (v == null || v == this) continue;

            if (v.currentState == PersonState.PhoneAddiction ||
                v.currentState == PersonState.Destructive)
                continue;

            float dSq = (v.transform.position - transform.position).sqrMagnitude;
            if (dSq < bestDistSq)
            {
                bestDistSq = dSq;
                target = v;
            }
        }

        if (target != null)
        {
            target.SetPhoneAddicted();
            hasKilledSomeone = true;
        }
    }

    private void HandleBuildingDestruction(float dt)
    {
        if (buildingManager == null || buildingManager.buildings == null)
            return;

        destructionTimer -= dt;
        if (destructionTimer > 0f) return;

        destructionTimer = 1.0f;

        Building victim = null;
        float bestDist = float.MaxValue;

        foreach (var b in buildingManager.buildings)
        {
            if (b == null) continue;
            if (b.currentState != BuildingState.Completed) continue;

            float d = Vector2.Distance(transform.position, b.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                victim = b;
            }
        }

        if (victim != null)
        {
            victim.TakeDamage(10f);
        }
    }

    public void BecomeBuildingDestroyer()
    {
        currentState = PersonState.Destructive;
        isBuildingDestroyer = true;
        destructionTimer = 0f;
        UpdateVisuals();
    }

    // ---------------- PRODUCTIVITY STATES ----------------

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
                if (currentState == PersonState.Working)
                    SetState(PersonState.ShiftingAttention);
                break;

            case ProductivityBand.Collapse:
                SetState(PersonState.Idle);
                break;
        }
    }

    // ---------------- STATE & VISUALS ----------------

    public void SetState(PersonState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        UpdateVisuals();
    }

    public void SetPhoneAddicted()
    {
        currentState = PersonState.PhoneAddiction;
        isPhoneChaser = false;
        targetPhone = null;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer == null) return;

        switch (currentState)
        {
            case PersonState.Working:
                spriteRenderer.color = Color.white;
                break;
            case PersonState.Idle:
                spriteRenderer.color = new Color(0.85f, 0.85f, 0.85f);
                break;
            case PersonState.ShiftingAttention:
                spriteRenderer.color = new Color(0.9f, 0.9f, 0.6f);
                break;
            case PersonState.PhoneAddiction:
                spriteRenderer.color = Color.cyan;
                break;
            case PersonState.Destructive:
                spriteRenderer.color = Color.red;
                break;
        }
    }

    private void SetWalkingAnimation(bool isWalking, float directionX)
    {
        // Only sprite flip now – no Animator
        if (!isWalking) return;

        if (spriteRenderer != null)
        {
            if (directionX > 0.01f)
                spriteRenderer.flipX = false;
            else if (directionX < -0.01f)
                spriteRenderer.flipX = true;
        }
    }

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
