using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("References")]
    public ProductivityManager productivityManager;
    public GameManager gameManager;
    public PopulationManager populationManager;

    [Header("HUD")]
    public TextMeshProUGUI timeText;        // drag TimeText here
    public TextMeshProUGUI statusText;      // drag StatusText here
    public Slider productivitySlider;       // used as population bar (0–1)
    public TextMeshProUGUI productivityText;

    [Header("Game Over")]
    public GameObject gameOverPanel;        // the panel object
    public TextMeshProUGUI finalTimeText;   // “You lasted X seconds”
    public TextMeshProUGUI bestTimeText;    // “Best time: Y seconds”

    private bool gameOverShown = false;

    // Smoothed display value for the population bar (0–100%)
    private float displayedPopulationPercent = 100f;

    void Start()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (statusText != null)
            statusText.text = "Status: Running";

        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();

        if (productivityManager == null)
            productivityManager = FindObjectOfType<ProductivityManager>();

        if (populationManager == null)
            populationManager = FindObjectOfType<PopulationManager>();

        // Initialise bar full
        if (productivitySlider != null)
        {
            // Expect slider min/max to be 0–1
            productivitySlider.normalizedValue = 1f;
        }

        if (productivityText != null)
            productivityText.text = "Population: 100%";
    }

    void Update()
    {
        if (gameManager == null) return;

        // --- Time alive HUD ---
        if (timeText != null)
            timeText.text = $"Time Alive: {gameManager.timeAlive:F1}s";

        // --- Population bar based on phone addiction ---
        float targetPopulationPercent = GetHealthyPopulationPercent();

        // Smoothly move the bar toward the target so it “slowly drops”
        displayedPopulationPercent =
            Mathf.Lerp(displayedPopulationPercent, targetPopulationPercent, 3f * Time.deltaTime);

        if (productivitySlider != null)
            productivitySlider.normalizedValue = Mathf.Clamp01(displayedPopulationPercent / 100f);

        if (productivityText != null)
            productivityText.text = $"Population: {displayedPopulationPercent:F0}%";

        // --- Game Over UI & Status text ---
        if (gameManager.isGameOver)
        {
            HandleGameOverUI();
        }
        else
        {
            gameOverShown = false;

            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);

            if (statusText != null)
                statusText.text = BuildStatusMessage(targetPopulationPercent);
        }
    }

    private void HandleGameOverUI()
    {
        if (gameOverShown) return;
        gameOverShown = true;

        if (statusText != null)
            statusText.text = "Status: Game Over";

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (finalTimeText != null)
            finalTimeText.text = $"You lasted {gameManager.timeAlive:F1} seconds";

        float bestTime = PlayerPrefs.GetFloat("BestTime", 0f);
        if (gameManager.timeAlive > bestTime)
        {
            bestTime = gameManager.timeAlive;
            PlayerPrefs.SetFloat("BestTime", bestTime);
        }

        if (bestTimeText != null)
            bestTimeText.text = $"Best time: {bestTime:F1} s";
    }

    // Called by the Restart button OnClick()
    public void OnResetButtonPressed()
    {
        if (gameManager != null)
            gameManager.ResetGame();
    }

    // Optional: external callers can push a population percentage [0–100]
    public void UpdateProductivityUI(float value)
    {
        displayedPopulationPercent = value;

        if (productivitySlider != null)
            productivitySlider.normalizedValue = Mathf.Clamp01(value / 100f);

        if (productivityText != null)
            productivityText.text = $"Population: {value:F0}%";
    }

    // ----------------- Helper methods -----------------

    // 0–100% of villagers who are NOT phone addicted
    private float GetHealthyPopulationPercent()
    {
        if (populationManager == null)
            return 100f;

        int total = populationManager.GetTotalVillagerCount();
        if (total <= 0)
            return 0f;

        int addicted = populationManager.GetPhoneAddictedCount();
        float healthyFraction = 1f - (float)addicted / total;
        healthyFraction = Mathf.Clamp01(healthyFraction);

        return healthyFraction * 100f;
    }

    private string BuildStatusMessage(float populationPercent)
    {
        // Optionally factor in productivity band as a "mood"
        ProductivityBand band = productivityManager != null
            ? productivityManager.GetBand()
            : ProductivityBand.Thriving;

        if (populationPercent >= 75f)
        {
            switch (band)
            {
                case ProductivityBand.Thriving:
                    return "Status: Thriving – everyone focused";
                case ProductivityBand.Declining:
                    return "Status: Busy but distracted";
                default:
                    return "Status: Holding on";
            }
        }
        else if (populationPercent >= 40f)
        {
            return "Status: Warning – phone distractions rising";
        }
        else
        {
            return "Status: Crisis – most of the population is addicted";
        }
    }
}
