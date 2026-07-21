using UnityEngine;
using UnityEngine.UI;

public class ScoreUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Text scoreText;
    [SerializeField] private string prefix = "Orbs: ";

    void Start()
    {
        // Subscribe to score change event
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged += UpdateUI;
        }
        else
        {
            Debug.LogWarning("GameManager not found! ScoreUI will not update.");
        }

        // Initial update
        UpdateUI();
    }

    void UpdateUI()
    {
        if (scoreText != null && GameManager.Instance != null)
        {
            scoreText.text = prefix + GameManager.Instance.Score;
        }
    }

    void OnDestroy()
    {
        // Clean up event subscription
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged -= UpdateUI;
        }
    }

    // Optional: Manually refresh UI
    public void RefreshUI()
    {
        UpdateUI();
    }
}