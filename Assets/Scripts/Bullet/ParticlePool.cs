using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ParticlePool : MonoBehaviour
{
    public static ParticlePool Instance;

    [Header("Particle Prefabs")]
    public GameObject teleportParticlePrefab;
    public GameObject hitParticlePrefab;
    public GameObject impactEffectPrefab;

    [Header("Pool Settings")]
    public int poolSize = 5;

    private Queue<GameObject> teleportPool = new Queue<GameObject>();
    private Queue<GameObject> hitPool = new Queue<GameObject>();
    private Queue<GameObject> impactPool = new Queue<GameObject>();

    // Dictionary for generic particle access
    private Dictionary<string, Queue<GameObject>> genericPools = new Dictionary<string, Queue<GameObject>>();
    private Dictionary<string, GameObject> prefabLookup = new Dictionary<string, GameObject>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        StartCoroutine(PreloadPools());
        InitializeGenericPools();
    }

    void InitializeGenericPools()
    {
        // Add prefabs to lookup
        if (teleportParticlePrefab != null)
        {
            prefabLookup[teleportParticlePrefab.name] = teleportParticlePrefab;
            genericPools[teleportParticlePrefab.name] = teleportPool;
        }
        if (hitParticlePrefab != null)
        {
            prefabLookup[hitParticlePrefab.name] = hitParticlePrefab;
            genericPools[hitParticlePrefab.name] = hitPool;
        }
        if (impactEffectPrefab != null)
        {
            prefabLookup[impactEffectPrefab.name] = impactEffectPrefab;
            genericPools[impactEffectPrefab.name] = impactPool;
        }
    }

    IEnumerator PreloadPools()
    {
        for (int i = 0; i < poolSize; i++)
        {
            if (teleportParticlePrefab != null)
            {
                GameObject obj = Instantiate(teleportParticlePrefab);
                obj.SetActive(false);
                teleportPool.Enqueue(obj);
            }
            yield return null;
        }

        for (int i = 0; i < poolSize; i++)
        {
            if (hitParticlePrefab != null)
            {
                GameObject obj = Instantiate(hitParticlePrefab);
                obj.SetActive(false);
                hitPool.Enqueue(obj);
            }
            yield return null;
        }

        for (int i = 0; i < poolSize; i++)
        {
            if (impactEffectPrefab != null)
            {
                GameObject obj = Instantiate(impactEffectPrefab);
                obj.SetActive(false);
                impactPool.Enqueue(obj);
            }
            yield return null;
        }

        Debug.Log("Particle pools preloaded successfully!");
    }

    // ===== ORIGINAL METHODS =====

    public GameObject GetTeleportParticle() => GetParticleFromPool(teleportPool, teleportParticlePrefab);
    public void ReturnTeleportParticle(GameObject obj, float delay) => StartCoroutine(ReturnAfterDelay(obj, delay, teleportPool));

    public GameObject GetHitParticle() => GetParticleFromPool(hitPool, hitParticlePrefab);
    public void ReturnHitParticle(GameObject obj, float delay) => StartCoroutine(ReturnAfterDelay(obj, delay, hitPool));

    public GameObject GetImpactEffect() => GetParticleFromPool(impactPool, impactEffectPrefab);
    public void ReturnImpactEffect(GameObject obj, float delay) => StartCoroutine(ReturnAfterDelay(obj, delay, impactPool));

    private GameObject GetParticleFromPool(Queue<GameObject> pool, GameObject prefab)
    {
        if (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            obj.SetActive(true);
            return obj;
        }
        else
        {
            return Instantiate(prefab);
        }
    }

    private IEnumerator ReturnAfterDelay(GameObject obj, float delay, Queue<GameObject> pool)
    {
        yield return new WaitForSeconds(delay);
        obj.SetActive(false);
        pool.Enqueue(obj);
    }

    // ===== NEW GENERIC METHODS (FIXES THE ERRORS) =====

    /// <summary>
    /// Get a particle from the pool by name
    /// </summary>
    public GameObject GetParticle(string particleName)
    {
        // Check if we have this particle in our lookup
        if (prefabLookup.ContainsKey(particleName))
        {
            // Find the correct pool
            if (genericPools.ContainsKey(particleName))
            {
                Queue<GameObject> pool = genericPools[particleName];
                GameObject prefab = prefabLookup[particleName];
                return GetParticleFromPool(pool, prefab);
            }
        }

        // Try to find by matching names in the pools
        foreach (var kvp in genericPools)
        {
            if (kvp.Key.Contains(particleName) || particleName.Contains(kvp.Key))
            {
                return GetParticleFromPool(kvp.Value, prefabLookup[kvp.Key]);
            }
        }

        Debug.LogWarning($"Particle '{particleName}' not found in pool!");
        return null;
    }

    /// <summary>
    /// Return a particle to the pool
    /// </summary>
    public void ReturnParticle(GameObject particle)
    {
        if (particle == null) return;

        string particleName = particle.name.Replace("(Clone)", "").Trim();

        // Find which pool this particle belongs to
        foreach (var kvp in genericPools)
        {
            if (kvp.Key.Contains(particleName) || particleName.Contains(kvp.Key))
            {
                particle.SetActive(false);
                particle.transform.position = Vector3.zero;
                particle.transform.rotation = Quaternion.identity;
                particle.transform.SetParent(transform);
                kvp.Value.Enqueue(particle);
                return;
            }
        }

        // If no pool found, destroy it
        Debug.LogWarning($"No pool found for particle '{particleName}', destroying...");
        Destroy(particle);
    }

    /// <summary>
    /// Get a particle by name with position
    /// </summary>
    public GameObject GetParticle(string particleName, Vector3 position)
    {
        GameObject particle = GetParticle(particleName);
        if (particle != null)
        {
            particle.transform.position = position;
        }
        return particle;
    }

    /// <summary>
    /// Clear all pools
    /// </summary>
    public void ClearPools()
    {
        ClearPool(teleportPool);
        ClearPool(hitPool);
        ClearPool(impactPool);
        genericPools.Clear();
        prefabLookup.Clear();
    }

    private void ClearPool(Queue<GameObject> pool)
    {
        while (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            if (obj != null)
                Destroy(obj);
        }
    }

    /// <summary>
    /// Get the pool size for a specific particle
    /// </summary>
    public int GetPoolSize(string particleName)
    {
        if (genericPools.ContainsKey(particleName))
            return genericPools[particleName].Count;
        return 0;
    }

    /// <summary>
    /// Get total count of all pooled objects
    /// </summary>
    public int GetTotalPoolCount()
    {
        return teleportPool.Count + hitPool.Count + impactPool.Count;
    }
}