using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Enemy spawner that validates positions and works with your hierarchy
/// FULLY INTEGRATED with YOUR GameManager
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefabs")]
    [SerializeField] private GameObject normalEnemyPrefab;
    [SerializeField] private GameObject flyingEnemyPrefab;
    [SerializeField] private GameObject teleportingEnemyPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private int maxEnemies = 10;
    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private float spawnRadius = 8f;
    [SerializeField] private LayerMask obstacleLayer = -1;

    [Header("Wave Settings")]
    [SerializeField] private int enemiesPerWave = 3;
    [SerializeField] private float waveDelay = 5f;

    [Header("Difficulty Scaling")]
    [SerializeField] private float healthMultiplierPerLevel = 0.2f;  // +20% health per upgrade level
    [SerializeField] private float speedMultiplierPerLevel = 0.1f;   // +10% speed per upgrade level

    private List<GameObject> activeEnemies = new List<GameObject>();
    private Transform player;
    private float nextSpawnTime = 0f;
    private int currentWave = 0;
    private bool isWaveSpawning = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null)
            Debug.LogError("[EnemySpawner] No player found!");

        // Spawn initial enemies
        for (int i = 0; i < 3; i++)
        {
            SpawnEnemy();
        }
        nextSpawnTime = Time.time + spawnInterval;
    }

    void Update()
    {
        // Clean up destroyed enemies
        activeEnemies.RemoveAll(e => e == null);

        // Check if we can spawn more
        if (!isWaveSpawning && activeEnemies.Count < maxEnemies && Time.time >= nextSpawnTime)
        {
            SpawnEnemy();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    void SpawnEnemy()
    {
        // Pick random enemy type
        GameObject prefab = GetRandomEnemyPrefab();
        if (prefab == null) return;

        // Find valid spawn position
        Vector2 spawnPos = GetValidSpawnPosition();
        if (spawnPos == Vector2.zero)
        {
            Debug.LogWarning("[EnemySpawner] No valid spawn position found!");
            return;
        }

        // Spawn the enemy
        GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
        activeEnemies.Add(enemy);

        // Apply difficulty scaling using YOUR GameManager
        ApplyDifficultyScaling(enemy);
    }

    GameObject GetRandomEnemyPrefab()
    {
        float roll = Random.value;
        
        // Get difficulty from YOUR GameManager's upgrade levels
        int totalUpgrades = 0;
        if (GameManager.Instance != null)
        {
            totalUpgrades = GameManager.Instance.speedLevel + 
                           GameManager.Instance.jumpLevel + 
                           GameManager.Instance.fireRateLevel;
        }

        // Enemy type distribution based on difficulty
        // Higher difficulty = more flying and teleporting enemies
        float normalChance = 0.5f - (totalUpgrades * 0.03f);
        float flyingChance = 0.3f + (totalUpgrades * 0.02f);
        float teleportChance = 0.2f + (totalUpgrades * 0.03f);

        // Clamp to valid ranges
        normalChance = Mathf.Clamp(normalChance, 0.2f, 0.7f);
        flyingChance = Mathf.Clamp(flyingChance, 0.1f, 0.5f);
        teleportChance = Mathf.Clamp(teleportChance, 0.1f, 0.4f);

        // Normalize so they add up to 1
        float total = normalChance + flyingChance + teleportChance;
        normalChance /= total;
        flyingChance /= total;
        teleportChance /= total;

        if (roll < normalChance) return normalEnemyPrefab;
        if (roll < normalChance + flyingChance) return flyingEnemyPrefab;
        return teleportingEnemyPrefab;
    }

    Vector2 GetValidSpawnPosition()
    {
        if (player == null) return Vector2.zero;

        for (int i = 0; i < 20; i++)
        {
            Vector2 randomPos = (Vector2)player.position + Random.insideUnitCircle * spawnRadius;
            
            // Check if position is valid (not in wall)
            Collider2D hit = Physics2D.OverlapCircle(randomPos, 0.6f, obstacleLayer);
            if (hit == null)
            {
                // Check it's not too close to other enemies
                bool tooClose = false;
                foreach (GameObject enemy in activeEnemies)
                {
                    if (enemy != null && Vector2.Distance(randomPos, enemy.transform.position) < 1.5f)
                    {
                        tooClose = true;
                        break;
                    }
                }
                
                // Check it's not too close to player
                if (!tooClose && Vector2.Distance(randomPos, player.position) > 2f)
                {
                    return randomPos;
                }
            }
        }

        // Fallback: try positions further away
        for (int i = 0; i < 10; i++)
        {
            Vector2 randomPos = (Vector2)player.position + Random.insideUnitCircle * (spawnRadius * 1.5f);
            Collider2D hit = Physics2D.OverlapCircle(randomPos, 0.6f, obstacleLayer);
            if (hit == null)
            {
                return randomPos;
            }
        }

        return Vector2.zero;
    }

    void ApplyDifficultyScaling(GameObject enemy)
    {
        if (GameManager.Instance == null) return;

        Enemy enemyScript = enemy.GetComponent<Enemy>();
        if (enemyScript == null) return;

        // Get total upgrade levels from YOUR GameManager
        int totalUpgrades = GameManager.Instance.speedLevel + 
                           GameManager.Instance.jumpLevel + 
                           GameManager.Instance.fireRateLevel;

        // Calculate multipliers
        float healthMult = 1f + (totalUpgrades * healthMultiplierPerLevel);
        float speedMult = 1f + (totalUpgrades * speedMultiplierPerLevel);

        // Apply to enemy (you'll need to add this method to Enemy.cs)
        enemyScript.ApplyDifficultyScaling(healthMult, speedMult, totalUpgrades);
    }

    public void StartWave()
    {
        if (!isWaveSpawning)
            StartCoroutine(SpawnWave());
    }

    IEnumerator SpawnWave()
    {
        isWaveSpawning = true;
        currentWave++;
        int enemiesToSpawn = enemiesPerWave + (currentWave / 2);

        // Scale wave size with YOUR GameManager difficulty
        if (GameManager.Instance != null)
        {
            int totalUpgrades = GameManager.Instance.speedLevel + 
                               GameManager.Instance.jumpLevel + 
                               GameManager.Instance.fireRateLevel;
            enemiesToSpawn += totalUpgrades / 2;
        }

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            if (activeEnemies.Count < maxEnemies)
                SpawnEnemy();
            
            yield return new WaitForSeconds(0.5f);
        }

        isWaveSpawning = false;
    }

    public int GetActiveEnemyCount()
    {
        activeEnemies.RemoveAll(e => e == null);
        return activeEnemies.Count;
    }

    public void ClearAllEnemies()
    {
        foreach (GameObject enemy in activeEnemies)
        {
            if (enemy != null)
                Destroy(enemy);
        }
        activeEnemies.Clear();
    }

    void OnDrawGizmosSelected()
    {
        if (player == null) return;
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(player.position, spawnRadius);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(player.position, 2f); // No-spawn zone around player
    }
}