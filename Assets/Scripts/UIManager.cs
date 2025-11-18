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

    [Header("Phone Selection UI")]
    public PhoneDropManager phoneDropManager;
    public Image socialMediaPhoneBox;   // red phone icon box
    public Image streamingPhoneBox;     // yellow phone icon box
    public Image mainstreamPhoneBox;    // blue phone icon box
    public Image gamblingPhoneBox;      // green phone icon box

    // These are no longer used to override color; we’ll keep them just so Unity
    // doesn’t lose serialized data, but we’ll drive highlight from the base colors.
    public Color selectedPhoneBoxColor   = Color.white;
    public Color deselectedPhoneBoxColor = new Color(1f, 1f, 1f, 0.3f);

    // Store each button's original color (the one you set in the Image)
    private Color socialBaseColor;
    private Color streamingBaseColor;
    private Color mainstreamBaseColor;
    private Color gamblingBaseColor;

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

        if (phoneDropManager == null)
            phoneDropManager = FindObjectOfType<PhoneDropManager>();

        // Cache the original button colors (red, yellow, blue, green)
        if (socialMediaPhoneBox != null)
            socialBaseColor = socialMediaPhoneBox.color;
        if (streamingPhoneBox != null)
            streamingBaseColor = streamingPhoneBox.color;
        if (mainstreamPhoneBox != null)
            mainstreamBaseColor = mainstreamPhoneBox.color;
        if (gamblingPhoneBox != null)
            gamblingBaseColor = gamblingPhoneBox.color;

        // Initialize selection highlight once
        if (phoneDropManager != null)
            UpdatePhoneSelectionUI(phoneDropManager.currentPhoneType);
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

        // --- Current phone type HUD + highlight ---
        if (phoneDropManager == null)
            phoneDropManager = FindObjectOfType<PhoneDropManager>();

        if (phoneDropManager != null)
        {
            if (currentPhoneTypeText != null)
                currentPhoneTypeText.text = $"Current Phone: {phoneDropManager.currentPhoneType}";

            UpdatePhoneSelectionUI(phoneDropManager.currentPhoneType);
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

    // -------------------------------------------------
    // PHONE SELECTION UI – highlight using alpha only
    // -------------------------------------------------

    private void UpdatePhoneSelectionUI(PhoneType selectedType)
    {
        float selectedAlpha = 1f;
        float deselectedAlpha = 0.4f;

        if (socialMediaPhoneBox != null)
        {
            Color c = socialBaseColor;
            c.a = (selectedType == PhoneType.SocialMediaRed) ? selectedAlpha : deselectedAlpha;
            socialMediaPhoneBox.color = c;
        }

        if (streamingPhoneBox != null)
        {
            Color c = streamingBaseColor;
            c.a = (selectedType == PhoneType.StreamingYellow) ? selectedAlpha : deselectedAlpha;
            streamingPhoneBox.color = c;
        }

        if (mainstreamPhoneBox != null)
        {
            Color c = mainstreamBaseColor;
            c.a = (selectedType == PhoneType.MainstreamBlue) ? selectedAlpha : deselectedAlpha;
            mainstreamPhoneBox.color = c;
        }

        if (gamblingPhoneBox != null)
        {
            Color c = gamblingBaseColor;
            c.a = (selectedType == PhoneType.GamblingGreen) ? selectedAlpha : deselectedAlpha;
            gamblingPhoneBox.color = c;
        }
    }

    private void SetSelectedPhoneType(PhoneType type)
    {
        if (phoneDropManager != null)
            phoneDropManager.SetCurrentPhoneType(type);

        UpdatePhoneSelectionUI(type);
    }

    // Hook these up to the 4 UI buttons in the Inspector
    public void OnSelectSocialMediaPhone()
    {
        SetSelectedPhoneType(PhoneType.SocialMediaRed);
    }

    public void OnSelectStreamingPhone()
    {
        SetSelectedPhoneType(PhoneType.StreamingYellow);
    }

    public void OnSelectMainstreamPhone()
    {
        SetSelectedPhoneType(PhoneType.MainstreamBlue);
    }

    public void OnSelectGamblingPhone()
    {
        SetSelectedPhoneType(PhoneType.GamblingGreen);
    }
}
