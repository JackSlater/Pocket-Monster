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
    public PopulationManager populationManager;

    // Internal movement
    private float actualMoveSpeed;
    private Building currentTargetBuilding;
    private Vector3 currentWorkOffset;

    // Phone-related
    private bool frozenByPhone = false;
    private bool isPhoneChaser = false;
    private Phone targetPhone = null;

    // Gambling-phone: this villager is a building destroyer
    private bool isBuildingDestroyer = false;
    private float destructionTimer = 0f;   // time spent destroying current building

    // Red-phone violence: each violent villager may kill only once
    private bool hasKilledSomeone = false;

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

        if (populationManager == null)
            populationManager = FindObjectOfType<PopulationManager>();

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
        {
            if (isBuildingDestroyer)
                HandleBuildingDestruction(dt);
            else
                HandleViolentBehaviour(dt);
        }
        else if (currentState == PersonState.PhoneAddiction)
        {
            // Add idle animation later if you want, but no movement
        }
        else if (isPhoneChaser && targetPhone != null && targetPhone.isActive)
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

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private float GetCurrentMoveSpeed()
    {
        float mult = 1f;
        if (populationManager != null)
            mult = populationManager.globalSpeedMultiplier; // blue phone slow-down

        return actualMoveSpeed * mult;
    }

    // -------------------------------------------------------------------------
    // PHONE LOGIC
    // -------------------------------------------------------------------------

    private void HandlePhoneChase(float dt)
    {
        if (targetPhone == null || !targetPhone.isActive)
        {
            isPhoneChaser = false;
            return;
        }

        float moveSpeed = GetCurrentMoveSpeed();

        Vector3 targetPos = targetPhone.transform.position;
        if (lockToGround) targetPos.y = groundY;

        Vector3 delta = targetPos - transform.position;
        float dist = delta.magnitude;

        // While the phone is still falling, we ONLY move toward it, never pick it up
        if (!targetPhone.hasLanded)
        {
            if (dist > closeEnoughDistance)
            {
                Vector3 dir = delta.normalized;
                Vector3 newPos = transform.position + dir * moveSpeed * dt;
                if (lockToGround) newPos.y = groundY;
                transform.position = newPos;
            }

            SetState(PersonState.ShiftingAttention);
            return;
        }

        // Phone has landed – now we can actually reach and pick it up
        if (dist > closeEnoughDistance)
        {
            Vector3 dir = delta.normalized;
            Vector3 newPos = transform.position + dir * moveSpeed * dt;
            if (lockToGround) newPos.y = groundY;
            transform.position = newPos;

            SetState(PersonState.ShiftingAttention);
        }
        else
        {
            // We've reached the phone on the ground
            if (populationManager != null)
            {
                populationManager.OnVillagerPickedUpPhone(this, targetPhone);
            }

            targetPhone.DisablePhone();
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

    // -------------------------------------------------------------------------
    // GAMBLING PHONE: BUILDING DESTRUCTION
    // -------------------------------------------------------------------------

    public void BecomeBuildingDestroyer()
    {
        isBuildingDestroyer = true;
        destructionTimer = 0f;
        currentTargetBuilding = null;
        currentState = PersonState.Destructive;
        UpdateVisuals();
    }

    private void HandleBuildingDestruction(float dt)
    {
        if (buildingManager == null || buildingManager.buildings == null || buildingManager.buildings.Count == 0)
            return;

        if (currentTargetBuilding == null || currentTargetBuilding.currentState == BuildingState.Destroyed)
        {
            currentTargetBuilding = FindNextAliveBuilding();
            destructionTimer = 0f;
        }

        if (currentTargetBuilding == null)
            return;

        Vector3 targetPos = currentTargetBuilding.transform.position;
        if (lockToGround) targetPos.y = groundY;

        Vector3 delta = targetPos - transform.position;
        float dist = delta.magnitude;
        float moveSpeed = GetCurrentMoveSpeed();

        if (dist > closeEnoughDistance)
        {
            Vector3 dir = delta.normalized;
            Vector3 newPos = transform.position + dir * moveSpeed * dt;
            if (lockToGround) newPos.y = groundY;
            transform.position = newPos;
        }
        else
        {
            // We're at the building: chip away over time.
            destructionTimer += dt;

            // Time a single worker would need to fully build this building
            float timeToDestroy = 1f;
            if (currentTargetBuilding.constructionRequirement > 0f && workEffortPerSecond > 0f)
            {
                timeToDestroy = currentTargetBuilding.constructionRequirement / workEffortPerSecond;
            }

            if (destructionTimer >= timeToDestroy)
            {
                currentTargetBuilding.ForceCollapse();
                currentTargetBuilding = null;
                destructionTimer = 0f;
            }
        }
    }

    private Building FindNextAliveBuilding()
    {
        if (buildingManager == null || buildingManager.buildings == null)
            return null;

        foreach (var b in buildingManager.buildings)
        {
            if (b == null) continue;
            if (b.currentState != BuildingState.Destroyed)
                return b;
        }
        return null;
    }

    // -------------------------------------------------------------------------
    // RED PHONE: VIOLENT BEHAVIOUR
    // -------------------------------------------------------------------------

    private void HandleViolentBehaviour(float dt)
    {
        // Once they’ve killed one person, they calm down and become idle
        if (hasKilledSomeone)
        {
            SetState(PersonState.Idle);
            return;
        }

        if (populationManager == null || populationManager.villagers == null)
            return;

        Villager target = null;
        float bestDistSq = float.MaxValue;

        foreach (var v in populationManager.villagers)
        {
            if (v == null || v == this) continue;
            if (v.currentState == PersonState.Destructive) continue; // ignore other violent ones

            float dSq = (v.transform.position - transform.position).sqrMagnitude;
            if (dSq < bestDistSq)
            {
                bestDistSq = dSq;
                target = v;
            }
        }

        float moveSpeed = GetCurrentMoveSpeed();
        float attackRadius = 0.1f;

        // Close enough to "hit"
        if (target != null && bestDistSq < attackRadius * attackRadius)
        {
            if (populationManager.villagers.Contains(target))
                populationManager.villagers.Remove(target);
            Destroy(target.gameObject);

            hasKilledSomeone = true;
            SetState(PersonState.Idle);
            return;
        }

        // Move toward target if we have one
        if (target != null)
        {
            Vector3 targetPos = target.transform.position;
            if (lockToGround) targetPos.y = groundY;

            Vector3 delta = targetPos - transform.position;
            float dist = delta.magnitude;

            if (dist > closeEnoughDistance)
            {
                Vector3 dir = delta.normalized;
                Vector3 newPos = transform.position + dir * moveSpeed * dt;
                if (lockToGround) newPos.y = groundY;
                transform.position = newPos;
            }
        }
        else
        {
            // No target – small wander
            Vector3 dir = new Vector3(Random.Range(-1f, 1f), 0f, 0f).normalized;
            Vector3 newPos = transform.position + dir * moveSpeed * dt * 0.5f;
            if (lockToGround) newPos.y = groundY;
            transform.position = newPos;
        }
    }

    // -------------------------------------------------------------------------
    // WORK / BUILDING LOGIC
    // -------------------------------------------------------------------------

    private void HandleWorkLogic(float dt)
    {
        if (buildingManager == null) return;

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
        if (lockToGround) targetPos.y = groundY;

        Vector3 delta = targetPos - transform.position;
        float dist = delta.magnitude;

        float moveSpeed = GetCurrentMoveSpeed();

        if (dist > closeEnoughDistance)
        {
            Vector3 dir = delta.normalized;
            Vector3 newPos = transform.position + dir * moveSpeed * dt;
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
                case ProductivityBand.Thriving: bandMultiplier = 1f;   break;
                case ProductivityBand.Declining: bandMultiplier = 0.5f; break;
                case ProductivityBand.Collapse:  bandMultiplier = 0.2f; break;
            }
            effort *= bandMultiplier;

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
                return b;
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

    // -------------------------------------------------------------------------
    // STATE / VISUALS
    // -------------------------------------------------------------------------

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

    // -------------------------------------------------------------------------
    // DEBUG
    // -------------------------------------------------------------------------

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
