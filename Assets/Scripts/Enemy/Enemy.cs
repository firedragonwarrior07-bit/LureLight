using UnityEngine;
using System.Collections;

/// <summary>
/// Advanced Enemy AI with Priority System for Lurelight
/// Integrates with your existing GameManager, PlayerController, and spawners
/// </summary>
public class Enemy : MonoBehaviour
{
    #region Enums
    public enum EnemyType { Normal, Flying, Teleporting }
    private enum AIState { Patrol, Chase, Suspicious, Retreat, OrbAttraction, Ambush }
    #endregion

    #region Serialized Fields
    [Header("Enemy Type")]
    [SerializeField] private EnemyType enemyType = EnemyType.Normal;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float chaseSpeed = 5f;
    [SerializeField] private float patrolSpeed = 1.5f;
    [SerializeField] private float retreatSpeed = 4.5f;
    [SerializeField] private float patrolRange = 3f;

    [Header("Detection - IMPROVED")]
    [SerializeField] private float chaseRange = 7f;        // Increased from 5f
    [SerializeField] private float loseAggroRange = 12f;   // Increased from 8f
    [SerializeField] private float suspiciousRange = 12f;  // Increased from 10f
    [SerializeField] private float suspiciousDuration = 4f; // Increased from 3f
    [SerializeField] private float fieldOfViewAngle = 70f;  // Increased from 60f
    [SerializeField] private LayerMask obstacleLayer = -1;
    [SerializeField] private float detectionInterval = 0.2f; // New: less frequent checks

    [Header("Combat")]
    [SerializeField] private int damageToPlayer = 1;
    [SerializeField] private float attackCooldown = 2f;    // Increased from 1.5f
    [SerializeField] private float attackRange = 1.5f;     // Increased from 1.2f

    [Header("Health")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private int currentHealth;
    [SerializeField] private float retreatHealthThreshold = 0.3f;

    [Header("Orb Attraction")]
    [SerializeField] private float attractionRange = 6f;
    [SerializeField] private float attractionSpeed = 2.5f;

    [Header("Ambush - IMPROVED")]
    [SerializeField] private bool startHidden = false;     // Changed from true to false
    [SerializeField] private float revealDelay = 1.5f;     // Increased from 0.5f
    [SerializeField] private float ambushRange = 3f;       // Decreased from 4f

    [Header("Teleport (Teleporting Type)")]
    [SerializeField] private float teleportCooldown = 3f;
    [SerializeField] private float teleportRange = 3f;

    [Header("VFX")]
    [SerializeField] private GameObject deathParticles;
    [SerializeField] private GameObject hitParticles;
    [SerializeField] private GameObject revealParticles;

    [Header("Score")]
    [SerializeField] private int scoreOnDeath = 10;
    #endregion

    #region Private Variables
    private Transform player;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Collider2D enemyCollider;

    private AIState currentState = AIState.Patrol;
    private AIState previousState = AIState.Patrol;
    private float stateTimer = 0f;
    private Vector2 patrolTarget;
    private Vector2 retreatTarget;
    private Vector2 ambushPosition;

    private GameObject targetOrb;
    private float nextOrbCheckTime = 0f;
    private Vector2 lastKnownPlayerPosition;
    private float suspiciousTimer = 0f;
    private bool playerRecentlySeen = false;

    private bool isHidden = false;
    private bool isRevealing = false;
    private bool isSpawned = false;
    private float spawnTimer = 0f;
    private float detectionTimer = 0f; // New: for less frequent checks

    private float nextAttackTime = 0f;
    private float nextTeleportTime = 0f;
    
    // New: Ground check for jumping
    private bool isGrounded = false;
    #endregion

    #region Unity Lifecycle
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        enemyCollider = GetComponent<Collider2D>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        if (player == null)
            Debug.LogError("[Enemy] No Player found in scene!");

        currentHealth = maxHealth;
        ambushPosition = transform.position;
        SetEnemyColor();
        SetRandomPatrolTarget();

        // Start hidden if configured
        if (startHidden)
        {
            isSpawned = false;
            spawnTimer = 0f;
        }
        else
        {
            isSpawned = true;
        }
    }

