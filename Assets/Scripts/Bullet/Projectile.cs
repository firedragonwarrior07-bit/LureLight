using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifetime = 3f;

    [Header("Damage")]
    [SerializeField] private int damage = 1;
    [SerializeField] private bool destroyOnHit = true;
    [SerializeField] private int maxPenetrations = 0; // 0 = no penetration

    [Header("Effects")]
    [SerializeField] private GameObject hitParticles;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private float hitSoundVolume = 0.8f;

    [Header("Visual")]
    [SerializeField] private TrailRenderer trailRenderer;

    // Private variables
    private Vector2 direction = Vector2.right;
    private int penetrationsRemaining = 0;
    private bool isActive = true;
    private GameObject owner; // Who shot this projectile

    void Start()
    {
        penetrationsRemaining = maxPenetrations;
        Destroy(gameObject, lifetime);

        // Auto-disable trail if not assigned
        if (trailRenderer == null)
            trailRenderer = GetComponent<TrailRenderer>();
    }

    void Update()
    {
        if (!isActive) return;
        
        // Move projectile
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive) return;
        
        // Ignore certain tags
        if (other.CompareTag("Player")) return;
        if (other.CompareTag("LightOrb")) return;
        if (other.CompareTag("Projectile")) return;

        // Check if we hit an enemy
        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                OnHit(other);
                return;
            }
        }

        // Hit anything else (walls, ground, etc.)
        OnHit(other);
    }

    void OnHit(Collider2D hitObject)
    {
        // Spawn particles
        if (hitParticles != null)
        {
            Vector3 hitPoint = hitObject.ClosestPoint(transform.position);
            Instantiate(hitParticles, hitPoint, Quaternion.identity);
        }

        // Play sound
        if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, transform.position, hitSoundVolume);
        }

        // Handle penetration
        if (penetrationsRemaining > 0 && hitObject.CompareTag("Enemy"))
        {
            penetrationsRemaining--;
            return; // Don't destroy, continue flying
        }

        // Destroy or deactivate
        if (destroyOnHit)
        {
            // Disable trail before destroying
            if (trailRenderer != null)
                trailRenderer.enabled = false;
            
            Destroy(gameObject);
        }
        else
        {
            isActive = false;
            gameObject.SetActive(false);
        }
    }

    // ===== PUBLIC METHODS =====

    /// <summary>
    /// Set the direction of the projectile
    /// </summary>
    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;
        
        // Rotate to face direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    /// <summary>
    /// Set the damage value
    /// </summary>
    public void SetDamage(int newDamage)
    {
        damage = newDamage;
    }

    /// <summary>
    /// Set who shot this projectile
    /// </summary>
    public void SetOwner(GameObject newOwner)
    {
        owner = newOwner;
    }

    /// <summary>
    /// Set the speed of the projectile
    /// </summary>
    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }

    /// <summary>
    /// Enable penetration (piercing bullets)
    /// </summary>
    public void SetPenetration(int count)
    {
        maxPenetrations = count;
        penetrationsRemaining = count;
    }

    /// <summary>
    /// Reset projectile for pooling
    /// </summary>
    public void ResetProjectile()
    {
        isActive = true;
        penetrationsRemaining = maxPenetrations;
        gameObject.SetActive(true);
        
        if (trailRenderer != null)
            trailRenderer.Clear();
    }

    // ===== DEBUG =====
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, direction * 2f);
    }
}