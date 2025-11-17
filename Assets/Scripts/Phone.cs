using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Phone : MonoBehaviour
{
    public bool isActive = true;

    private PhoneDropManager manager;
    private bool hasLanded = false;
    public bool HasLanded => hasLanded;

    private void Awake()
    {
        manager = FindObjectOfType<PhoneDropManager>();

        // Make sure collider is NOT a trigger so physics will stop on ground
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.isTrigger = false;
        }
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

    // Called when the phone hits something solid (e.g. Ground)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasLanded) return;

        // Only care about the ground
        if (!collision.collider.CompareTag("Ground"))
            return;

        hasLanded = true;

        // Stop falling / bouncing
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.gravityScale = 0f;
        }
    }

    public void DisablePhone()
    {
        if (!isActive) return;

        isActive = false;
        Destroy(gameObject);
    }
}
