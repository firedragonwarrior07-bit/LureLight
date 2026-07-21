using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// GameOver UI Handler - Fully integrated with your GameManager
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Text finalScoreText;
    [SerializeField] private Text finalTimeText;
    [SerializeField] private Text highScoreText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Upgrade Shop Reference")]
    [SerializeField] private UpgradeShop upgradeShop;

    [Header("Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private float restartCooldown = 0.5f;

    private SurvivalTimer survivalTimer;
    private bool isRestarting = false;
    private float highScore = 0f;

    void Start()
    {
        // HIDE GAME OVER ON START
        gameObject.SetActive(false);

        survivalTimer = FindObjectOfType<SurvivalTimer>();
        LoadHighScore();

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(GoToMainMenu);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerDied += OnPlayerDied;
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerDied -= OnPlayerDied;
        }
    }

    void OnPlayerDied()
    {
        ShowGameOver();
    }

    void LoadHighScore()
    {
        highScore = PlayerPrefs.GetFloat("SurvivalHighScore", 0f);
    }

    void SaveHighScore(float time)
    {
        if (time > highScore)
        {
            highScore = time;
            PlayerPrefs.SetFloat("SurvivalHighScore", highScore);
            PlayerPrefs.Save();
            Debug.Log($"New High Score: {FormatTime(highScore)}");
        }
    }

    string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    void UpdateUI()
    {
        // Update final score
        if (finalScoreText != null && GameManager.Instance != null)
        {
            finalScoreText.text = $"Final Score: {GameManager.Instance.Score}";
        }

        // Update final time and high score
        if (survivalTimer != null)
        {
            float currentTime = survivalTimer.GetElapsedTime();
            
            // Update final time text
            if (finalTimeText != null)
            {
                finalTimeText.text = $"Time Survived: {FormatTime(currentTime)}";
            }

            // Save high score if beaten
            SaveHighScore(currentTime);

            // Update high score text
            if (highScoreText != null)
            {
                highScoreText.text = $"Best Time: {FormatTime(highScore)}";
                
                // Check if it's a new best
                if (currentTime >= highScore && currentTime > 0)
                {
                    highScoreText.text = $"🏆 NEW BEST! {FormatTime(highScore)} 🏆";
                    highScoreText.color = Color.yellow;
                }
                else
                {
                    highScoreText.color = Color.white;
                }
            }
        }
    }

    void RestartGame()
    {
        if (isRestarting) return;
        isRestarting = true;

        Time.timeScale = 1f;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetGame();
        }
        else
        {
            ResetManually();
        }

        // Reset cooldown
        Invoke(nameof(ResetRestartCooldown), restartCooldown);
    }

    void ResetRestartCooldown()
    {
        isRestarting = false;
    }

    void ResetManually()
    {
        if (upgradeShop != null)
        {
            upgradeShop.ResetUpgrades();
        }
        else
        {
            string savePath = System.IO.Path.Combine(Application.persistentDataPath, "upgrades_lurelight.json");
            if (System.IO.File.Exists(savePath))
                System.IO.File.Delete(savePath);
            
            PlayerPrefs.DeleteKey("LurelightSaveData");
            PlayerPrefs.Save();
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void GoToMainMenu()
    {
        Time.timeScale = 1f;

        // Reset high score display
        if (highScoreText != null)
        {
            highScoreText.color = Color.white;
        }

        // Load main menu scene
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void ShowGameOver()
    {
        gameObject.SetActive(true);
        UpdateUI();
        Debug.Log("Game Over UI Shown!");
    }

    public void HideGameOver()
    {
        gameObject.SetActive(false);
        Debug.Log("Game Over UI Hidden!");
    }

    // Optional: Reset high score (for testing)
    public void ResetHighScore()
    {
        PlayerPrefs.DeleteKey("SurvivalHighScore");
        highScore = 0f;
        if (highScoreText != null)
        {
            highScoreText.text = "Best Time: 00:00";
            highScoreText.color = Color.white;
        }
        Debug.Log("High Score Reset!");
    }
}