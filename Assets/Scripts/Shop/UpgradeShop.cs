using UnityEngine;
using UnityEngine.UI;
using System.IO;

/// <summary>
/// Fixed upgrade shop system for your hierarchy
/// Works with ShopPanel, ShopBox, SpeedText, JumpText, FireRateText, OrbText
/// </summary>
public class UpgradeShop : MonoBehaviour
{
    [Header("UI References - Match Your Hierarchy")]
    [SerializeField] private Text speedText;
    [SerializeField] private Text jumpText;
    [SerializeField] private Text fireRateText;
    [SerializeField] private Text orbText;
    [SerializeField] private Button speedButton;
    [SerializeField] private Button jumpButton;
    [SerializeField] private Button fireRateButton;

    [Header("Upgrade Costs")]
    [SerializeField] private int[] speedCosts = { 50, 100, 150 };
    [SerializeField] private int[] jumpCosts = { 50, 100, 150 };
    [SerializeField] private int[] fireRateCosts = { 50, 100, 150 };

    [Header("Save File")]
    private string savePath;

    [System.Serializable]
    public class UpgradeSaveData
    {
        public int speedLevel = 0;
        public int jumpLevel = 0;
        public int fireRateLevel = 0;
        public int orbsCollected = 0;
        public int totalScore = 0;
    }

    private UpgradeSaveData saveData = new UpgradeSaveData();
    private const int MAX_UPGRADE_LEVEL = 3;
    private bool isShopOpen = false;

    void Start()
    {
        // CLOSE SHOP ON START - FIXES UI OVERLAP ISSUE
        gameObject.SetActive(false);
        isShopOpen = false;

        savePath = Path.Combine(Application.persistentDataPath, "upgrades_lurelight.json");
        LoadUpgrades();
        UpdateUI();
        SetupButtons();
    }

    void Update()
    {
        // Toggle shop with Tab key
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleShop();
        }

