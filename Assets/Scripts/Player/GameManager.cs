using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    // ===== SINGLETON =====
    public static GameManager Instance;

    // ===== GAME STATE =====
    public enum GameState { Playing, Paused, GameOver }
    public GameState currentState = GameState.Playing;

    // ===== PLAYER =====
    private GameObject currentPlayer;
    private PlayerController playerController;

    // ===== RESPAWN =====
    [Header("Respawn")]
    public Transform respawnPoint;
    public float respawnDelay = 1.5f;
    private bool isRespawning = false;

    // ===== HEALTH =====
    [Header("Health")]
    public int maxHealth = 3;
    private int currentHealth;

    // ===== SCORE =====
    [Header("Score")]
    private int score = 0;
    public int Score => score;

    // ===== UPGRADES =====
    [Header("Upgrades")]
    public int speedLevel = 0;
    public int jumpLevel = 0;
    public int fireRateLevel = 0;
    public int[] speedCosts = { 10, 20, 30 };
    public int[] jumpCosts = { 10, 20, 30 };
    public int[] fireRateCosts = { 15, 25, 40 };

    // ===== BASE STATS (for reset) =====
    private float baseMoveSpeed = 5f;
    private float baseJumpForce = 12f;
    private float baseShootCooldown = 0.5f;

    // ===== DIFFICULTY =====
    [Header("Difficulty")]
    [Range(1, 3)]
    public int difficultyLevel = 1;
    public float enemyHealthMultiplier = 1f;
    public float enemySpeedMultiplier = 1f;
    public int enemySpawnMultiplier = 1;

    // ===== EVENTS =====
    public Action OnHealthChanged;
    public Action OnScoreChanged;
    public Action OnPlayerDied;
    public Action OnPlayerRespawned;
    public Action OnUpgradeChanged;
    public Action OnDifficultyChanged;

    // ===== SAVE =====
    private string saveKey = "LurelightSaveData";

    // ===== TIMER REFERENCE =====
    private SurvivalTimer survivalTimer;

    // ===== ENEMY SPAWNER REFERENCE =====
    private EnemySpawner enemySpawner;

    // ===== SCENE LOAD TRACKING =====
    private bool isFirstSceneLoad = true;

    // ===== AWAKE =====
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        LoadGame();
    }

    // ===== START =====
    void Start()
    {
        FindPlayer();
        FindReferences();
        
        currentHealth = maxHealth;
        Debug.Log("GameManager Start - Health: " + currentHealth + "/" + maxHealth);
        OnHealthChanged?.Invoke();

        // Apply saved upgrades to player
        ApplyUpgradesToPlayer();
        
        // Apply difficulty
        ApplyDifficultySettings();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindPlayer();
        FindReferences();
        ApplyUpgradesToPlayer();
        
        if (!isRespawning)
        {
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke();
        }
    }

    // ===== UPDATE =====
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            ResetGame();

        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
            
        if (Input.GetKeyDown(KeyCode.Alpha1))
            ChangeDifficulty(1);
        if (Input.GetKeyDown(KeyCode.Alpha2))
            ChangeDifficulty(2);
        if (Input.GetKeyDown(KeyCode.Alpha3))
            ChangeDifficulty(3);
    }

    // ===== FIND REFERENCES =====
    void FindPlayer()
    {
        currentPlayer = GameObject.FindGameObjectWithTag("Player");
        if (currentPlayer != null)
        {
            playerController = currentPlayer.GetComponent<PlayerController>();
            Debug.Log("GameManager found player: " + currentPlayer.name);
        }
        else
        {
            Debug.LogWarning("Player not found! Will retry.");
            Invoke(nameof(FindPlayer), 0.5f);
        }
    }

    void FindReferences()
    {
        survivalTimer = FindObjectOfType<SurvivalTimer>();
        enemySpawner = FindObjectOfType<EnemySpawner>();
    }

    // ===== HEALTH =====
    public int GetHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;

    public void TakeDamage(int damage)
    {
        if (currentState == GameState.GameOver || isRespawning) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);

        OnHealthChanged?.Invoke();

        if (currentHealth <= 0)
            PlayerDies();
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        OnHealthChanged?.Invoke();
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke();
    }

    // ===== SCORE - FIXED WITH DEBUG =====
    public void AddScore(int amount)
    {
        score += amount;
        Debug.Log($"✅ Score: {score} (added {amount}) - OnScoreChanged event firing!");
        OnScoreChanged?.Invoke();
    }

    public void RemoveScore(int amount)
    {
        score -= amount;
        if (score < 0) score = 0;
        Debug.Log($"❌ Score: {score} (removed {amount})");
        OnScoreChanged?.Invoke();
    }

    public bool HasEnoughScore(int amount) => score >= amount;

    // ===== UPGRADES =====
    public bool UpgradeSpeed()
    {
        if (speedLevel >= 3) return false;
        int cost = speedCosts[speedLevel];
        if (!HasEnoughScore(cost)) return false;

        RemoveScore(cost);
        speedLevel++;
        ApplyUpgradesToPlayer();
        OnUpgradeChanged?.Invoke();
        SaveGame();
        return true;
    }

    public bool UpgradeJump()
    {
        if (jumpLevel >= 3) return false;
        int cost = jumpCosts[jumpLevel];
        if (!HasEnoughScore(cost)) return false;

        RemoveScore(cost);
        jumpLevel++;
        ApplyUpgradesToPlayer();
        OnUpgradeChanged?.Invoke();
        SaveGame();
        return true;
    }

    public bool UpgradeFireRate()
    {
        if (fireRateLevel >= 3) return false;
        int cost = fireRateCosts[fireRateLevel];
        if (!HasEnoughScore(cost)) return false;

        RemoveScore(cost);
        fireRateLevel++;
        ApplyUpgradesToPlayer();
        OnUpgradeChanged?.Invoke();
        SaveGame();
        return true;
    }

    void ApplyUpgradesToPlayer()
    {
        if (playerController == null) return;

        playerController.moveSpeed = baseMoveSpeed;
        playerController.jumpForce = baseJumpForce;
        playerController.shootCooldown = baseShootCooldown;

        playerController.moveSpeed += speedLevel * 0.5f;
        playerController.jumpForce += jumpLevel * 1.5f;

        playerController.shootCooldown -= fireRateLevel * 0.05f;
        if (playerController.shootCooldown < 0.1f)
            playerController.shootCooldown = 0.1f;

        Debug.Log($"Upgrades applied: Speed={playerController.moveSpeed}, Jump={playerController.jumpForce}, FireRate={playerController.shootCooldown}");
    }

    public void ResetUpgrades()
    {
        speedLevel = 0;
        jumpLevel = 0;
        fireRateLevel = 0;

        ApplyUpgradesToPlayer();
        OnUpgradeChanged?.Invoke();
        Debug.Log("Upgrades reset to base values!");
    }

    public int GetUpgradeLevel(string type)
    {
        switch (type.ToLower())
        {
            case "speed": return speedLevel;
            case "jump": return jumpLevel;
            case "firerate": return fireRateLevel;
            default: return 0;
        }
    }

    public int GetUpgradeCost(string type)
    {
        switch (type.ToLower())
        {
            case "speed": return speedLevel < 3 ? speedCosts[speedLevel] : 999;
            case "jump": return jumpLevel < 3 ? jumpCosts[jumpLevel] : 999;
            case "firerate": return fireRateLevel < 3 ? fireRateCosts[fireRateLevel] : 999;
            default: return 999;
        }
    }

    // ===== DIFFICULTY =====
    public void ChangeDifficulty(int level)
    {
        difficultyLevel = Mathf.Clamp(level, 1, 3);
        ApplyDifficultySettings();
        OnDifficultyChanged?.Invoke();
        Debug.Log($"Difficulty changed to {difficultyLevel}");
    }

    void ApplyDifficultySettings()
    {
        switch (difficultyLevel)
        {
            case 1:
                enemyHealthMultiplier = 0.8f;
                enemySpeedMultiplier = 0.8f;
                enemySpawnMultiplier = 1;
                break;
            case 2:
                enemyHealthMultiplier = 1f;
                enemySpeedMultiplier = 1f;
                enemySpawnMultiplier = 1;
                break;
            case 3:
                enemyHealthMultiplier = 1.5f;
                enemySpeedMultiplier = 1.3f;
                enemySpawnMultiplier = 2;
                break;
        }

        Enemy[] enemies = FindObjectsOfType<Enemy>();
        foreach (Enemy enemy in enemies) { }

        Debug.Log($"Difficulty settings applied: Health={enemyHealthMultiplier}, Speed={enemySpeedMultiplier}, Spawn={enemySpawnMultiplier}");
    }

    // ===== PLAYER DEATH & RESPAWN =====
    void PlayerDies()
    {
        if (isRespawning) return;
        
        Debug.Log("Player died!");
        currentState = GameState.GameOver;
        isRespawning = true;

        if (playerController != null)
            playerController.Die();

        OnPlayerDied?.Invoke();
        SaveGame();

        if (survivalTimer != null)
            survivalTimer.StopTimer();

        Invoke(nameof(RespawnPlayer), respawnDelay);
    }

    void RespawnPlayer()
    {
        if (currentPlayer == null)
            FindPlayer();

        if (currentPlayer != null)
        {
            currentPlayer.SetActive(true);

            if (respawnPoint != null)
                currentPlayer.transform.position = respawnPoint.position;
            else
                currentPlayer.transform.position = Vector3.zero;

            if (playerController != null)
                playerController.ResetPlayer();

            ResetHealth();

            if (survivalTimer != null)
                survivalTimer.ResetTimer();

            CameraFollow cam = Camera.main.GetComponent<CameraFollow>();
            if (cam != null)
                cam.target = currentPlayer.transform;

            currentState = GameState.Playing;
            isRespawning = false;
            
            OnPlayerRespawned?.Invoke();
            Debug.Log("Player respawned!");
        }
        else
        {
            Debug.LogError("Player reference lost! Can't respawn.");
            isRespawning = false;
        }
    }

    // ===== PAUSE =====
    public void TogglePause()
    {
        if (currentState == GameState.Paused)
            ResumeGame();
        else if (currentState == GameState.Playing)
            PauseGame();
    }

    public void PauseGame()
    {
        currentState = GameState.Paused;
        Time.timeScale = 0f;

        if (survivalTimer != null)
            survivalTimer.PauseTimer();

        Debug.Log("Game Paused");
    }

    public void ResumeGame()
    {
        currentState = GameState.Playing;
        Time.timeScale = 1f;

        if (survivalTimer != null)
            survivalTimer.ResumeTimer();

        Debug.Log("Game Resumed");
    }

    // ===== RESET =====
    public void ResetGame()
    {
        Debug.Log("ResetGame called!");
        
        Time.timeScale = 1f;
        currentState = GameState.Playing;
        isRespawning = false;

        ResetHealth();
        ResetUpgrades();

        score = 0;
        OnScoreChanged?.Invoke();

        if (survivalTimer != null)
            survivalTimer.ResetTimer();

        if (enemySpawner != null)
            enemySpawner.ClearAllEnemies();

        PlayerPrefs.DeleteKey(saveKey);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ===== SAVE / LOAD =====
    public void SaveGame()
    {
        SaveData data = new SaveData
        {
            score = score,
            speedLevel = speedLevel,
            jumpLevel = jumpLevel,
            fireRateLevel = fireRateLevel,
            maxHealth = maxHealth,
            difficultyLevel = difficultyLevel
        };

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(saveKey, json);
        PlayerPrefs.Save();
        Debug.Log("Game saved!");
    }

    public void LoadGame()
    {
        if (PlayerPrefs.HasKey(saveKey))
        {
            string json = PlayerPrefs.GetString(saveKey);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            score = data.score;
            speedLevel = data.speedLevel;
            jumpLevel = data.jumpLevel;
            fireRateLevel = data.fireRateLevel;
            maxHealth = data.maxHealth;
            difficultyLevel = data.difficultyLevel;

            Debug.Log("Game loaded! Score: " + score + ", Difficulty: " + difficultyLevel);
        }
        else
        {
            Debug.Log("No save data found.");
        }
    }

    // ===== UTILITY =====
    public void KillAllEnemies()
    {
        if (enemySpawner != null)
            enemySpawner.ClearAllEnemies();
        else
        {
            Enemy[] enemies = FindObjectsOfType<Enemy>();
            foreach (Enemy enemy in enemies)
            {
                Destroy(enemy.gameObject);
            }
        }
    }

    // ===== SAVE DATA CLASS =====
    [Serializable]
    public class SaveData
    {
        public int score;
        public int speedLevel;
        public int jumpLevel;
        public int fireRateLevel;
        public int maxHealth;
        public int difficultyLevel = 1;
    }
}