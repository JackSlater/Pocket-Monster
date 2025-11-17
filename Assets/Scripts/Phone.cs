using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Phone : MonoBehaviour
{
    public bool isActive = true;

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

        // We only care about hitting the ground, which should be tagged "Ground"
        if (collision.collider.CompareTag("Ground"))
        {
            if (manager != null)
            {
                manager.OnPhoneLanded(this);
            }

            // Stop moving so it sits on the ground
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
