using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Phone : MonoBehaviour
{
    public bool isActive = true;

    // True once phone hits ground
    public bool hasLanded = false;

    // Phone category (color-coded)
    public PhoneType phoneType = PhoneType.SocialMediaRed;

    private PhoneDropManager manager;
    private Rigidbody2D rb;
    private SpriteRenderer sr;

    private void Awake()
    {
        manager = FindObjectOfType<PhoneDropManager>();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        ApplyColor();
    }

    // -------------------------------
    // COLOR LOGIC
    // -------------------------------
    private void ApplyColor()
    {
        if (sr == null) return;

        sr.color = GetColorForType(phoneType);
    }

    private Color GetColorForType(PhoneType type)
    {
        switch (type)
        {
            case PhoneType.SocialMediaRed:
                return Color.red;

            case PhoneType.StreamingYellow:
                return Color.yellow;

            case PhoneType.MainstreamBlue:
                return Color.blue; // or Color.cyan if preferred

            case PhoneType.GamblingGreen:
                return Color.green;
        }

        return Color.white;
    }

    // -------------------------------
    // INPUT (tap to delete)
    // -------------------------------
    private void OnMouseDown()
    {
        if (!isActive) return;

        if (manager != null)
        {
            manager.OnPhoneTapped(this);
        }

        DisablePhone();
    }

    // -------------------------------
    // COLLISION (hit ground)
    // -------------------------------
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isActive) return;

        if (collision.collider.CompareTag("Ground"))
        {
            hasLanded = true;

            if (manager != null)
            {
                manager.OnPhoneLanded(this);
            }

            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.bodyType = RigidbodyType2D.Static;
            }
        }
    }

    // -------------------------------
    // REMOVAL
    // -------------------------------
    public void DisablePhone()
    {
        if (!isActive) return;

        isActive = false;

        if (manager != null)
        {
            manager.OnPhoneDisabled(this);
        }

        Destroy(gameObject);
    }
}
