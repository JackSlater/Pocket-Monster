using UnityEngine;

public class ProductivityManager : MonoBehaviour
{
    [Header("Productivity Settings")]
    public float baseProductivity = 100f;
    [Range(0f, 1f)]
    public float startingFactor = 1f;     // starts full strength
    [Range(0f, 1f)]
    public float phoneDecayFactor = 0.9f; // multiply per phone drop

    public float CurrentProductivity { get; private set; }
    public float ProductivityFactor  { get; private set; }

    private void Awake()
    {
        Initialize();
    }

    public void Initialize()
    {
        ProductivityFactor = startingFactor;
        UpdateProductivity();
    }

    public void ApplyPhoneDrop()
    {
        ProductivityFactor *= phoneDecayFactor;
        UpdateProductivity();
    }

    private void UpdateProductivity()
    {
        CurrentProductivity = baseProductivity * ProductivityFactor;
        CurrentProductivity = Mathf.Max(0f, CurrentProductivity);
        // You can Debug.Log here if you want:
        // Debug.Log($"Productivity: {CurrentProductivity}");
    }

    public ProductivityBand GetBand()
    {
        if (CurrentProductivity >= 75f)
            return ProductivityBand.Thriving;
        if (CurrentProductivity > 0f)
            return ProductivityBand.Declining;
        return ProductivityBand.Collapse;
    }
}
