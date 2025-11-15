using UnityEngine;
using UnityEngine.EventSystems;

public class PhoneDropManager : MonoBehaviour
{
    [Header("References")]
    public ProductivityManager productivityManager;
    public PopulationManager populationManager;
    public GameObject phonePrefab;          // assign the Phone prefab in Inspector
    public Camera mainCamera;               // optional; will use Camera.main if null

    [Header("Drop Settings")]
    public float spawnHeight = 5f;          // how high above the click the phone appears
    public float phoneLifetime = 5f;        // how long a phone can stay if not tapped

    private Phone activePhone;              // track the current phone (optional)

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (populationManager == null)
            populationManager = FindObjectOfType<PopulationManager>();
    }

    private void Update()
    {
        // If game is over, don't spawn more phones
        if (GameManager.Instance != null && GameManager.Instance.isGameOver)
            return;

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
            return;

        // If we only want one active phone at a time, bail if one exists
        if (activePhone != null && activePhone.isActive)
            return;

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
            Debug.LogWarning("PhoneDropManager: Spawned phonePrefab has no Phone component.");
        }

        // Make it fall if there is a Rigidbody2D
        Rigidbody2D rb = phoneObj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 1f;
        }

        // Apply game-logic effect when phone drops
        if (productivityManager != null)
        {
            productivityManager.ApplyPhoneDrop();
        }

        // Tell the population that a phone has dropped
        if (populationManager == null)
            populationManager = FindObjectOfType<PopulationManager>();

        if (populationManager != null && activePhone != null)
        {
            populationManager.OnPhoneDropped(activePhone);
        }

        // Start a lifetime timer
        if (phoneLifetime > 0f && activePhone != null)
        {
            StartCoroutine(PhoneLifetimeRoutine(activePhone, phoneLifetime));
        }
    }

    private System.Collections.IEnumerator PhoneLifetimeRoutine(Phone phone, float lifetime)
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

        if (phone == activePhone)
        {
            activePhone = null;
        }

        // Phone disappeared without being picked up → unfreeze villagers
        if (populationManager == null)
            populationManager = FindObjectOfType<PopulationManager>();

        if (populationManager != null)
        {
            populationManager.OnPhoneCleared();
        }
    }

    // Called by Phone.cs when the player taps the phone
    public void OnPhoneTapped(Phone phone)
    {
        Debug.Log("PhoneDropManager: Phone tapped!");

        // If we only track one active phone, clear it here
        if (phone == activePhone)
        {
            activePhone = null;
        }

        // Phone was explicitly cleared → unfreeze villagers
        if (populationManager == null)
            populationManager = FindObjectOfType<PopulationManager>();

        if (populationManager != null)
        {
            populationManager.OnPhoneCleared();
        }
    }
}
