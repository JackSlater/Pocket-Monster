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
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI statusText;
    public Slider productivitySlider;   // population bar 0–1
    public TextMeshProUGUI productivityText;

    [Header("Game Over")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalTimeText;
    public TextMeshProUGUI bestTimeText;
    public Button restartButton;        // <-- assign in Inspector

    [Header("Phone HUD (optional)")]
    public TextMeshProUGUI currentPhoneTypeText;

    private float bestTime = 0f;

    private void Start()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (productivitySlider != null)
        {
            productivitySlider.minValue = 0f;
            productivitySlider.maxValue = 1f;
            productivitySlider.value = 1f;
        }

        if (productivityText != null)
            productivityText.text = "Population: 100%";

        // Wire up restart button in code so we don't rely on Inspector events
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(OnResetButtonPressed);
        }
    }

    private void Update()
    {
        if (gameManager == null) return;

        // --- Time alive HUD ---
        if (timeText != null)
            timeText.text = $"Time Alive: {gameManager.timeAlive:F1}s";

        // --- Population HUD using PopulationManager ---
        float populationPercent = 100f;
        if (populationManager != null)
        {
            int total = populationManager.GetTotalVillagerCount();
            int addicted = populationManager.GetPhoneAddictedCount();

            float healthyFraction = 1f;
            if (total > 0)
            {
                healthyFraction = Mathf.Clamp01((float)(total - addicted) / total);
                populationPercent = healthyFraction * 100f;
            }
            else
            {
                healthyFraction = 0f;
                populationPercent = 0f;
            }

            if (productivitySlider != null)
                productivitySlider.value = healthyFraction;

            if (productivityText != null)
                productivityText.text = $"Population: {populationPercent:F0}%";

            if (statusText != null)
                statusText.text = BuildStatusText(populationPercent);
        }

        // --- Current phone type HUD (optional) ---
        if (currentPhoneTypeText != null)
        {
            var pdm = FindObjectOfType<PhoneDropManager>();
            if (pdm != null)
                currentPhoneTypeText.text = $"Current Phone: {pdm.currentPhoneType}";
        }

        // --- Game over UI ---
        if (gameManager.isGameOver)
        {
            if (gameOverPanel != null && !gameOverPanel.activeSelf)
                gameOverPanel.SetActive(true);

            if (finalTimeText != null)
                finalTimeText.text = $"You lasted {gameManager.timeAlive:F1} seconds";

            if (gameManager.timeAlive > bestTime)
                bestTime = gameManager.timeAlive;

            if (bestTimeText != null)
                bestTimeText.text = $"Best: {bestTime:F1} s";
        }
        else
        {
            if (gameOverPanel != null && gameOverPanel.activeSelf)
                gameOverPanel.SetActive(false);
        }
    }

    // called by restartButton.onClick
    public void OnResetButtonPressed()
    {
        if (gameManager != null)
        {
            Debug.Log("UIManager: Restart button pressed.");
            gameManager.ResetGame();
        }
    }

    private string BuildStatusText(float populationPercent)
    {
        if (populationPercent >= 80f)
            return "Status: Thriving – most villagers are productive";
        if (populationPercent >= 60f)
            return "Status: Stable – distractions are manageable";
        if (populationPercent >= 40f)
            return "Status: Warning – phone distractions rising";
        if (populationPercent >= 20f)
            return "Status: Crisis – majority are addicted";

        return "Status: Collapse – society is falling apart";
    }
}
