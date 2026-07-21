using UnityEngine;

public class LightOrb : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int scoreValue = 10;
    [SerializeField] private float floatSpeed = 0.8f;
    [SerializeField] private float floatHeight = 0.3f;
    [SerializeField] private float rotationSpeed = 120f;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseAmount = 0.1f;
    [SerializeField] private float collectionCooldown = 0.5f;

    [Header("Effects")]
    [SerializeField] private GameObject orbCollectParticles;
    [SerializeField] private AudioClip collectSound;

    [Header("References")]
    [SerializeField] private LightOrbSpawner spawner; // Auto-assigned if null

    private Vector2 startPosition;
    private float floatTimer;
    private bool isCollected = false;
    private float collectionTimer = 0f;

    void Start()
    {
        startPosition = transform.position;
        floatTimer = Random.Range(0f, 2f);

        // Auto-find spawner if not assigned
        if (spawner == null)
            spawner = FindObjectOfType<LightOrbSpawner>();
    }

    void Update()
    {
        // Cooldown timer
        if (isCollected)
        {
            collectionTimer -= Time.deltaTime;
            if (collectionTimer <= 0)
                isCollected = false;
        }

        // Floating animation
        floatTimer += Time.deltaTime * floatSpeed;
        float yOffset = Mathf.Sin(floatTimer) * floatHeight;
        transform.position = new Vector2(startPosition.x, startPosition.y + yOffset);

        // Rotation
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);

        // Pulsing scale
        float pulse = 1f + Mathf.Sin(floatTimer * pulseSpeed) * pulseAmount;
        transform.localScale = new Vector3(pulse, pulse, 1);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isCollected)
        {
            Collect();
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        // Safety check: if player is inside but OnTriggerEnter didn't fire
        if (other.CompareTag("Player") && !isCollected)
        {
            Collect();
        }
    }

    void Collect()
    {
        // Prevent double collection
        if (isCollected) return;
        isCollected = true;
        collectionTimer = collectionCooldown;

        // Add score through GameManager
        if (GameManager.Instance != null)
            GameManager.Instance.AddScore(scoreValue);

        // Spawn particles
        if (orbCollectParticles != null)
            Instantiate(orbCollectParticles, transform.position, Quaternion.identity);

        // Play sound
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position, 0.8f);
        }

        // Return to spawner pool or destroy
        if (spawner != null)
        {
            // Use the spawner's pooling system
            spawner.ReturnOrbToPool(gameObject);
        }
        else
        {
            // Fallback: just destroy
            Destroy(gameObject);
        }
    }

    // Reset method for pooling
    public void ResetOrb()
    {
        isCollected = false;
        collectionTimer = 0f;
        startPosition = transform.position;
        floatTimer = Random.Range(0f, 2f);
        gameObject.SetActive(true);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}