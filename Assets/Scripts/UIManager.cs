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
    public TextMeshProUGUI bestTimeHUDText;    // shows "Best Time: xx.xs" during play

    [Header("Game Over")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalTimeText;
    public TextMeshProUGUI bestTimeText;       // shows "High Score: xx.x seconds" on panel
    public Button restartButton;

    [Header("Phone HUD (optional)")]
    public TextMeshProUGUI currentPhoneTypeText;

    [Header("Phone Selection UI")]
    public PhoneDropManager phoneDropManager;
    public Image socialMediaPhoneBox;   // red phone icon box
    public Image streamingPhoneBox;     // yellow phone icon box
    public Image mainstreamPhoneBox;    // blue phone icon box
    public Image gamblingPhoneBox;      // green phone icon box

    public Color selectedPhoneBoxColor   = Color.white;
    public Color deselectedPhoneBoxColor = new Color(1f, 1f, 1f, 0.3f);

    // original button colors
    private Color socialBaseColor;
    private Color streamingBaseColor;
    private Color mainstreamBaseColor;
    private Color gamblingBaseColor;

    // --- HIGH SCORE PERSISTENCE ---
    private const string BestTimeKey = "BestTime";
    private float bestTime = 0f;
    private bool hasSavedBestThisRun = false;

    // -------------------------------
    // HOW TO PLAY / INTRO
    // -------------------------------
    [Header("How To Play / Intro")]
    public GameObject howToPlayPanel;          // full-screen infographic panel
    public Button howToPlayPlayButton;         // "Play" button at bottom of infographic
    public bool showHowToPlayOnStart = true;   // show on scene load?
    private bool isShowingHowToPlay = false;

    private void Start()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();
        if (populationManager == null)
            populationManager = FindObjectOfType<PopulationManager>();
        if (productivityManager == null)
            productivityManager = FindObjectOfType<ProductivityManager>();

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

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(OnResetButtonPressed);
        }

        if (phoneDropManager == null)
            phoneDropManager = FindObjectOfType<PhoneDropManager>();

        if (socialMediaPhoneBox != null)
            socialBaseColor = socialMediaPhoneBox.color;
        if (streamingPhoneBox != null)
            streamingBaseColor = streamingPhoneBox.color;
        if (mainstreamPhoneBox != null)
            mainstreamBaseColor = mainstreamPhoneBox.color;
        if (gamblingPhoneBox != null)
            gamblingBaseColor = gamblingPhoneBox.color;

        if (phoneDropManager != null)
            UpdatePhoneSelectionUI(phoneDropManager.currentPhoneType);

        // --- LOAD PERSISTENT BEST TIME ---
        bestTime = PlayerPrefs.GetFloat(BestTimeKey, 0f);
        UpdateBestTimeTexts();

        // -------------------------------
        // HOW TO PLAY SETUP
        // -------------------------------
        if (howToPlayPlayButton != null)
        {
            howToPlayPlayButton.onClick.RemoveAllListeners();
            howToPlayPlayButton.onClick.AddListener(OnHowToPlayPlayPressed);
        }

        if (showHowToPlayOnStart && howToPlayPanel != null)
        {
            isShowingHowToPlay = true;
            howToPlayPanel.SetActive(true);
            Time.timeScale = 0f;

            if (phoneDropManager != null)
                phoneDropManager.enabled = false;
        }
        else
        {
            isShowingHowToPlay = false;
            if (howToPlayPanel != null)
                howToPlayPanel.SetActive(false);

            Time.timeScale = 1f;

            if (phoneDropManager != null)
                phoneDropManager.enabled = true;
        }
    }

    private void Update()
    {
        if (gameManager == null) return;

        // Time HUD
        if (timeText != null)
            timeText.text = $"Time Alive: {gameManager.timeAlive:F1}s";

        // Population HUD
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

        // Phone HUD
        if (phoneDropManager == null)
            phoneDropManager = FindObjectOfType<PhoneDropManager>();

        if (phoneDropManager != null)
        {
            if (currentPhoneTypeText != null)
                currentPhoneTypeText.text = $"Current Phone: {phoneDropManager.currentPhoneType}";

            UpdatePhoneSelectionUI(phoneDropManager.currentPhoneType);
        }

        // Game over + high score
        if (gameManager.isGameOver)
        {
            if (gameOverPanel != null && !gameOverPanel.activeSelf)
                gameOverPanel.SetActive(true);

            if (finalTimeText != null)
                finalTimeText.text = $"You lasted {gameManager.timeAlive:F1} seconds";

            if (gameManager.timeAlive > bestTime)
            {
                bestTime = gameManager.timeAlive;

                if (!hasSavedBestThisRun)
                {
                    PlayerPrefs.SetFloat(BestTimeKey, bestTime);
                    PlayerPrefs.Save();
                    hasSavedBestThisRun = true;
                }
            }

            UpdateBestTimeTexts();
        }
        else
        {
            if (gameOverPanel != null && gameOverPanel.activeSelf)
                gameOverPanel.SetActive(false);
        }
    }

    // Restart button
    public void OnResetButtonPressed()
    {
        if (gameManager != null)
        {
            Debug.Log("UIManager: Restart button pressed.");
            gameManager.ResetGame();
            hasSavedBestThisRun = false;
        }
    }

    // How To Play Play button
    public void OnHowToPlayPlayPressed()
    {
        if (!isShowingHowToPlay)
            return;

        isShowingHowToPlay = false;

        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(false);

        Time.timeScale = 1f;

        if (phoneDropManager != null)
            phoneDropManager.enabled = true;
    }

    // --- Status line based on population percent ---
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

    // --- Phone selection highlight ---
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

    public void OnSelectSocialMediaPhone()  => SetSelectedPhoneType(PhoneType.SocialMediaRed);
    public void OnSelectStreamingPhone()    => SetSelectedPhoneType(PhoneType.StreamingYellow);
    public void OnSelectMainstreamPhone()   => SetSelectedPhoneType(PhoneType.MainstreamBlue);
    public void OnSelectGamblingPhone()     => SetSelectedPhoneType(PhoneType.GamblingGreen);

    // Update both HUD and Game Over best-time labels
    private void UpdateBestTimeTexts()
    {
        if (bestTimeText != null)
            bestTimeText.text = $"High Score: {bestTime:F1} seconds";

        if (bestTimeHUDText != null)
            bestTimeHUDText.text = $"Best Time: {bestTime:F1}s";
    }
}
