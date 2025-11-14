using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Villager : MonoBehaviour
{
    public PersonState currentState = PersonState.Working;

    [Header("Visuals")]
    public Animator animator;              // optional
    public SpriteRenderer spriteRenderer;  // required

    private void Reset()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void UpdateStateFromProductivity(ProductivityBand band, float productivity)
    {
        PersonState newState = currentState;

        switch (band)
        {
            case ProductivityBand.Thriving:
                newState = PersonState.Working;
                break;

            case ProductivityBand.Declining:
                if (productivity >= 50f)
                    newState = PersonState.ShiftingAttention;
                else if (productivity >= 25f)
                    newState = PersonState.PhoneAddiction;
                else if (productivity > 0f)
                    newState = PersonState.Idle;
                break;

            case ProductivityBand.Collapse:
                newState = PersonState.Destructive;
                break;
        }

        SetState(newState);
    }

    public void SetState(PersonState newState)
    {
        if (currentState == newState) return;

        currentState = newState;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        // Animator hook (optional)
        if (animator != null)
        {
            animator.SetInteger("State", (int)currentState);
        }

        // Color feedback (works even without Animator)
        if (spriteRenderer != null)
        {
            switch (currentState)
            {
                case PersonState.Working:
                    spriteRenderer.color = Color.green;
                    break;
                case PersonState.ShiftingAttention:
                    spriteRenderer.color = Color.yellow;
                    break;
                case PersonState.PhoneAddiction:
                    spriteRenderer.color = new Color(1f, 0.5f, 0f); // orange
                    break;
                case PersonState.Idle:
                    spriteRenderer.color = Color.gray;
                    break;
                case PersonState.Destructive:
                    spriteRenderer.color = Color.red;
                    break;
            }
        }
    }
}
