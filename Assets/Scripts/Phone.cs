using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Phone : MonoBehaviour
{
    [Header("State")]
    public bool isActive = true;
    public bool hasLanded = false;
    public PhoneType phoneType = PhoneType.SocialMediaRed;

    private PhoneDropManager manager;
    private float lifetimeSeconds;

    // Cached components
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private BoxCollider2D box;

    private void Awake()
    {
        rb   = GetComponent<Rigidbody2D>();
        sr   = GetComponent<SpriteRenderer>();
        box  = GetComponent<BoxCollider2D>();

        // Simple physics setup
        rb.gravityScale = 1f;
        rb.simulated = true;
    }

    /// <summary>
    /// Called immediately after the phone is instantiated by PhoneDropManager.
    /// </summary>
    public void Initialize(PhoneDropManager owner, PhoneType type, float lifetime)
    {
        manager        = owner;
        phoneType      = type;
        lifetimeSeconds = lifetime;

        ApplyColor();

        if (lifetimeSeconds > 0f)
            StartCoroutine(LifetimeRoutine());
    }

    private IEnumerator LifetimeRoutine()
    {
        float t = lifetimeSeconds;
        while (t > 0f && isActive)
        {
            t -= Time.deltaTime;
            yield return null;
        }

        // Timed out while still active → just disable it
        if (isActive)
            DisablePhone();
    }

    private void ApplyColor()
    {
        if (sr == null) return;

        Color c = Color.white;
        switch (phoneType)
        {
            case PhoneType.SocialMediaRed:   c = Color.red;    break;
            case PhoneType.StreamingYellow:  c = Color.yellow; break;
            case PhoneType.MainstreamBlue:   c = Color.blue;   break;
            case PhoneType.GamblingGreen:    c = Color.green;  break;
        }
        sr.color = c;
    }

    // -------------------------------
    // COLLISION (hit ground)
    // -------------------------------
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isActive) return;
        if (hasLanded) return;

        // Make sure your ground object has tag "Ground"
        if (collision.collider.CompareTag("Ground"))
        {
            hasLanded = true;

            if (manager != null)
                manager.OnPhoneLanded(this);
        }
    }

    // -------------------------------
    // CLICK TO DISMISS
    // -------------------------------
    private void OnMouseDown()
    {
        if (!isActive) return;

        if (manager != null)
            manager.OnPhoneTapped(this);
    }

    // -------------------------------
    // DISABLE / DESTROY
    // -------------------------------
    public void DisablePhone()
    {
        if (!isActive) return;

        isActive = false;

        if (manager != null)
            manager.OnPhoneDisabled(this);

        Destroy(gameObject);
    }
}