        // Close shop with Escape key
        if (Input.GetKeyDown(KeyCode.Escape) && isShopOpen)
        {
            CloseShop();
        }
    }

    void ToggleShop()
    {
        if (isShopOpen)
        {
            CloseShop();
        }
        else
        {
            OpenShop();
        }
    }

    void OpenShop()
    {
        gameObject.SetActive(true);
        isShopOpen = true;
        UpdateUI(); // Refresh UI when opening
        Time.timeScale = 0f; // Pause game
        Debug.Log("Shop opened!");
    }

    void CloseShop()
    {
        gameObject.SetActive(false);
        isShopOpen = false;
        Time.timeScale = 1f; // Resume game
        Debug.Log("Shop closed!");
    }

    void LoadUpgrades()
    {
        if (File.Exists(savePath))
        {
            try
            {
                string json = File.ReadAllText(savePath);
                saveData = JsonUtility.FromJson<UpgradeSaveData>(json);
                
                // Clamp to max level (FIXES THE RESET ISSUE)
                saveData.speedLevel = Mathf.Clamp(saveData.speedLevel, 0, MAX_UPGRADE_LEVEL);
                saveData.jumpLevel = Mathf.Clamp(saveData.jumpLevel, 0, MAX_UPGRADE_LEVEL);
                saveData.fireRateLevel = Mathf.Clamp(saveData.fireRateLevel, 0, MAX_UPGRADE_LEVEL);
                
                Debug.Log($"Loaded upgrades: Speed {saveData.speedLevel}, Jump {saveData.jumpLevel}, FireRate {saveData.fireRateLevel}");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to load upgrades: {e.Message}. Creating new save.");
                saveData = new UpgradeSaveData();
            }
        }
        else
        {
            saveData = new UpgradeSaveData();
            SaveUpgrades(); // Create save file
        }
    }

    void SaveUpgrades()
    {
        try
        {
            string json = JsonUtility.ToJson(saveData);
            File.WriteAllText(savePath, json);
            Debug.Log("Upgrades saved successfully!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save upgrades: {e.Message}");
        }
    }

    public void ResetUpgrades()
    {
        saveData = new UpgradeSaveData();
        SaveUpgrades();
        UpdateUI();
        Debug.Log("Upgrades reset!");
    }

    void SetupButtons()
    {
        if (speedButton != null)
            speedButton.onClick.AddListener(() => PurchaseUpgrade("Speed"));
        
        if (jumpButton != null)
            jumpButton.onClick.AddListener(() => PurchaseUpgrade("Jump"));
        
        if (fireRateButton != null)
            fireRateButton.onClick.AddListener(() => PurchaseUpgrade("FireRate"));
    }

    void PurchaseUpgrade(string upgradeType)
    {
        int currentLevel = 0;
        int[] costs = null;

        switch (upgradeType)
        {
            case "Speed":
                currentLevel = saveData.speedLevel;
                costs = speedCosts;
                break;
            case "Jump":
                currentLevel = saveData.jumpLevel;
                costs = jumpCosts;
                break;
            case "FireRate":
                currentLevel = saveData.fireRateLevel;
                costs = fireRateCosts;
                break;
        }

        // Check if already max level
        if (currentLevel >= MAX_UPGRADE_LEVEL)
        {
            Debug.Log($"{upgradeType} already at max level!");
            return;
        }

        // Get cost for next level
        int cost = costs[currentLevel];
        
        // Check if player has enough orbs (using your orb system)
        if (!HasEnoughOrbs(cost))
        {
            Debug.Log($"Not enough orbs! Need {cost}");
            return;
        }

        // Apply upgrade
        switch (upgradeType)
        {
            case "Speed":
                saveData.speedLevel++;
                break;
            case "Jump":
                saveData.jumpLevel++;
                break;
            case "FireRate":
                saveData.fireRateLevel++;
                break;
        }

        // Deduct orbs
        SpendOrbs(cost);
        
        // Save immediately
        SaveUpgrades();
        UpdateUI();
        ApplyUpgradeEffects(upgradeType);
    }

    bool HasEnoughOrbs(int cost)
    {
        // Use your orb system - check if saveData.orbsCollected >= cost
        return saveData.orbsCollected >= cost;
    }

    void SpendOrbs(int cost)
    {
        saveData.orbsCollected -= cost;
        // Update your orb UI here
    }

    void ApplyUpgradeEffects(string upgradeType)
    {
        // Apply to player controller
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            switch (upgradeType)
            {
                case "Speed":
                    // player.moveSpeed += 0.5f * saveData.speedLevel;
                    break;
                case "Jump":
                    // player.jumpForce += 1f * saveData.jumpLevel;
                    break;
                case "FireRate":
                    // player.fireRate = Mathf.Max(0.1f, 0.5f - 0.1f * saveData.fireRateLevel);
                    break;
            }
        }
    }

    void UpdateUI()
    {
        // Update text displays
        if (speedText != null)
            speedText.text = $"Speed\nLevel {saveData.speedLevel}/{MAX_UPGRADE_LEVEL}";
        
        if (jumpText != null)
            jumpText.text = $"Jump\nLevel {saveData.jumpLevel}/{MAX_UPGRADE_LEVEL}";
        
        if (fireRateText != null)
            fireRateText.text = $"Fire Rate\nLevel {saveData.fireRateLevel}/{MAX_UPGRADE_LEVEL}";
        
        if (orbText != null)
            orbText.text = $"Orbs: {saveData.orbsCollected}";
    }

    // Call this from GameOver UI RestartButton
    public void OnRestartGame()
    {
        ResetUpgrades();
        // Load first level
        UnityEngine.SceneManagement.SceneManager.LoadScene("Level1");
    }

    // Call this when collecting orbs
    public void AddOrbs(int amount)
    {
        saveData.orbsCollected += amount;
        SaveUpgrades();
        UpdateUI();
    }

    // Public methods to open/close shop from buttons
    public void OpenShopFromButton()
    {
        OpenShop();
    }

    public void CloseShopFromButton()
    {
        CloseShop();
    }
}