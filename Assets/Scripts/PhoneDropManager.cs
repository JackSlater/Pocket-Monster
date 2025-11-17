using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class PhoneDropManager : MonoBehaviour
{
    [Header("References")]
    public ProductivityManager productivityManager;
    public GameObject phonePrefab;          // assign the Phone prefab in Inspector
    public Camera mainCamera;               // optional; will use Camera.main if null

    [Header("Drop Settings")]
    public float spawnHeight = 5f;          // how high above the click the phone appears
    public float phoneLifetime = 5f;        // how long a phone can stay if not tapped
    public float phoneCooldown = 3f;        // delay before another phone can be dropped

    private Phone activePhone;              // track the current phone (only one at a time)
    private float cooldownTimer = 0f;

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void Update()
    {
        // If game is over, don't spawn more phones
        if (GameManager.Instance != null && GameManager.Instance.isGameOver)
            return;

        // Cooldown timer
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }

        // Left mouse / primary click
        if (Input.GetMouseButtonDown(0))
        {
            // Ignore clicks over UI
            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            TrySpawnPhoneAtClick();
        }
    }

    private void TrySpawnPhoneAtClick()
    {
        if (phonePrefab == null || mainCamera == null)
        {
            Debug.LogWarning("PhoneDropManager: Missing phonePrefab or mainCamera.");
            return;
        }

        // Block if a phone is already active or we're in cooldown
        if (activePhone != null && activePhone.isActive)
        {
            Debug.Log("PhoneDropManager: Phone already active, ignoring click.");
            return;
        }

        if (cooldownTimer > 0f)
        {
            Debug.Log("PhoneDropManager: Cooldown active, ignoring click.");
            return;
        }

        // Convert mouse position to world position
        Vector3 screenPos = Input.mousePosition;
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(screenPos);
        worldPos.z = 0f;

        // Spawn above the click so it falls down
        Vector3 spawnPos = worldPos + Vector3.up * spawnHeight;

        GameObject phoneObj = Instantiate(phonePrefab, spawnPos, Quaternion.identity);

        // Cache the Phone component
        activePhone = phoneObj.GetComponent<Phone>();
        if (activePhone == null)
        {
            activePhone = phoneObj.AddComponent<Phone>();
        }

        // Make sure the phone falls
        Rigidbody2D rb = phoneObj.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = phoneObj.AddComponent<Rigidbody2D>();
        }
        rb.gravityScale = 1f;

        // Auto-despawn after lifetime if not tapped
        if (phoneLifetime > 0f)
        {
            StartCoroutine(PhoneLifetimeRoutine(activePhone, phoneLifetime));
        }
    }

    private IEnumerator PhoneLifetimeRoutine(Phone phone, float lifetime)
    {
        float t = 0f;
        while (t < lifetime && phone != null && phone.isActive)
        {
            t += Time.deltaTime;
            yield return null;
        }

        // If phone is still around and active after lifetime, despawn it
        if (phone != null && phone.isActive)
        {
            phone.DisablePhone();
        }
    }

    /// <summary>
    /// Called by Phone when it hits the ground.
    /// This is where we apply the productivity hit AND make villagers react.
    /// </summary>
    public void OnPhoneLanded(Phone phone)
    {
        // 1) Productivity impact (numbers going down, band changing, etc.)
        if (productivityManager != null)
        {
            productivityManager.ApplyPhoneDrop();
        }

        // 2) Villager behavior: everyone freezes, one villager goes to the phone
        StartPhoneEffectForVillagers(phone);
    }

    /// <summary>
    /// Called by Phone when the player taps it.
    /// We don't apply productivity here – that already happened on landing –
    /// but we do start cooldown once the phone is removed.
    /// </summary>
    public void OnPhoneTapped(Phone phone)
    {
        Debug.Log("PhoneDropManager: Phone tapped!");
        // Phone will call DisablePhone(), which triggers OnPhoneDisabled.
    }

    /// <summary>
    /// Called by Phone.DisablePhone when the phone object is being removed.
    /// Clears the active phone reference and starts the cooldown.
    /// </summary>
    public void OnPhoneDisabled(Phone phone)
    {
        if (phone == activePhone)
        {
            activePhone = null;
        }

        cooldownTimer = phoneCooldown;
    }

    /// <summary>
    /// Handles how villagers react when a phone has landed:
    /// - Pick ONE villager (nearest to the phone) to chase it
    /// - Everyone else keeps moving / working as normal
    /// </summary>
    private void StartPhoneEffectForVillagers(Phone phone)
    {
        // Get all villagers currently in the scene
        Villager[] villagers = FindObjectsOfType<Villager>();
        if (villagers == null || villagers.Length == 0 || phone == null)
            return;

        // 1) Clear any leftover frozen flags from a previous phone
        foreach (var v in villagers)
        {
            if (v != null)
            {
                v.SetFrozenByPhone(false);
            }
        }

        // 2) Pick ONE villager to be the chaser (nearest non-addicted, non-destructive)
        Villager chaser = null;
        float bestDistSq = float.MaxValue;

        foreach (var v in villagers)
        {
            if (v == null) continue;

            // Don’t pick villagers that are already gone to the dark side
            if (v.currentState == PersonState.PhoneAddiction ||
                v.currentState == PersonState.Destructive)
            {
                continue;
            }

            float dSq = (v.transform.position - phone.transform.position).sqrMagnitude;
            if (dSq < bestDistSq)
            {
                bestDistSq = dSq;
                chaser = v;
            }
        }

        // 3) Send that one villager after the phone
        if (chaser != null)
        {
            chaser.BecomePhoneChaser(phone);
        }
    }
}
