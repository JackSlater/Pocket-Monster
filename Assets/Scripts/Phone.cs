using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Phone : MonoBehaviour
{
    public bool isActive = true;

    // NEW: true once we hit the ground
    public bool hasLanded = false;

    // Which phone this instance is (red/yellow/blue/green)
    public PhoneType phoneType = PhoneType.SocialMediaRed;

    private PhoneDropManager manager;
    private Rigidbody2D rb;

    private void Awake()
    {
        manager = FindObjectOfType<PhoneDropManager>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnMouseDown()
    {
        if (!isActive) return;

        if (manager != null)
        {
            manager.OnPhoneTapped(this);
        }

        DisablePhone();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isActive) return;

        if (collision.collider.CompareTag("Ground"))
        {
            hasLanded = true; // <- now villagers are allowed to interact

            if (manager != null)
            {
                manager.OnPhoneLanded(this);
            }

            // Sit on the ground
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.bodyType = RigidbodyType2D.Static;
            }
        }
    }

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
