using UnityEngine;
using System.Collections.Generic;

public class Preloader : MonoBehaviour
{
    [Header("Prefabs to Preload")]
    [SerializeField] private GameObject[] prefabsToPreload;

    [Header("Pool Settings")]
    [SerializeField] private bool usePooling = true;
    [SerializeField] private int defaultPoolSize = 10;

    [Header("Particle Pool")]
    [SerializeField] private ParticlePool particlePool;

    // Storage for preloaded objects
    private Dictionary<string, Queue<GameObject>> objectPools = new Dictionary<string, Queue<GameObject>>();
    private Dictionary<string, GameObject> prefabLookup = new Dictionary<string, GameObject>();
    private List<GameObject> allPreloadedObjects = new List<GameObject>();

    // Singleton
    public static Preloader Instance { get; private set; }

    void Awake()
    {
        // Singleton setup
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

        // Preload each prefab
        PreloadAllPrefabs();
    }

    void PreloadAllPrefabs()
    {
        if (prefabsToPreload == null || prefabsToPreload.Length == 0)
        {
            Debug.LogWarning("[Preloader] No prefabs to preload!");
            return;
        }

        int totalPreloaded = 0;

        foreach (GameObject prefab in prefabsToPreload)
        {
            if (prefab == null) continue;

            // Store prefab reference
            string prefabName = prefab.name;
            prefabLookup[prefabName] = prefab;

            if (usePooling)
            {
                // Create pool for this prefab
                CreatePool(prefab, defaultPoolSize);
                totalPreloaded += defaultPoolSize;
            }
            else
            {
                // Simple preload: instantiate once and store
                GameObject instance = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                instance.SetActive(false);
                DontDestroyOnLoad(instance);
                allPreloadedObjects.Add(instance);
                totalPreloaded++;
            }
        }

        Debug.Log($"[Preloader] Preloaded {totalPreloaded} objects from {prefabsToPreload.Length} prefabs.");
    }

    void CreatePool(GameObject prefab, int poolSize)
    {
        string prefabName = prefab.name;
        Queue<GameObject> pool = new Queue<GameObject>();

        for (int i = 0; i < poolSize; i++)
        {
            GameObject instance = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            instance.SetActive(false);
            DontDestroyOnLoad(instance);
            pool.Enqueue(instance);
            allPreloadedObjects.Add(instance);
        }

        objectPools[prefabName] = pool;
    }

    // ===== PUBLIC METHODS =====

    /// <summary>
    /// Get a preloaded object from the pool
    /// </summary>
    public GameObject GetObject(string prefabName)
    {
        if (!usePooling)
        {
            // Fallback: instantiate new
            if (prefabLookup.ContainsKey(prefabName))
            {
                return Instantiate(prefabLookup[prefabName]);
            }
            Debug.LogError($"[Preloader] Prefab '{prefabName}' not found!");
            return null;
        }

        if (!objectPools.ContainsKey(prefabName))
        {
            Debug.LogError($"[Preloader] Pool for '{prefabName}' doesn't exist!");
            return null;
        }

        Queue<GameObject> pool = objectPools[prefabName];

        // Get from pool or create new
        GameObject obj;
        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
        }
        else
        {
            // Pool empty, create new
            if (prefabLookup.ContainsKey(prefabName))
            {
                obj = Instantiate(prefabLookup[prefabName]);
                allPreloadedObjects.Add(obj);
                Debug.LogWarning($"[Preloader] Pool for '{prefabName}' was empty! Creating new instance.");
            }
            else
            {
                Debug.LogError($"[Preloader] Prefab '{prefabName}' not found!");
                return null;
            }
        }

        obj.SetActive(true);
        return obj;
    }

    /// <summary>
    /// Return an object to the pool
    /// </summary>
    public void ReturnObject(GameObject obj)
    {
        if (obj == null) return;

        string objName = obj.name.Replace("(Clone)", "").Trim();

        if (!usePooling || !objectPools.ContainsKey(objName))
        {
            // If not using pooling, just destroy
            Destroy(obj);
            return;
        }

        // Reset object
        obj.SetActive(false);
        obj.transform.position = Vector3.zero;
        obj.transform.rotation = Quaternion.identity;

        // Return to pool
        objectPools[objName].Enqueue(obj);
    }

    /// <summary>
    /// Check if a prefab is preloaded
    /// </summary>
    public bool IsPreloaded(string prefabName)
    {
        return prefabLookup.ContainsKey(prefabName);
    }

    /// <summary>
    /// Get the pool size for a prefab
    /// </summary>
    public int GetPoolSize(string prefabName)
    {
        if (objectPools.ContainsKey(prefabName))
            return objectPools[prefabName].Count;
        return 0;
    }

    /// <summary>
    /// Expand a pool by adding more instances
    /// </summary>
    public void ExpandPool(string prefabName, int additionalCount)
    {
        if (!usePooling) return;
        if (!prefabLookup.ContainsKey(prefabName))
        {
            Debug.LogError($"[Preloader] Prefab '{prefabName}' not found!");
            return;
        }

        if (!objectPools.ContainsKey(prefabName))
        {
            objectPools[prefabName] = new Queue<GameObject>();
        }

        for (int i = 0; i < additionalCount; i++)
        {
            GameObject instance = Instantiate(prefabLookup[prefabName], Vector3.zero, Quaternion.identity);
            instance.SetActive(false);
            DontDestroyOnLoad(instance);
            objectPools[prefabName].Enqueue(instance);
            allPreloadedObjects.Add(instance);
        }

        Debug.Log($"[Preloader] Expanded pool '{prefabName}' by {additionalCount}.");
    }

    /// <summary>
    /// Clear all pools and preloaded objects
    /// </summary>
    public void ClearAllPools()
    {
        foreach (GameObject obj in allPreloadedObjects)
        {
            if (obj != null)
                Destroy(obj);
        }

        objectPools.Clear();
        prefabLookup.Clear();
        allPreloadedObjects.Clear();

        Debug.Log("[Preloader] All pools cleared!");
    }

    /// <summary>
    /// Get total count of preloaded objects
    /// </summary>
    public int GetTotalPreloadedCount()
    {
        return allPreloadedObjects.Count;
    }

    // ===== PARTICLE POOL INTEGRATION =====

    /// <summary>
    /// Get particle from pool
    /// </summary>
    public GameObject GetParticle(string particleName)
    {
        if (particlePool != null)
        {
            return particlePool.GetParticle(particleName);
        }
        return GetObject(particleName);
    }

    /// <summary>
    /// Return particle to pool
    /// </summary>
    public void ReturnParticle(GameObject particle)
    {
        if (particlePool != null)
        {
            particlePool.ReturnParticle(particle);
        }
        else
        {
            ReturnObject(particle);
        }
    }

    // ===== DEBUG =====
    void OnDestroy()
    {
        ClearAllPools();
    }
}