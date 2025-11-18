using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class PhoneDropManager : MonoBehaviour
{
    [Header("References")]
    public ProductivityManager productivityManager;
    public PopulationManager populationManager;
    public GameObject phonePrefab;
    public Camera mainCamera;

    [Header("Drop Settings")]
    public float spawnHeight = 5f;
    public float phoneLifetime = 5f;
    public float phoneCooldown = 3f;

    [Header("Phone Type Selection")]
    public PhoneType currentPhoneType = PhoneType.SocialMediaRed;

    private Phone activePhone;
    private float cooldownTimer = 0f;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (productivityManager == null)
            productivityManager = FindObjectOfType<ProductivityManager>();

        if (populationManager == null)
            populationManager = FindObjectOfType<PopulationManager>();
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameOver)
            return;

        HandlePhoneTypeSelection();

        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (Input.GetMouseButtonDown(0))
        {
            // Ignore UI clicks
            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
                return;

            TrySpawnPhoneAtClick();
        }
    }

    // Called from keyboard (1–4) AND from UI buttons
    public void SetCurrentPhoneType(PhoneType newType)
    {
        currentPhoneType = newType;
    }

    private void HandlePhoneTypeSelection()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            SetCurrentPhoneType(PhoneType.SocialMediaRed);
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            SetCurrentPhoneType(PhoneType.StreamingYellow);
        else if (Input.GetKeyDown(KeyCode.Alpha3))
            SetCurrentPhoneType(PhoneType.MainstreamBlue);
        else if (Input.GetKeyDown(KeyCode.Alpha4))
            SetCurrentPhoneType(PhoneType.GamblingGreen);
    }

    private void TrySpawnPhoneAtClick()
    {
        if (phonePrefab == null || mainCamera == null)
        {
            Debug.LogWarning("PhoneDropManager: Missing phonePrefab or mainCamera.");
            return;
        }

        // Only one active phone + respect cooldown
        if (activePhone != null && activePhone.isActive)
            return;
        if (cooldownTimer > 0f)
            return;

        Vector3 screenPos = Input.mousePosition;
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(screenPos);
        worldPos.z = 0f;

        Vector3 spawnPos = worldPos + Vector3.up * spawnHeight;

        GameObject phoneObj = Instantiate(phonePrefab, spawnPos, Quaternion.identity);
        activePhone = phoneObj.GetComponent<Phone>();
        if (activePhone == null)
            activePhone = phoneObj.AddComponent<Phone>();

        // Set the gameplay type
        activePhone.phoneType = currentPhoneType;

        // Color the phone based on its type
        SpriteRenderer sr = phoneObj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            switch (currentPhoneType)
            {
                case PhoneType.SocialMediaRed:
                    sr.color = Color.red;
                    break;
                case PhoneType.StreamingYellow:
                    sr.color = Color.yellow;
                    break;
                case PhoneType.MainstreamBlue:
                    sr.color = Color.blue;
                    break;
                case PhoneType.GamblingGreen:
                    sr.color = Color.green;
                    break;
                default:
                    sr.color = Color.white;
                    break;
            }
        }

        Rigidbody2D rb = phoneObj.GetComponent<Rigidbody2D>();
        if (rb == null) rb = phoneObj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 1f;

        // Tell population that a phone dropped (so villagers freeze + one chases)
        if (populationManager != null)
            populationManager.OnPhoneDropped(activePhone);

        if (phoneLifetime > 0f)
            StartCoroutine(PhoneLifetimeRoutine(activePhone, phoneLifetime));
    }

    private IEnumerator PhoneLifetimeRoutine(Phone phone, float lifetime)
    {
        float t = 0f;
        while (t < lifetime && phone != null && phone.isActive)
        {
            t += Time.deltaTime;
            yield return null;
        }

        if (phone != null && phone.isActive)
            phone.DisablePhone();
    }

    public void OnPhoneLanded(Phone phone)
    {
        // Generic productivity impact
        if (productivityManager != null)
            productivityManager.ApplyPhoneDrop();

        if (populationManager == null || phone == null)
            return;

        switch (phone.phoneType)
        {
            case PhoneType.SocialMediaRed:
                populationManager.ApplySocialMediaPhone(phone);
                break;
            case PhoneType.StreamingYellow:
                populationManager.ApplyStreamingPhone(phone);
                break;
            case PhoneType.MainstreamBlue:
                populationManager.ApplyMainstreamMediaPhone();
                break;
            case PhoneType.GamblingGreen:
                populationManager.ApplyGamblingPhone(phone);
                break;
        }
    }

    public void OnPhoneTapped(Phone phone)
    {
        // No extra logic needed here; DisablePhone() will be called.
    }

    public void OnPhoneDisabled(Phone phone)
    {
        if (phone == activePhone)
            activePhone = null;

        cooldownTimer = phoneCooldown;

        if (populationManager != null)
            populationManager.OnPhoneCleared();
    }
}
