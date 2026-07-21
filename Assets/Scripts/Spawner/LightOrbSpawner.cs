using UnityEngine;
using System.Collections.Generic;

public class LightOrbSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject lightOrbPrefab;
    [SerializeField] private int maxOrbs = 6;
    [SerializeField] private float spawnInterval = 4f;
    [SerializeField] private float spawnRadius = 10f;
    [SerializeField] private float minSpawnDistance = 3f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float checkRadius = 0.5f;
    [SerializeField] private float minOrbSpacing = 3f;

    [Header("Spawn Area Limits")]
    [SerializeField] private bool useScreenBounds = true;
    [SerializeField] private float screenPadding = 1f;

    [Header("Pool Settings")]
    [SerializeField] private bool usePooling = true;
    [SerializeField] private int poolSize = 10;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private bool logSpawnEvents = false;

    private List<GameObject> activeOrbs = new List<GameObject>();
    private Queue<GameObject> orbPool = new Queue<GameObject>();
    private float spawnTimer;
    private Transform player;
    private Camera mainCamera;
    private Bounds levelBounds;

    void Start()
    {
        // Setup layers
        if (obstacleLayer == 0)
            obstacleLayer = LayerMask.GetMask("Ground", "Wall", "Platform");

        // Find references
        FindPlayer();
        FindCamera();

        if (player == null)
            Invoke(nameof(FindPlayer), 1f);

        if (mainCamera == null)
            Invoke(nameof(FindCamera), 1f);

        // Setup pool
        if (usePooling)
            InitializePool();

        // Initial spawn
        spawnTimer = spawnInterval * 0.5f;
        SpawnInitialOrbs();
    }

    void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            if (logSpawnEvents)
                Debug.Log("[LightOrbSpawner] Found player: " + player.name);
        }
        else
        {
            Debug.LogWarning("[LightOrbSpawner] Player not found! Will retry.");
            Invoke(nameof(FindPlayer), 1f);
        }
    }

    void FindCamera()
    {
        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            // Calculate level bounds from camera
            Vector2 min = mainCamera.ViewportToWorldPoint(Vector2.zero);
            Vector2 max = mainCamera.ViewportToWorldPoint(Vector2.one);
            levelBounds = new Bounds((min + max) / 2, (max - min) + Vector2.one * 2f);
        }
        else
        {
            Debug.LogWarning("[LightOrbSpawner] Main Camera not found!");
        }
    }

    void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject orb = Instantiate(lightOrbPrefab);
            orb.SetActive(false);
            orbPool.Enqueue(orb);
        }

        if (logSpawnEvents)
            Debug.Log($"[LightOrbSpawner] Pool initialized with {poolSize} orbs");
    }

    void SpawnInitialOrbs()
    {
        int initialOrbs = Random.Range(Mathf.Min(2, maxOrbs), Mathf.Min(4, maxOrbs + 1));
        for (int i = 0; i < initialOrbs; i++)
        {
            SpawnOrb();
        }

        if (logSpawnEvents)
            Debug.Log($"[LightOrbSpawner] Initial orbs spawned: {initialOrbs}");
    }

    void Update()
    {
        if (player == null) return;

        // Clean up destroyed orbs
        activeOrbs.RemoveAll(o => o == null);

        // Handle respawning
        if (activeOrbs.Count < maxOrbs)
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0)
            {
                SpawnOrb();
                spawnTimer = spawnInterval;
            }
        }
    }

    void SpawnOrb()
    {
        if (lightOrbPrefab == null || player == null) return;

        Vector2 spawnPos = GetValidSpawnPosition();
        if (spawnPos == Vector2.zero)
        {
            // Fallback: spawn near player but away from walls
            for (int i = 0; i < 10; i++)
            {
                Vector2 fallbackPos = (Vector2)player.position + Random.insideUnitCircle * minSpawnDistance;
                if (IsPositionValid(fallbackPos))
                {
                    spawnPos = fallbackPos;
                    break;
                }
            }

            // Last resort: spawn at player position (not ideal but better than nothing)
            if (spawnPos == Vector2.zero)
            {
                spawnPos = (Vector2)player.position + Vector2.up * minSpawnDistance;
                Debug.LogWarning("[LightOrbSpawner] Using last resort spawn position!");
            }
        }

        // Spawn orb from pool or instantiate
        GameObject orb = GetOrbFromPool();
        if (orb != null)
        {
            orb.transform.position = spawnPos;
            orb.SetActive(true);
            activeOrbs.Add(orb);

            if (logSpawnEvents)
                Debug.Log($"[LightOrbSpawner] Orb spawned at: {spawnPos}");
        }
    }

    GameObject GetOrbFromPool()
    {
        if (usePooling && orbPool.Count > 0)
        {
            GameObject orb = orbPool.Dequeue();
            if (orb != null)
                return orb;
        }

        // Instantiate new one if pool is empty or not using pooling
        return Instantiate(lightOrbPrefab);
    }

    public void ReturnOrbToPool(GameObject orb)
    {
        if (orb == null) return;

        activeOrbs.Remove(orb);
        orb.SetActive(false);

        if (usePooling && orbPool.Count < poolSize)
        {
            orbPool.Enqueue(orb);
        }
        else
        {
            Destroy(orb);
        }

        if (logSpawnEvents)
            Debug.Log("[LightOrbSpawner] Orb returned to pool");
    }

    Vector2 GetValidSpawnPosition()
    {
        if (player == null) return Vector2.zero;

        for (int i = 0; i < 30; i++)
        {
            // Random direction and distance
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            float distance = Random.Range(minSpawnDistance, spawnRadius);
            Vector2 pos = (Vector2)player.position + randomDir * distance;

            // Check if within screen bounds
            if (useScreenBounds && mainCamera != null && !IsOnScreen(pos))
                continue;

            // Check if too close to existing orbs
            if (IsTooCloseToOrbs(pos))
                continue;

            // Check if position is valid (not in wall/obstacle)
            if (IsPositionValid(pos))
                return pos;
        }

        return Vector2.zero;
    }

    bool IsPositionValid(Vector2 pos)
    {
        // Check for obstacles (walls, platforms)
        Collider2D hit = Physics2D.OverlapCircle(pos, checkRadius, obstacleLayer);
        if (hit != null) return false;

        // Check if there's ground below (for ground-based orbs)
        // Reduced vertical check to allow spawning on platforms
        Vector2 belowPos = pos + Vector2.down * 0.3f;
        Collider2D belowHit = Physics2D.OverlapCircle(belowPos, checkRadius * 0.7f, obstacleLayer);
        
        // If no ground below, check if we're on a platform
        if (belowHit == null)
        {
            // Try a slightly different position
            Vector2 belowPos2 = pos + Vector2.down * 0.1f;
            Collider2D belowHit2 = Physics2D.OverlapCircle(belowPos2, checkRadius * 0.5f, obstacleLayer);
            if (belowHit2 == null)
                return false;
        }

        return true;
    }

    bool IsTooCloseToOrbs(Vector2 pos)
    {
        foreach (GameObject orb in activeOrbs)
        {
            if (orb != null && Vector2.Distance(pos, orb.transform.position) < minOrbSpacing)
                return true;
        }
        return false;
    }

    bool IsOnScreen(Vector2 worldPos)
    {
        if (mainCamera == null) return true;

        Vector3 viewportPos = mainCamera.WorldToViewportPoint(worldPos);
        return viewportPos.x > -screenPadding && viewportPos.x < 1 + screenPadding &&
               viewportPos.y > -screenPadding && viewportPos.y < 1 + screenPadding;
    }

    // ===== PUBLIC METHODS =====
    public void DespawnAllOrbs()
    {
        foreach (GameObject orb in activeOrbs)
        {
            if (orb != null)
            {
                orb.SetActive(false);
                if (usePooling)
                    orbPool.Enqueue(orb);
                else
                    Destroy(orb);
            }
        }
        activeOrbs.Clear();
    }

    public void ForceSpawnOrb()
    {
        SpawnOrb();
    }

    public int GetActiveOrbCount()
    {
        activeOrbs.RemoveAll(o => o == null);
        return activeOrbs.Count;
    }

    // ===== DEBUG VISUALIZATION =====
    void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        // Player detection range
        if (player != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(player.position, spawnRadius);
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(player.position, minSpawnDistance);
        }

        // Screen bounds
        if (useScreenBounds && mainCamera != null)
        {
            Gizmos.color = Color.cyan;
            Vector2 min = mainCamera.ViewportToWorldPoint(Vector2.zero);
            Vector2 max = mainCamera.ViewportToWorldPoint(Vector2.one);
            
            Vector2 paddedMin = min - Vector2.one * screenPadding;
            Vector2 paddedMax = max + Vector2.one * screenPadding;
            
            Gizmos.DrawWireCube((paddedMin + paddedMax) / 2, paddedMax - paddedMin);
        }

        // Active orbs
        Gizmos.color = Color.magenta;
        foreach (GameObject orb in activeOrbs)
        {
            if (orb != null)
                Gizmos.DrawWireSphere(orb.transform.position, 0.3f);
        }
    }
}