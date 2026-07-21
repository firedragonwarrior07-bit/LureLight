using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Movement - IMPROVED")]
    public float moveSpeed = 7f;
    public float acceleration = 15f;
    public float deceleration = 20f;
    public float maxSpeed = 9f;
    public float groundFriction = 0.92f;

    [Header("Jump System")]
    public float jumpForce = 12f;
    public float minJumpForce = 5f;
    public float maxJumpForce = 20f;
    public float chargeSpeed = 10f;
    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 2f;
    public float coyoteTime = 0.15f;
    public float jumpBufferTime = 0.15f;
    public int maxAirJumps = 1;
    private int airJumpsRemaining = 0;

    [Header("Ground Detection")]
    public GroundDetector groundDetector;
    public LayerMask groundLayer;
    public Transform groundCheckPoint;
    public float groundCheckRadius = 0.2f;

    [Header("Wall Detection")]
    public Transform wallCheckPoint;
    public float wallCheckRadius = 0.2f;
    public bool isTouchingWall = false;

    [Header("Shooting")]
    public GameObject bulletPrefab;
    public float shootCooldown = 0.5f;
    public Transform shootPoint;
    public bool aimWithMouse = true;

    [Header("Dash")]
    public float dashDistance = 3f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    private bool isDashing = false;
    private float dashTimer = 0f;
    private float nextDashTime = 0f;
    private Vector2 dashDirection;

    [Header("Animations")]
    public Animator animator;

    [Header("Effects")]
    public GameObject deathParticles;
    public GameObject hitParticles;
    public GameObject orbCollectParticles;
    public GameObject dashParticles;

    [Header("Invincibility")]
    public float invincibilityTime = 1f;
    private bool isInvincible = false;
    private float invincibilityTimer = 0f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip jumpSound;
    public AudioClip shootSound;
    public AudioClip damageSound;
    public AudioClip deathSound;
    public AudioClip orbCollectSound;

    // Private variables
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private float currentHorizontalSpeed = 0f;
    private float nextShootTime = 0f;
    private float moveInput = 0f;
    private bool isFacingRight = true;
    private bool isGrounded = false;
    private bool isChargingJump = false;
    private float currentJumpForce = 0f;
    private bool hasJumped = false;
    private float coyoteTimer = 0f;
    private float jumpBufferTimer = 0f;
    private Vector3 originalScale;

    // ===== UNITY LIFECYCLE =====
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (groundCheckPoint == null)
        {
            GameObject checkPoint = new GameObject("GroundCheck");
            checkPoint.transform.parent = transform;
            checkPoint.transform.localPosition = new Vector3(0, -0.5f, 0);
            groundCheckPoint = checkPoint.transform;
        }

        if (wallCheckPoint == null)
        {
            GameObject checkPoint = new GameObject("WallCheck");
            checkPoint.transform.parent = transform;
            checkPoint.transform.localPosition = new Vector3(0.5f, 0, 0);
            wallCheckPoint = checkPoint.transform;
        }

        if (shootPoint == null)
        {
            GameObject point = new GameObject("ShootPoint");
            point.transform.parent = transform;
            point.transform.localPosition = new Vector3(0.7f, 0.3f, 0);
            shootPoint = point.transform;
        }

        originalScale = transform.localScale;
        airJumpsRemaining = maxAirJumps;
        
        ApplyUpgrades();
    }

    void Update()
    {
        UpdateTimers();
        HandleInvincibility();
        HandleMovementInput();
        HandleJumpInput();
        HandleShootInput();
        HandleDashInput();
        UpdateGroundedState();
        UpdateAnimations();
    }

    void FixedUpdate()
    {
        if (isDashing)
        {
            HandleDashMovement();
            return;
        }

        ApplyMovement();
        ApplyJumpPhysics();
        UpdateWallCheck();
    }

    // ===== TIMERS =====
    void UpdateTimers()
    {
        if (isGrounded)
            coyoteTimer = coyoteTime;
        else
            coyoteTimer -= Time.deltaTime;

        if (jumpBufferTimer > 0)
            jumpBufferTimer -= Time.deltaTime;

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0)
                EndDash();
        }
    }

    // ===== GROUND & WALL DETECTION =====
    void UpdateGroundedState()
    {
        bool wasGrounded = isGrounded;
        
        if (groundDetector != null)
        {
            isGrounded = groundDetector.isGrounded;
        }
        else if (groundCheckPoint != null)
        {
            Collider2D hit = Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayer);
            isGrounded = hit != null;
        }
        else
        {
            isGrounded = false;
        }

        if (isGrounded && !wasGrounded)
        {
            airJumpsRemaining = maxAirJumps;
            hasJumped = false;
        }
    }

    void UpdateWallCheck()
    {
        if (wallCheckPoint != null)
        {
            Collider2D hit = Physics2D.OverlapCircle(wallCheckPoint.position, wallCheckRadius, groundLayer);
            isTouchingWall = hit != null;
        }
    }

    // ===== HANDLE INPUT =====
    void HandleMovementInput()
    {
        moveInput = Input.GetAxisRaw("Horizontal");
    }

    void HandleJumpInput()
    {
        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferTimer = jumpBufferTime;
        }

        if (Input.GetButtonDown("Jump") && (isGrounded || airJumpsRemaining > 0) && !isChargingJump && !hasJumped)
        {
            StartJumpCharge();
        }

        if (isChargingJump && Input.GetButton("Jump"))
        {
            ChargeJump();
        }

        if (Input.GetButtonUp("Jump") && isChargingJump)
        {
            ReleaseJump();
        }

        if (jumpBufferTimer > 0 && (isGrounded || airJumpsRemaining > 0) && !isChargingJump && !hasJumped)
        {
            StartJumpCharge();
            jumpBufferTimer = 0f;
        }

        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0 && !isChargingJump)
        {
            float cutForce = Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime;
            rb.linearVelocity += new Vector2(0, cutForce);
        }
    }

    void HandleShootInput()
    {
        if (Input.GetButtonDown("Fire1") && Time.time >= nextShootTime && bulletPrefab != null)
        {
            Shoot();
            nextShootTime = Time.time + shootCooldown;
        }
    }

    void HandleDashInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) && Time.time >= nextDashTime && !isDashing && !isChargingJump)
        {
            StartDash();
        }
    }

    // ===== JUMP SYSTEM =====
    void StartJumpCharge()
    {
        isChargingJump = true;
        currentJumpForce = minJumpForce;
        hasJumped = false;
        
        if (!isGrounded && airJumpsRemaining > 0)
        {
            airJumpsRemaining--;
        }
    }

    void ChargeJump()
    {
        currentJumpForce += chargeSpeed * Time.deltaTime;
        currentJumpForce = Mathf.Min(currentJumpForce, maxJumpForce);

        float chargePercent = (currentJumpForce - minJumpForce) / (maxJumpForce - minJumpForce);
        float scaleY = 1f - (chargePercent * 0.1f);
        float scaleX = 1f + (chargePercent * 0.05f);
        transform.localScale = new Vector3(
            originalScale.x * (isFacingRight ? scaleX : scaleX),
            originalScale.y * scaleY,
            originalScale.z
        );
    }

    void ReleaseJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, currentJumpForce);
        hasJumped = true;
        isChargingJump = false;
        coyoteTimer = 0f;
        transform.localScale = originalScale;
        PlaySound(jumpSound);
    }

    // ===== DASH SYSTEM =====
    void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        nextDashTime = Time.time + dashCooldown;
        
        if (moveInput != 0)
            dashDirection = new Vector2(moveInput, 0).normalized;
        else
            dashDirection = isFacingRight ? Vector2.right : Vector2.left;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            dashDirection.y = 0.5f;

        dashDirection.Normalize();

        if (dashParticles != null)
            Instantiate(dashParticles, transform.position, Quaternion.identity);

        isInvincible = true;
        invincibilityTimer = dashDuration + 0.1f;
    }

    void HandleDashMovement()
    {
        rb.linearVelocity = dashDirection * (dashDistance / dashDuration);
    }

    void EndDash()
    {
        isDashing = false;
        rb.linearVelocity *= 0.5f;
    }

    // ===== MOVEMENT - IMPROVED =====
    void ApplyMovement()
    {
        if (isChargingJump)
        {
            moveInput *= 0.3f;
        }

        float targetSpeed = moveInput * moveSpeed;
        targetSpeed = Mathf.Clamp(targetSpeed, -maxSpeed, maxSpeed);
        
        float speedChange;
        if (Mathf.Abs(moveInput) > 0.01f)
        {
            speedChange = acceleration;
        }
        else
        {
            speedChange = isGrounded ? deceleration * 1.5f : deceleration;
        }
        
        currentHorizontalSpeed = Mathf.MoveTowards(currentHorizontalSpeed, targetSpeed, speedChange * Time.fixedDeltaTime);

        if (isGrounded && Mathf.Abs(moveInput) < 0.01f)
        {
            currentHorizontalSpeed *= groundFriction;
            if (Mathf.Abs(currentHorizontalSpeed) < 0.05f)
                currentHorizontalSpeed = 0f;
        }

        rb.linearVelocity = new Vector2(currentHorizontalSpeed, rb.linearVelocity.y);

        if (moveInput > 0 && !isFacingRight)
            Flip();
        else if (moveInput < 0 && isFacingRight)
            Flip();
    }

    void ApplyJumpPhysics()
    {
        if (rb.linearVelocity.y < 0)
        {
            float fallForce = Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
            rb.linearVelocity += new Vector2(0, fallForce);
        }
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x = originalScale.x * (isFacingRight ? 1 : -1);
        transform.localScale = scale;
    }

    // ===== SHOOTING =====
    void Shoot()
    {
        Vector2 direction;
        
        if (aimWithMouse)
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0f;
            direction = (mousePos - shootPoint.position).normalized;
        }
        else
        {
            direction = isFacingRight ? Vector2.right : Vector2.left;
        }

        GameObject bullet = Instantiate(bulletPrefab, shootPoint.position, Quaternion.identity);
        
        Projectile projectile = bullet.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.SetDirection(direction);
        }

        PlaySound(shootSound);
    }

    // ===== INVINCIBILITY =====
    void HandleInvincibility()
    {
        if (isInvincible)
        {
            invincibilityTimer -= Time.deltaTime;
            
            if (spriteRenderer != null)
            {
                float flashSpeed = 10f;
                bool visible = Mathf.Sin(Time.time * flashSpeed) > 0;
                spriteRenderer.enabled = visible;
            }

            if (invincibilityTimer <= 0)
            {
                isInvincible = false;
                if (spriteRenderer != null)
                {
                    spriteRenderer.enabled = true;
                    spriteRenderer.color = Color.white;
                }
            }
        }
    }

    // ===== COLLISION - FULLY FIXED =====
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!gameObject.activeInHierarchy) return;

        if (other.CompareTag("LightOrb"))
        {
            CollectOrb(other.gameObject);
        }

        if (other.CompareTag("Enemy") && !isInvincible && !isDashing)
        {
            TakeDamage(1);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Stomp enemies from above
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Enemy enemy = collision.gameObject.GetComponent<Enemy>();
            if (enemy != null)
            {
                float contactY = collision.contacts[0].point.y;
                float playerBottom = transform.position.y - 0.5f;
                
                if (playerBottom > contactY + 0.1f && rb.linearVelocity.y < 0)
                {
                    // Stomp! Kill the enemy
                    enemy.TakeDamage(999);
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * 0.8f);
                    PlaySound(jumpSound);
                }
                else
                {
                    // Push player away from enemy
                    Vector2 pushDirection = (transform.position - collision.transform.position).normalized;
                    rb.linearVelocity = new Vector2(pushDirection.x * 3f, rb.linearVelocity.y * 0.5f);
                    
                    // ✅ FIX: Proper null check before taking damage
                    if (!isInvincible && !isDashing && GameManager.Instance != null)
                    {
                        TakeDamage(1);
                    }
                }
            }
        }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Vector2 pushDirection = (transform.position - collision.transform.position).normalized;
            rb.linearVelocity += pushDirection * Time.deltaTime * 3f;
        }
    }

    // ===== COLLECT ORBS =====
    void CollectOrb(GameObject orb)
    {
        if (orbCollectParticles != null)
            Instantiate(orbCollectParticles, transform.position, Quaternion.identity);

        PlaySound(orbCollectSound);

        if (GameManager.Instance != null)
            GameManager.Instance.AddScore(10);

        Destroy(orb);
    }

    // ===== DAMAGE - FULLY FIXED =====
