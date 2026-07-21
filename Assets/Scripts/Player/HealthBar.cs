using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image healthFill;
    [SerializeField] private Text healthText;

    [Header("Colors")]
    [SerializeField] private Color healthyColor = Color.green;
    [SerializeField] private Color damagedColor = Color.yellow;
    [SerializeField] private Color criticalColor = Color.red;
    [SerializeField] private Color flashColor = Color.white;

    [Header("Settings")]
    [SerializeField] private bool smoothFill = true;
    [SerializeField] private float fillSmoothSpeed = 5f;
    [SerializeField] private bool showText = true;
    [SerializeField] private string textFormat = "{0} / {1}";

    [Header("Effects")]
    [SerializeField] private float damageFlashDuration = 0.15f;
    [SerializeField] private float lowHealthPulseSpeed = 2f;
    [SerializeField] private bool pulseOnLowHealth = true;

    // Private variables
    private float targetFill = 1f;
    private float currentFill = 1f;
    private Coroutine flashCoroutine;
    private bool isLowHealth = false;
    private Color currentColor;

    void Start()
    {
        // Subscribe to events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnHealthChanged += UpdateUI;
            GameManager.Instance.OnPlayerDied += OnPlayerDied;
            GameManager.Instance.OnPlayerRespawned += OnPlayerRespawned;
        }

        // Initial update
        UpdateUI();
    }

    void Update()
    {
        // Smooth fill animation
        if (smoothFill && healthFill != null)
        {
            currentFill = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * fillSmoothSpeed);
            healthFill.fillAmount = currentFill;
        }

        // Low health pulse
        if (pulseOnLowHealth && isLowHealth && healthFill != null)
        {
            float pulse = Mathf.Sin(Time.time * lowHealthPulseSpeed) * 0.15f + 0.85f;
            healthFill.color = Color.Lerp(criticalColor, flashColor, pulse);
        }
    }

    void UpdateUI()
    {
        if (GameManager.Instance == null) return;

        int currentHealth = GameManager.Instance.GetHealth();
        int maxHealth = GameManager.Instance.GetMaxHealth();
        float healthPercent = (float)currentHealth / maxHealth;

        // Update target fill
        targetFill = healthPercent;
        
        if (!smoothFill)
        {
            currentFill = healthPercent;
            if (healthFill != null)
                healthFill.fillAmount = healthPercent;
        }

        // Update color
        UpdateHealthColor(healthPercent);

        // Update text
        if (healthText != null && showText)
        {
            healthText.text = string.Format(textFormat, currentHealth, maxHealth);
        }
    }

    void UpdateHealthColor(float healthPercent)
    {
        if (healthFill == null) return;

        if (healthPercent > 0.6f)
        {
            healthFill.color = healthyColor;
            isLowHealth = false;
        }
        else if (healthPercent > 0.3f)
        {
            healthFill.color = damagedColor;
            isLowHealth = false;
        }
        else
        {
            healthFill.color = criticalColor;
            isLowHealth = true;
        }

        currentColor = healthFill.color;
    }

    // ===== EVENT HANDLERS =====

    void OnPlayerDied()
    {
        // Flash red on death
        if (healthFill != null)
        {
            healthFill.color = Color.red;
            isLowHealth = false;
        }
    }

    void OnPlayerRespawned()
    {
        // Reset on respawn
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);
        
        UpdateUI();
    }

    // ===== PUBLIC METHODS =====

    /// <summary>
    /// Flash the health bar white when taking damage
    /// </summary>
    public void DamageFlash()
    {
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashDamage());
    }

    IEnumerator FlashDamage()
    {
        if (healthFill == null) yield break;

        Color originalColor = healthFill.color;
        healthFill.color = flashColor;
        
        yield return new WaitForSeconds(damageFlashDuration);
        
        // Return to proper color based on health
        UpdateUI();
        flashCoroutine = null;
    }

    /// <summary>
    /// Manually update the health bar
    /// </summary>
    public void RefreshUI()
    {
        UpdateUI();
    }

    /// <summary>
    /// Set the health bar fill image
    /// </summary>
    public void SetHealthFill(Image newFill)
    {
        healthFill = newFill;
        UpdateUI();
    }

    /// <summary>
    /// Set the health text
    /// </summary>
    public void SetHealthText(Text newText)
    {
        healthText = newText;
        UpdateUI();
    }

    /// <summary>
    /// Get current health percentage (0-1)
    /// </summary>
    public float GetHealthPercent()
    {
        return targetFill;
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnHealthChanged -= UpdateUI;
            GameManager.Instance.OnPlayerDied -= OnPlayerDied;
            GameManager.Instance.OnPlayerRespawned -= OnPlayerRespawned;
        }
    }
}