using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] public Transform target;
    [SerializeField] private bool autoFindTarget = true;
    [SerializeField] private string targetTag = "Player";

    [Header("Smoothing")]
    [SerializeField] private float smoothSpeed = 0.125f;
    [SerializeField] private float smoothRotation = 0.05f;
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);

    [Header("Dead Zone")]
    [SerializeField] private bool useDeadZone = false;
    [SerializeField] private float deadZoneWidth = 2f;
    [SerializeField] private float deadZoneHeight = 1.5f;

    [Header("Look Ahead")]
    [SerializeField] private bool lookAhead = false;
    [SerializeField] private float lookAheadSpeed = 2f;
    [SerializeField] private float lookAheadDistance = 1f;

    [Header("Bounds")]
    [SerializeField] private bool clampToBounds = false;
    [SerializeField] private Bounds levelBounds;
    [SerializeField] private float boundsPadding = 0.5f;

    [Header("Zoom")]
    [SerializeField] private bool enableZoom = false;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 15f;
    [SerializeField] private float defaultZoom = 10f;
    [SerializeField] private float zoomSpeed = 1f;

    [Header("Shake")]
    [SerializeField] private bool enableShake = true;
    [SerializeField] private float shakeIntensity = 0.5f;
    [SerializeField] private float shakeDuration = 0.3f;

    // Private variables
    private Camera cam;
    private Vector3 velocity = Vector3.zero;
    private Vector3 shakeOffset = Vector3.zero;
    private float shakeTimer = 0f;
    private float currentZoom;
    private float targetZoom;
    private Vector2 deadZoneCenter;

    void Start()
    {
        // Get camera component
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            currentZoom = cam.orthographicSize;
            targetZoom = currentZoom;
        }

        // Auto-find target if not assigned
        if (target == null && autoFindTarget)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag(targetTag);
            if (playerObj != null)
                target = playerObj.transform;
        }

        // Set initial dead zone center
        if (target != null)
            deadZoneCenter = target.position;

        // Set initial zoom
        if (enableZoom && cam != null)
        {
            cam.orthographicSize = defaultZoom;
            currentZoom = defaultZoom;
            targetZoom = defaultZoom;
        }
    }

    void LateUpdate()
    {
        // Auto-find target if missing
        if (target == null && autoFindTarget)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag(targetTag);
            if (playerObj != null)
                target = playerObj.transform;
            else
                return;
        }

        if (target == null) return;

        // Update shake
        UpdateShake();

        // Calculate desired position
        Vector3 desiredPosition = GetDesiredPosition();

        // Apply smoothing
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        // Apply shake offset
        transform.position += shakeOffset;

        // Clamp to bounds
        if (clampToBounds)
            ClampToBounds();

        // Handle zoom
        if (enableZoom && cam != null)
        {
            currentZoom = Mathf.Lerp(currentZoom, targetZoom, Time.deltaTime * zoomSpeed);
            cam.orthographicSize = currentZoom;
        }
    }

    Vector3 GetDesiredPosition()
    {
        if (target == null) return transform.position;

        Vector3 targetPos = target.position;

        // Dead zone
        if (useDeadZone)
        {
            Vector2 diff = targetPos - (Vector3)deadZoneCenter;
            if (Mathf.Abs(diff.x) > deadZoneWidth)
                deadZoneCenter.x += Mathf.Sign(diff.x) * (Mathf.Abs(diff.x) - deadZoneWidth);
            
            if (Mathf.Abs(diff.y) > deadZoneHeight)
                deadZoneCenter.y += Mathf.Sign(diff.y) * (Mathf.Abs(diff.y) - deadZoneHeight);
            
            targetPos = deadZoneCenter;
        }

        // Look ahead
        if (lookAhead)
        {
            Rigidbody2D rb = target.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 velocity = rb.linearVelocity;
                float lookAmount = Mathf.Clamp(velocity.magnitude / lookAheadSpeed, 0, 1);
                Vector3 lookOffset = (Vector3)velocity.normalized * lookAheadDistance * lookAmount;
                targetPos += lookOffset;
            }
        }

        return targetPos + offset;
    }

    void UpdateShake()
    {
        if (!enableShake) return;

        if (shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime;
            float intensity = shakeIntensity * (shakeTimer / shakeDuration);
            shakeOffset = Random.insideUnitSphere * intensity;
            shakeOffset.z = 0;
        }
        else
        {
            shakeOffset = Vector3.zero;
        }
    }

    void ClampToBounds()
    {
        if (!clampToBounds) return;

        Vector3 pos = transform.position;
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        pos.x = Mathf.Clamp(pos.x, levelBounds.min.x + halfWidth + boundsPadding, levelBounds.max.x - halfWidth - boundsPadding);
        pos.y = Mathf.Clamp(pos.y, levelBounds.min.y + halfHeight + boundsPadding, levelBounds.max.y - halfHeight - boundsPadding);

        transform.position = pos;
    }

    // ===== PUBLIC METHODS =====

    /// <summary>
    /// Set the camera target
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
            deadZoneCenter = target.position;
    }

    /// <summary>
    /// Trigger screen shake
    /// </summary>
    public void Shake(float intensity = -1f, float duration = -1f)
    {
        if (!enableShake) return;
        
        shakeTimer = duration > 0 ? duration : shakeDuration;
        shakeIntensity = intensity > 0 ? intensity : shakeIntensity;
    }

    /// <summary>
    /// Set camera zoom
    /// </summary>
    public void SetZoom(float zoom)
    {
        if (!enableZoom) return;
        targetZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
    }

    /// <summary>
    /// Reset zoom to default
    /// </summary>
    public void ResetZoom()
    {
        if (!enableZoom) return;
        targetZoom = defaultZoom;
    }

    /// <summary>
    /// Set level bounds for clamping
    /// </summary>
    public void SetBounds(Bounds bounds)
    {
        levelBounds = bounds;
        clampToBounds = true;
    }

    /// <summary>
    /// Set dead zone center
    /// </summary>
    public void SetDeadZoneCenter(Vector2 center)
    {
        deadZoneCenter = center;
    }

    // ===== DEBUG =====
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
        {
            // Find target if not assigned
            if (target == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                    target = playerObj.transform;
            }
        }

        // Dead zone
        if (useDeadZone && target != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 center = Application.isPlaying ? (Vector3)deadZoneCenter : target.position;
            Gizmos.DrawWireCube(center, new Vector3(deadZoneWidth * 2, deadZoneHeight * 2, 0));
        }

        // Level bounds
        if (clampToBounds)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(levelBounds.center, levelBounds.size);
        }

        // Camera view
        if (cam != null)
        {
            Gizmos.color = Color.green;
            float height = cam.orthographicSize * 2;
            float width = height * cam.aspect;
            Gizmos.DrawWireCube(transform.position, new Vector3(width, height, 0));
        }
    }
}