public void TakeDamage(int damage)
{
    // ✅ FIX: Better null check with retry
    if (GameManager.Instance == null)
    {
        Debug.LogWarning("GameManager is null! Attempting to find it...");
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null)
        {
            Debug.Log("GameManager found! Continuing...");
        }
        else
        {
            Debug.LogError("GameManager not found! Cannot take damage.");
            return;
        }
    }
    
    if (isInvincible || isDashing || GameManager.Instance.currentState == GameManager.GameState.GameOver) 
        return;

    GameManager.Instance.TakeDamage(damage);
    TakeDamageVisual();
}

void TakeDamageVisual()
{
    if (!gameObject.activeInHierarchy) return;

    if (hitParticles != null)
        Instantiate(hitParticles, transform.position, Quaternion.identity);

    PlaySound(damageSound);

    isInvincible = true;
    invincibilityTimer = invincibilityTime;

    if (rb != null)
    {
        Vector2 knockback = new Vector2(-moveInput * 3f, 3f);
        rb.linearVelocity = knockback;
    }
}
    // ===== DEATH =====
    public void Die()
    {
        if (deathParticles != null)
            Instantiate(deathParticles, transform.position, Quaternion.identity);

        PlaySound(deathSound);

        gameObject.SetActive(false);
    }

    // ===== RESET =====
    public void ResetPlayer()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = Color.white;
        }
        
        isInvincible = false;
        invincibilityTimer = 0f;
        isDashing = false;
        isChargingJump = false;
        transform.localScale = originalScale;
        
        rb.linearVelocity = Vector2.zero;
        
        gameObject.SetActive(true);
        hasJumped = false;
        airJumpsRemaining = maxAirJumps;
    }

    // ===== UPGRADES =====
    void ApplyUpgrades()
    {
        if (GameManager.Instance == null) return;

        int speedLevel = GameManager.Instance.speedLevel;
        int jumpLevel = GameManager.Instance.jumpLevel;
        int fireRateLevel = GameManager.Instance.fireRateLevel;

        moveSpeed = 5f + (speedLevel * 0.5f);
        jumpForce = 12f + (jumpLevel * 1.5f);
        maxJumpForce = 20f + (jumpLevel * 1.5f);
        shootCooldown = Mathf.Max(0.1f, 0.5f - (fireRateLevel * 0.05f));

        Debug.Log($"Upgrades applied: Speed={moveSpeed}, Jump={jumpForce}, FireRate={shootCooldown}");
    }

    // ===== ANIMATIONS =====
    void UpdateAnimations()
    {
        if (animator == null) return;

        animator.SetBool("isMoving", Mathf.Abs(moveInput) > 0.1f);
        animator.SetBool("isGrounded", isGrounded);
        animator.SetBool("isChargingJump", isChargingJump);
        animator.SetBool("isDashing", isDashing);
        animator.SetFloat("verticalVelocity", rb.linearVelocity.y);
        animator.SetFloat("moveSpeed", Mathf.Abs(rb.linearVelocity.x) / moveSpeed);
    }

    // ===== AUDIO =====
    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    // ===== DEBUG =====
    void OnDrawGizmosSelected()
    {
        if (groundCheckPoint != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
        }

        if (wallCheckPoint != null)
        {
            Gizmos.color = isTouchingWall ? Color.cyan : Color.gray;
            Gizmos.DrawWireSphere(wallCheckPoint.position, wallCheckRadius);
        }

        if (shootPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(shootPoint.position, 0.1f);
        }
    }
}