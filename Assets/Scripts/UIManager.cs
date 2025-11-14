using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("References")]
    public ProductivityManager productivityManager;
    public GameManager gameManager;

    [Header("HUD")]
    public TextMeshProUGUI timeText;        // drag TimeText here
    public TextMeshProUGUI statusText;      // drag StatusText here
    public Slider productivitySlider;       // optional: a UI Slider bar
    public TextMeshProUGUI productivityText;

    [Header("Game Over")]
    public GameObject gameOverPanel;        // the panel object
    public TextMeshProUGUI finalTimeText;   // “You lasted X seconds”
    public TextMeshProUGUI bestTimeText;    // “Best time: Y seconds”

    private bool gameOverShown = false;

    void Start()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (statusText != null)
            statusText.text = "Status: Running";
    }

    void Update()
    {
        if (gameManager == null) return;

        // --- Time alive HUD ---
        if (timeText != null)
            timeText.text = $"Time Alive: {gameManager.timeAlive:F1}s";

        // --- Productivity HUD (optional) ---
        if (productivityManager != null)
        {
            float p = productivityManager.CurrentProductivity;  // assumes you expose this
            if (productivitySlider != null)
                productivitySlider.value = p;                    // slider min/max set in Inspector

            if (productivityText != null)
                productivityText.text = $"Productivity: {p:F0}%";
        }

        // --- Game Over UI ---
        if (gameManager.isGameOver)
        {
            if (!gameOverShown)
            {
                gameOverShown = true;

                if (statusText != null)
                    statusText.text = "Status: Game Over";

                if (gameOverPanel != null)
                    gameOverPanel.SetActive(true);

                if (finalTimeText != null)
                    finalTimeText.text = $"You lasted {gameManager.timeAlive:F1} seconds";

                // Best time could be stored in PlayerPrefs or GameManager; example:
                float bestTime = PlayerPrefs.GetFloat("BestTime", 0f);
                if (gameManager.timeAlive > bestTime)
                {
                    bestTime = gameManager.timeAlive;
                    PlayerPrefs.SetFloat("BestTime", bestTime);
                }

                if (bestTimeText != null)
                    bestTimeText.text = $"Best time: {bestTime:F1} s";
            }
        }
        else
        {
            // back to running state
            if (statusText != null)
                statusText.text = "Status: Running";

            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);

            gameOverShown = false;
        }
    }

    // Called by the Restart button OnClick()
    public void OnResetButtonPressed()
    {
        if (gameManager != null)
            gameManager.ResetGame();
    }

    // Optional: if ProductivityManager wants to push updates directly
    public void UpdateProductivityUI(float value)
    {
        if (productivitySlider != null)
            productivitySlider.value = value;

        if (productivityText != null)
            productivityText.text = $"Productivity: {value:F0}%";
    }
}
