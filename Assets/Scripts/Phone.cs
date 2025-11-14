using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Phone : MonoBehaviour
{
    public bool isActive = true;
    private PhoneDropManager manager;

    void Awake()
    {
        manager = FindObjectOfType<PhoneDropManager>();
    }

    void OnMouseDown()
    {
        if (!isActive) return;

        if (manager != null)
        {
            manager.OnPhoneTapped(this);
        }

        Destroy(gameObject);
    }

    public void DisablePhone()
    {
        isActive = false;
        Destroy(gameObject);
    }
}
