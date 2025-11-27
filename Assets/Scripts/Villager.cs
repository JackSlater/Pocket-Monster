using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Villager : MonoBehaviour
{
    public PersonState currentState = PersonState.Working;

    [Header("Movement")]
    public float baseMoveSpeed = 1.5f;
    public float moveSpeedVariation = 0.75f;
    public float closeEnoughDistance = 0.2f;

    [Header("State Speed Modifiers")]
    [Range(0.1f, 1f)]
    public float shiftingAttentionSpeedMultiplier = 0.75f;

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
    private bool frozenByPhone = false;

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
        // Force a clean starting state (in case the prefab enum got serialized weird)
        currentState = PersonState.Working;
        isPhoneChaser = false;
        targetPhone = null;
        isBuildingDestroyer = false;
        hasKilledSomeone = false;
        frozenByPhone = false;

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
            // phone-addicted villagers just stand there
            SetWalkingAnimation(false, 0f);
        }
        else if (frozenByPhone)
        {
            // paused by a phone, do nothing
            SetWalkingAnimation(false, 0f);
        }
        else
        {
            // ✅ Only these states are allowed to work/move
            if (currentState == PersonState.Working ||
                currentState == PersonState.ShiftingAttention)
            {
                HandleWorkLogic(dt);
            }
            else
            {
                // Idle or any other state: no movement, no building
                SetWalkingAnimation(false, 0f);
            }
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

        // Retarget if no building, or current building is destroyed / completed
        if (currentTargetBuilding == null ||
            currentTargetBuilding.currentState == BuildingState.Destroyed ||
            currentTargetBuilding.currentState == BuildingState.Completed)
        {
            currentTargetBuilding = SelectBestBuilding();
            PickNewWorkOffset();
        }

        if (currentTargetBuilding == null)
            return;

        Vector3 targetPos = currentTargetBuilding.transform.position + currentWorkOffset;
        if (lockToGround)
            targetPos.y = groundY;  // snap to road

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
        // Start from this villager's own base speed (with per-villager variation)
        float speed = actualMoveSpeed;

        // Villagers who are "ShiftingAttention" move a bit slower
        if (currentState == PersonState.ShiftingAttention)
        {
            speed *= shiftingAttentionSpeedMultiplier;
        }

        // Apply any global movement effect (e.g. Mainstream Media phone)
        if (populationManager != null)
        {
            speed *= populationManager.globalSpeedMultiplier;
        }

        return speed;
    }

    private Building SelectBestBuilding()
    {
        if (buildingManager == null || buildingManager.buildings == null || buildingManager.buildings.Count == 0)
            return null;

        Building bestUnderConstruction = null;
        float bestUnderDist = float.MaxValue;

        Building bestAny = null;
        float bestAnyDist = float.MaxValue;

        foreach (var b in buildingManager.buildings)
        {
            if (b == null) continue;
            if (b.currentState == BuildingState.Destroyed) continue;

            float d = Vector2.Distance(transform.position, b.transform.position);

            // Prefer under-construction buildings
            if (b.currentState == BuildingState.UnderConstruction)
            {
                if (d < bestUnderDist)
                {
                    bestUnderDist = d;
                    bestUnderConstruction = b;
                }
            }

            // Fallback: nearest standing building of any non-destroyed state
            if (d < bestAnyDist)
            {
                bestAnyDist = d;
                bestAny = b;
            }
        }

        if (bestUnderConstruction != null)
            return bestUnderConstruction;

        return bestAny;
    }

    private Building SelectNearestCompletedBuilding()
    {
        if (buildingManager == null || buildingManager.buildings == null || buildingManager.buildings.Count == 0)
            return null;

        Building best = null;
        float bestDist = float.MaxValue;

        foreach (var b in buildingManager.buildings)
        {
            if (b == null) continue;
            if (b.currentState != BuildingState.Completed) continue;

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
                // Cache before callbacks
                Phone collected = targetPhone;

                isPhoneChaser = false;
                targetPhone = null;

                if (populationManager != null)
                    populationManager.OnVillagerPickedUpPhone(this, collected);

                if (collected != null && collected.isActive)
                    collected.DisablePhone();
            }
        }
    }

    public void BecomePhoneChaser(Phone phone)
    {
        // Never allow idle, already-addicted, or destructive villagers to chase phones
        if (currentState == PersonState.Idle ||
            currentState == PersonState.PhoneAddiction ||
            currentState == PersonState.Destructive)
        {
            return;
        }

        targetPhone = phone;
        isPhoneChaser = true;
        frozenByPhone = false;
    }

    public void BecomeMediaInfluenced()
    {
        currentState = PersonState.ShiftingAttention;
        UpdateVisuals();

        // Apply global slowdown (e.g. half speed)
        if (populationManager != null)
            populationManager.ApplyMediaPhoneSlowdown(0.5f);
    }

    public void SetFrozenByPhone(bool frozen)
    {
        frozenByPhone = frozen;
        if (!frozen)
        {
            SetWalkingAnimation(false, 0f);
        }
    }

    // ---------------- VIOLENT / DESTRUCTIVE (Social phone) ----------------

    private void HandleViolentBehaviour(float dt)
    {
        // Already killed someone: calm down
        if (hasKilledSomeone)
        {
            SetState(PersonState.Idle);
            return;
        }

        if (populationManager == null || populationManager.villagers == null)
            return;

        // Find nearest valid victim
        Villager target = null;
        float bestDist = float.MaxValue;

        foreach (var v in populationManager.villagers)
        {
            if (v == null || v == this) continue;

            // Don’t target people already lost or destructive
            if (v.currentState == PersonState.PhoneAddiction ||
                v.currentState == PersonState.Destructive)
                continue;

            float d = Vector2.Distance(transform.position, v.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                target = v;
            }
        }

        if (target == null)
        {
            // No one left to kill
            SetState(PersonState.Idle);
            return;
        }

        Vector3 targetPos = target.transform.position;
        if (lockToGround) targetPos.y = groundY;

        Vector3 delta = targetPos - transform.position;
        float dist = delta.magnitude;
        float moveSpeed = GetCurrentSpeed();

        if (dist > closeEnoughDistance)
        {
            // Walk toward the victim
            Vector3 step = delta.normalized * moveSpeed * dt;
            if (step.magnitude > dist)
                step = delta;

            transform.position += step;
            SetWalkingAnimation(true, step.x);
        }
        else
        {
            // In range: kill the victim, then become idle
            SetWalkingAnimation(false, 0f);

            if (populationManager.villagers.Contains(target))
                populationManager.villagers.Remove(target);

            if (target != null && target.gameObject.scene.IsValid())
                Destroy(target.gameObject);

            hasKilledSomeone = true;
            SetState(PersonState.Idle);
        }
    }

    private void HandleBuildingDestruction(float dt)
    {
        if (buildingManager == null || buildingManager.buildings == null || buildingManager.buildings.Count == 0)
        {
            SetWalkingAnimation(false, 0f);
            return;
        }

        // Make sure we have a valid completed-building target
        if (currentTargetBuilding == null || currentTargetBuilding.currentState != BuildingState.Completed)
        {
            currentTargetBuilding = SelectNearestCompletedBuilding();
            PickNewWorkOffset();
        }

        // No completed buildings to destroy → just stand still
        if (currentTargetBuilding == null)
        {
            SetWalkingAnimation(false, 0f);
            return;
        }

        // Walk over to the chosen completed building (same style as work logic)
        Vector3 targetPos = currentTargetBuilding.transform.position + currentWorkOffset;
        if (lockToGround)
            targetPos.y = groundY;

        Vector3 delta = targetPos - transform.position;
        float dist = delta.magnitude;
        float moveSpeed = GetCurrentSpeed();

        if (dist > closeEnoughDistance)
        {
            // Still walking toward the building
            Vector3 step = delta.normalized * moveSpeed * dt;
            if (step.magnitude > dist)
                step = delta;

            transform.position += step;
            SetWalkingAnimation(true, step.x);
        }
        else
        {
            // Reached the building: now deconstruct it at the same rate
            // as one villager constructing (workEffortPerSecond).
            SetWalkingAnimation(false, 0f);

            if (currentTargetBuilding.currentState == BuildingState.Completed)
            {
                // Same magnitude as construction, but applied to TakeDamage()
                currentTargetBuilding.TakeDamage(workEffortPerSecond * dt);
            }
            else
            {
                // If the building changed state (e.g. destroyed), retarget next frame
                currentTargetBuilding = null;
            }
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
        // Once a villager is Idle, PhoneAddicted, or Destructive,
        // productivity can NEVER change their state again.
        if (currentState == PersonState.PhoneAddiction ||
            currentState == PersonState.Destructive   ||
            currentState == PersonState.Idle)
        {
            return;
        }

        switch (band)
        {
            case ProductivityBand.Thriving:
                // Only move from ShiftingAttention back to Working
                if (currentState == PersonState.ShiftingAttention)
                    SetState(PersonState.Working);
                break;

            case ProductivityBand.Declining:
                // Working → ShiftingAttention
                if (currentState == PersonState.Working)
                    SetState(PersonState.ShiftingAttention);
                break;

            case ProductivityBand.Collapse:
                // Working or ShiftingAttention → Idle (and then they stay Idle forever)
                if (currentState == PersonState.Working ||
                    currentState == PersonState.ShiftingAttention)
                {
                    SetState(PersonState.Idle);
                }
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
                spriteRenderer.color = Color.green;
                break;
            case PersonState.Idle:
                spriteRenderer.color = new Color(0.5f, 0.5f, 0.5f);
                break;
            case PersonState.ShiftingAttention:
                spriteRenderer.color = Color.yellow;
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