    void Update()
    {
        if (player == null) return;

        // Handle spawn delay
        if (!isSpawned)
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= 2f)
            {
                isSpawned = true;
                if (startHidden) Hide();
            }
            return;
        }

        UpdateStateMachine();
        ApplyStateBehavior();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Bullet"))
        {
            TakeDamage(1);
            Destroy(other.gameObject);
            
            if (isHidden) StartCoroutine(Reveal());
        }
        else if (other.CompareTag("Player") && !isHidden && Time.time >= nextAttackTime)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.TakeDamage(damageToPlayer);
            
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    // New: Ground detection for jumping enemies
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground") || 
            collision.gameObject.CompareTag("Platform"))
        {
            isGrounded = true;
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground") || 
            collision.gameObject.CompareTag("Platform"))
        {
            isGrounded = false;
        }
    }
    #endregion

    #region State Machine
    private void UpdateStateMachine()
    {
        // PRIORITY 0: Retreat (Survival)
        if (ShouldRetreat() && currentState != AIState.Retreat)
        {
            TransitionToState(AIState.Retreat);
            return;
        }
        if (currentState == AIState.Retreat && !ShouldRetreat())
        {
            TransitionToState(previousState);
            return;
        }
        if (currentState == AIState.Retreat) return;

        // Skip AI if hidden
        if (isHidden)
        {
            if (currentState != AIState.Ambush)
                TransitionToState(AIState.Ambush);
            
            if (Vector2.Distance(transform.position, player.position) < ambushRange && !isRevealing)
                StartCoroutine(Reveal());
            
            return;
        }

        // Less frequent detection checks for performance
        detectionTimer += Time.deltaTime;
        if (detectionTimer < detectionInterval) return;
        detectionTimer = 0f;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        bool playerVisible = IsPlayerVisible();

        // PRIORITY 1: Chase Player
        if (playerVisible && distanceToPlayer <= chaseRange)
        {
            if (currentState != AIState.Chase)
            {
                TransitionToState(AIState.Chase);
                lastKnownPlayerPosition = player.position;
            }
            return;
        }

        // Suspicious State: Lost sight but player nearby
        if (playerRecentlySeen && distanceToPlayer <= suspiciousRange)
        {
            if (currentState != AIState.Suspicious)
                TransitionToState(AIState.Suspicious);
            return;
        }

        // PRIORITY 2: Go to Orbs (only if player is far)
        if (distanceToPlayer > chaseRange)
        {
            FindNearestOrb();
            if (targetOrb != null && currentState != AIState.OrbAttraction)
            {
                TransitionToState(AIState.OrbAttraction);
                return;
            }
        }

        // PRIORITY 3: Patrol
        if (currentState != AIState.Patrol)
            TransitionToState(AIState.Patrol);
    }

    private void TransitionToState(AIState newState)
    {
        previousState = currentState;
        currentState = newState;
        stateTimer = 0f;
        
        switch (newState)
        {
            case AIState.Patrol:
                SetRandomPatrolTarget();
                break;
            case AIState.Chase:
                lastKnownPlayerPosition = player.position;
                break;
            case AIState.Suspicious:
                suspiciousTimer = suspiciousDuration;
                lastKnownPlayerPosition = player.position;
                break;
            case AIState.Retreat:
                SetRetreatTarget();
                break;
            case AIState.OrbAttraction:
                FindNearestOrb();
                break;
        }
    }

    private void ApplyStateBehavior()
    {
        switch (currentState)
        {
            case AIState.Patrol: PatrolBehavior(); break;
            case AIState.Chase: ChaseBehavior(); break;
            case AIState.Suspicious: SuspiciousBehavior(); break;
            case AIState.Retreat: RetreatBehavior(); break;
            case AIState.OrbAttraction: OrbAttractionBehavior(); break;
            case AIState.Ambush: break;
        }
    }
    #endregion

    #region State Behaviors
    private void PatrolBehavior()
    {
        float dist = Vector2.Distance(transform.position, patrolTarget);
        if (dist < 0.2f)
            SetRandomPatrolTarget();
        else
            MoveToward(patrolTarget, patrolSpeed);
    }

    private void ChaseBehavior()
    {
        float dist = Vector2.Distance(transform.position, player.position);
        
        bool canSeePlayer = IsPlayerVisible();
        
        if (!canSeePlayer && dist <= loseAggroRange)
        {
            playerRecentlySeen = true;
            TransitionToState(AIState.Suspicious);
            return;
        }
        
        if (dist > loseAggroRange)
        {
            playerRecentlySeen = false;
            TransitionToState(AIState.Patrol);
            return;
        }

        if (dist <= attackRange && Time.time >= nextAttackTime)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Improved chase with jumping for ground enemies
        Vector2 directionToPlayer = (player.position - transform.position).normalized;
        
        if (enemyType == EnemyType.Flying)
        {
            // Flying enemies move directly
            rb.linearVelocity = directionToPlayer * chaseSpeed;
        }
        else if (enemyType == EnemyType.Teleporting)
        {
            rb.linearVelocity = directionToPlayer * chaseSpeed;
            TryTeleport();
        }
        else
        {
            // Ground enemies - move horizontally and jump when needed
            rb.linearVelocity = new Vector2(directionToPlayer.x * chaseSpeed, rb.linearVelocity.y);
            
            // Jump to reach player if they're above
            if (player.position.y > transform.position.y + 1.5f && isGrounded)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 6f);
            }
        }
        
        lastKnownPlayerPosition = player.position;
        FlipSprite(directionToPlayer.x);
    }

    private void SuspiciousBehavior()
    {
        suspiciousTimer -= Time.deltaTime;
        
        float dist = Vector2.Distance(transform.position, lastKnownPlayerPosition);
        if (dist > 0.5f)
            MoveToward(lastKnownPlayerPosition, moveSpeed * 0.7f);
        else
            rb.linearVelocity = Vector2.zero;

        // Re-acquire player if seen
        if (IsPlayerVisible() && Vector2.Distance(transform.position, player.position) <= chaseRange)
        {
            playerRecentlySeen = false;
            TransitionToState(AIState.Chase);
            return;
        }

        if (suspiciousTimer <= 0f)
        {
            playerRecentlySeen = false;
            TransitionToState(AIState.Patrol);
        }
    }

    private void RetreatBehavior()
    {
        stateTimer += Time.deltaTime;
        
        if (player != null)
        {
            Vector2 retreatDir = (transform.position - player.position).normalized;
            Vector2 target = (Vector2)transform.position + retreatDir * 5f;
            MoveToward(target, retreatSpeed);
        }

        if (!ShouldRetreat() || stateTimer > 3f)
            TransitionToState(previousState != AIState.Retreat ? previousState : AIState.Patrol);
    }

    private void OrbAttractionBehavior()
    {
        // Check if player is close enough to switch to chase
        if (IsPlayerVisible() && Vector2.Distance(transform.position, player.position) <= chaseRange)
        {
            TransitionToState(AIState.Chase);
            return;
        }

        if (Time.time >= nextOrbCheckTime)
        {
            FindNearestOrb();
            nextOrbCheckTime = Time.time + 0.5f;
        }

        if (targetOrb != null)
            MoveToward(targetOrb.transform.position, attractionSpeed);
        else
            TransitionToState(AIState.Patrol);
    }
    #endregion

    #region Core Methods
    private void MoveToward(Vector2 target, float speed)
    {
        Vector2 direction = (target - (Vector2)transform.position).normalized;
        
        switch (enemyType)
        {
            case EnemyType.Flying:
                rb.linearVelocity = direction * speed;
                break;
            case EnemyType.Teleporting:
                rb.linearVelocity = direction * speed;
                TryTeleport();
                break;
            default:
                rb.linearVelocity = new Vector2(direction.x * speed, rb.linearVelocity.y);
                break;
        }
        
        FlipSprite(direction.x);
    }

    private void FlipSprite(float direction)
    {
        if (Mathf.Abs(direction) < 0.01f) return;
        transform.localScale = new Vector3(Mathf.Sign(direction), 1, 1);
    }

    private void TryTeleport()
    {
        if (Time.time < nextTeleportTime || Random.value > 0.05f) return;
        
        Vector2 teleportPos = (Vector2)transform.position + Random.insideUnitCircle * teleportRange;
        Collider2D hit = Physics2D.OverlapCircle(teleportPos, 0.5f, obstacleLayer);
        
        if (hit == null)
        {
            transform.position = teleportPos;
            nextTeleportTime = Time.time + teleportCooldown;
            if (hitParticles != null)
                Instantiate(hitParticles, transform.position, Quaternion.identity);
        }
    }

    private bool IsPlayerVisible()
    {
        if (player == null) return false;
        
        Vector2 dir = (player.position - transform.position).normalized;
        float dist = Vector2.Distance(transform.position, player.position);
        
        // Check angle
        float angle = Vector2.Angle(transform.right, dir);
        if (angle > fieldOfViewAngle / 2f) return false;
        
        // Raycast for obstacles
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, dist, obstacleLayer);
        return hit.collider == null || hit.collider.CompareTag("Player");
    }

    private void FindNearestOrb()
    {
        GameObject[] orbs = GameObject.FindGameObjectsWithTag("LightOrb");
        targetOrb = null;
        float closest = attractionRange;
        
        foreach (GameObject orb in orbs)
        {
            float dist = Vector2.Distance(transform.position, orb.transform.position);
            if (dist < closest)
            {
                closest = dist;
                targetOrb = orb;
            }
        }
    }

    private void SetRandomPatrolTarget()
    {
        patrolTarget = (Vector2)ambushPosition + Random.insideUnitCircle * patrolRange;
    }

    private void SetRetreatTarget()
    {
        if (player != null)
            retreatTarget = (Vector2)transform.position + ((Vector2)transform.position - (Vector2)player.position).normalized * 5f;
        else
            SetRandomPatrolTarget();
    }

    private bool ShouldRetreat() => (float)currentHealth / maxHealth <= retreatHealthThreshold;

    private void SetEnemyColor()
    {
        if (spriteRenderer == null) return;
        switch (enemyType)
        {
            case EnemyType.Normal: spriteRenderer.color = Color.red; break;
            case EnemyType.Flying: spriteRenderer.color = Color.blue; break;
            case EnemyType.Teleporting: spriteRenderer.color = Color.green; break;
        }
    }
    #endregion

    #region Ambush System
    private void Hide()
    {
        isHidden = true;
        isRevealing = false;
        currentState = AIState.Ambush;
        
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        if (enemyCollider != null) enemyCollider.enabled = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    private IEnumerator Reveal()
    {
        if (isRevealing) yield break;
        isRevealing = true;

        if (revealParticles != null)
            Instantiate(revealParticles, transform.position, Quaternion.identity);

        yield return new WaitForSeconds(revealDelay);

        if (spriteRenderer != null) spriteRenderer.enabled = true;
        if (enemyCollider != null) enemyCollider.enabled = true;

        isHidden = false;
        isRevealing = false;
        TransitionToState(AIState.Chase);
        
        if (rb != null && player != null)
        {
            Vector2 burst = (player.position - transform.position).normalized * chaseSpeed * 1.5f;
            rb.linearVelocity = burst;
        }
    }
    #endregion

    #region Combat
    public void TakeDamage(int damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
        
        if (hitParticles != null)
            Instantiate(hitParticles, transform.position, Quaternion.identity);

        if (spriteRenderer != null)
            StartCoroutine(FlashSprite());

        if (currentHealth <= 0) Die();
    }

    private IEnumerator FlashSprite()
    {
        if (spriteRenderer == null) yield break;
        Color original = spriteRenderer.color;
        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = original;
    }

    private void Die()
    {
        if (deathParticles != null)
            Instantiate(deathParticles, transform.position, Quaternion.identity);
        
        if (GameManager.Instance != null)
            GameManager.Instance.AddScore(scoreOnDeath);
        
        Destroy(gameObject);
    }
    #endregion

    // ===== NEW METHOD: Apply Difficulty Scaling =====
    public void ApplyDifficultyScaling(float healthMultiplier, float speedMultiplier, int totalUpgrades)
    {
        int newMaxHealth = Mathf.RoundToInt(maxHealth * healthMultiplier);
        maxHealth = newMaxHealth;
        currentHealth = maxHealth;

        moveSpeed *= speedMultiplier;
        chaseSpeed *= speedMultiplier;
        patrolSpeed *= speedMultiplier;
        retreatSpeed *= speedMultiplier;

        scoreOnDeath = Mathf.RoundToInt(scoreOnDeath * (1f + totalUpgrades * 0.1f));

        Debug.Log($"[Enemy] Scaled: Health={maxHealth}, Speed={moveSpeed:F2}, Score={scoreOnDeath}");
    }
}