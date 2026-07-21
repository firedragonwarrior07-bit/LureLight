using UnityEngine;

public class GroundDetector : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private LayerMask groundLayer = -1; // -1 = Everything
    [SerializeField] private bool useTrigger = true;
    [SerializeField] private float checkRadius = 0.2f;
    [SerializeField] private Transform checkPoint;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    // Public properties
    private bool _isGrounded = false;
    public bool isGrounded 
    { 
        get 
        {
            // If using trigger, return cached value
            if (useTrigger)
                return _isGrounded;
            
            // If using physics check, do it in real-time
            return PhysicsCheck();
        }
        private set => _isGrounded = value;
    }

    public bool IsGrounded => isGrounded;

    private Collider2D currentGround = null;
    private int groundContactCount = 0;

    void Start()
    {
        // If no check point assigned, use this transform
        if (checkPoint == null)
            checkPoint = transform;

        // If ground layer not set, default to "Ground"
        if (groundLayer == -1)
        {
            groundLayer = LayerMask.GetMask("Ground");
        }

        // Initial ground check
        if (useTrigger)
        {
            // Check if already touching ground at start
            Collider2D[] hits = Physics2D.OverlapCircleAll(checkPoint.position, checkRadius, groundLayer);
            isGrounded = hits.Length > 0;
        }
    }

    void Update()
    {
        // If not using trigger, we don't need to do anything in Update
        // The getter does the check in real-time
    }

    // ===== TRIGGER BASED DETECTION =====
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!useTrigger) return;
        if (!IsGroundLayer(other.gameObject.layer)) return;

        groundContactCount++;
        currentGround = other;
        _isGrounded = true;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        // Only needed if OnTriggerEnter might be missed
        if (!useTrigger) return;
        if (!IsGroundLayer(other.gameObject.layer)) return;
        if (_isGrounded) return;

        // Fallback: if we're inside but OnTriggerEnter wasn't called
        groundContactCount = 1;
        currentGround = other;
        _isGrounded = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!useTrigger) return;
        if (!IsGroundLayer(other.gameObject.layer)) return;

        groundContactCount--;
        if (groundContactCount <= 0)
        {
            groundContactCount = 0;
            currentGround = null;
            _isGrounded = false;
        }
    }

    // ===== PHYSICS BASED DETECTION (Alternative) =====
    bool PhysicsCheck()
    {
        if (checkPoint == null) return false;

        Collider2D hit = Physics2D.OverlapCircle(checkPoint.position, checkRadius, groundLayer);
        return hit != null;
    }

    bool IsGroundLayer(int layer)
    {
        return (groundLayer & (1 << layer)) != 0;
    }

    // ===== PUBLIC METHODS =====
    public void ForceUpdate()
    {
        if (useTrigger)
        {
            // Check if currently on ground via physics
            bool wasGrounded = _isGrounded;
            _isGrounded = PhysicsCheck();
            
            if (_isGrounded && !wasGrounded)
                OnTriggerEnter2D(null); // Simulate enter event
            
            if (!_isGrounded && wasGrounded)
                OnTriggerExit2D(null); // Simulate exit event
        }
    }

    public GameObject GetCurrentGround()
    {
        return currentGround != null ? currentGround.gameObject : null;
    }

    // ===== DEBUG =====
    void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        Transform point = checkPoint != null ? checkPoint : transform;
        
        // Draw ground check circle
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(point.position, checkRadius);

        // Draw fill
        Gizmos.color = isGrounded ? new Color(0, 1, 0, 0.2f) : new Color(1, 0, 0, 0.2f);
        Gizmos.DrawSphere(point.position, checkRadius);

        // Draw line from center to ground if grounded
        if (isGrounded && currentGround != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(point.position, currentGround.transform.position);
        }
    }
}