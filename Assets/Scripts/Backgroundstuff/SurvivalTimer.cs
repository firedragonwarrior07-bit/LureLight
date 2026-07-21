using UnityEngine;
using UnityEngine.UI;

public class SurvivalTimer : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Text timerText;
    [SerializeField] private string timeFormat = "{0:00}:{1:00}";
    [SerializeField] private bool showMilliseconds = false;

    [Header("Settings")]
    [SerializeField] private bool startOnAwake = true;
    [SerializeField] private float elapsedTime = 0f;

    [Header("High Score")]
    [SerializeField] private bool saveHighScore = true;
    [SerializeField] private string highScoreKey = "SurvivalHighScore";
    [SerializeField] private Text highScoreText;

    // State
    private bool isRunning = false;
    private bool isPaused = false;
    private float highScore = 0f;

    // Events
    public System.Action<float> OnTimerUpdate;
    public System.Action<float> OnTimerStop;
    public System.Action<float> OnHighScoreBeat;

    void Start()
    {
        // Find text component if not assigned
        if (timerText == null)
            timerText = GetComponent<Text>();

        // Load high score
        if (saveHighScore)
            LoadHighScore();

        // Start timer
        if (startOnAwake)
            StartTimer();

        // Initial UI update
        UpdateUI();
        UpdateHighScoreUI();
    }

    void Update()
    {
        if (!isRunning || isPaused) return;

        // Check if game is not game over
        if (GameManager.Instance != null && GameManager.Instance.currentState != GameManager.GameState.GameOver)
        {
            elapsedTime += Time.deltaTime;
            UpdateUI();
            OnTimerUpdate?.Invoke(elapsedTime);

            // Check high score
            if (saveHighScore && elapsedTime > highScore)
            {
                highScore = elapsedTime;
                SaveHighScore();
                UpdateHighScoreUI();
                OnHighScoreBeat?.Invoke(highScore);
            }
        }
    }

    void UpdateUI()
    {
        if (timerText != null)
        {
            if (showMilliseconds)
            {
                int minutes = Mathf.FloorToInt(elapsedTime / 60);
                int seconds = Mathf.FloorToInt(elapsedTime % 60);
                int milliseconds = Mathf.FloorToInt((elapsedTime * 100) % 100);
                timerText.text = string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, milliseconds);
            }
            else
            {
                int minutes = Mathf.FloorToInt(elapsedTime / 60);
                int seconds = Mathf.FloorToInt(elapsedTime % 60);
                timerText.text = string.Format(timeFormat, minutes, seconds);
            }
        }
    }

    void UpdateHighScoreUI()
    {
        if (highScoreText != null)
        {
            int minutes = Mathf.FloorToInt(highScore / 60);
            int seconds = Mathf.FloorToInt(highScore % 60);
            highScoreText.text = $"Best: {minutes:00}:{seconds:00}";
        }
    }

    // ===== PUBLIC METHODS =====

    public void StartTimer()
    {
        isRunning = true;
        isPaused = false;
        Debug.Log("⏱️ Timer started!");
    }

    public void StopTimer()
    {
        isRunning = false;
        isPaused = false;
        OnTimerStop?.Invoke(elapsedTime);
        Debug.Log($"⏱️ Timer stopped! Time: {GetFormattedTime()}");
    }

    public void ResetTimer()
    {
        elapsedTime = 0f;
        isRunning = true;
        isPaused = false;
        UpdateUI();
        Debug.Log("⏱️ Timer reset!");
    }

    public void PauseTimer()
    {
        if (isRunning)
        {
            isPaused = true;
            Debug.Log("⏱️ Timer paused.");
        }
    }

    public void ResumeTimer()
    {
        if (isPaused)
        {
            isPaused = false;
            Debug.Log("⏱️ Timer resumed.");
        }
    }

    public float GetElapsedTime()
    {
        return elapsedTime;
    }

    public string GetFormattedTime()
    {
        int minutes = Mathf.FloorToInt(elapsedTime / 60);
        int seconds = Mathf.FloorToInt(elapsedTime % 60);
        return string.Format(timeFormat, minutes, seconds);
    }

    public string GetFormattedTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        return string.Format(timeFormat, minutes, seconds);
    }

    public bool IsRunning()
    {
        return isRunning;
    }

    public bool IsPaused()
    {
        return isPaused;
    }

    public float GetHighScore()
    {
        return highScore;
    }

    public bool IsHighScoreBeat()
    {
        return elapsedTime > highScore && elapsedTime > 0;
    }

    // ===== SAVE / LOAD HIGH SCORE =====

    void LoadHighScore()
    {
        highScore = PlayerPrefs.GetFloat(highScoreKey, 0f);
        Debug.Log($"High score loaded: {GetFormattedTime(highScore)}");
    }

    void SaveHighScore()
    {
        PlayerPrefs.SetFloat(highScoreKey, highScore);
        PlayerPrefs.Save();
        Debug.Log($"New high score saved: {GetFormattedTime(highScore)}");
    }

    public void ResetHighScore()
    {
        highScore = 0f;
        SaveHighScore();
        UpdateHighScoreUI();
        Debug.Log("High score reset!");
    }

    // ===== UI UPDATE METHODS =====

    public void SetTimerText(Text newText)
    {
        timerText = newText;
        UpdateUI();
    }

    public void SetHighScoreText(Text newText)
    {
        highScoreText = newText;
        UpdateHighScoreUI();
    }
}