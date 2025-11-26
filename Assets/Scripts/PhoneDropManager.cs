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
    public float spawnHeight   = 5f;
    public float phoneLifetime = 5f;   // 0 = no timeout
    public float phoneCooldown = 3f;
    public float minHorizontalX = -8f;
    public float maxHorizontalX =  8f;

    [Header("Input")]
    public bool enableClickToDrop = true;

    [Header("Debug")]
    public PhoneType currentPhoneType = PhoneType.SocialMediaRed;

    // Runtime state
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

        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (!enableClickToDrop)
            return;

        // Ignore clicks over UI
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
            return;

        if (Input.GetMouseButtonDown(0))
        {
            TryDropPhoneAtMouse();
        }
    }

    // Called by UI buttons (Social / Streaming / Mainstream / Gambling)
    public void SetCurrentPhoneType(PhoneType type)
    {
        currentPhoneType = type;
    }

    // -------------------------------------------------
    // SPAWNING
    // -------------------------------------------------
    private void TryDropPhoneAtMouse()
    {
        if (cooldownTimer > 0f) return;
        if (activePhone != null && activePhone.isActive) return;
        if (phonePrefab == null) return;
        if (mainCamera == null)
        {
            Debug.LogWarning("PhoneDropManager: mainCamera not set.");
            return;
        }

        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        float clampedX = Mathf.Clamp(mouseWorld.x, minHorizontalX, maxHorizontalX);
        Vector3 spawnPos = new Vector3(clampedX, spawnHeight, 0f);

        SpawnPhone(spawnPos, currentPhoneType);
    }

    public void SpawnPhone(Vector3 worldPosition, PhoneType phoneType)
    {
        if (phonePrefab == null)
        {
            Debug.LogWarning("PhoneDropManager: phonePrefab is not set.");
            return;
        }

        if (activePhone != null && activePhone.isActive)
            return;

        GameObject phoneObj = Instantiate(phonePrefab, worldPosition, Quaternion.identity);
        Phone phone = phoneObj.GetComponent<Phone>();
        if (phone == null)
            phone = phoneObj.AddComponent<Phone>();

        // Let the phone know who owns it & what type it is
        phone.Initialize(this, phoneType, phoneLifetime);
        activePhone = phone;
    }

    // -------------------------------------------------
    // EVENTS FROM PHONE
    // -------------------------------------------------

    // Phone hit the ground for the first time
    public void OnPhoneLanded(Phone phone)
    {
        if (productivityManager != null)
            productivityManager.ApplyPhoneDrop();

        if (populationManager != null && phone != null)
            populationManager.OnPhoneDropped(phone);
    }

    // Player clicked/tapped the phone
    public void OnPhoneTapped(Phone phone)
    {
        if (phone == null || !phone.isActive)
            return;

        // Just disable it early
        phone.DisablePhone();
        // DisablePhone() will call OnPhoneDisabled below.
    }

    // Phone is being removed (timeout or tap or villager pickup)
    public void OnPhoneDisabled(Phone phone)
    {
        if (phone == activePhone)
            activePhone = null;

        cooldownTimer = phoneCooldown;

        if (populationManager != null)
            populationManager.OnPhoneCleared();
    }
}
