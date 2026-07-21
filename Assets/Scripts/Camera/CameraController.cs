using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour
{
    [Header("Follow Settings")]
    public Transform target;
    public float smoothSpeed = 0.125f;
    public Vector3 offset = new Vector3(0, 0, -10);

    [Header("Shake Settings")]
    public float shakeDuration = 0.2f;
    public float shakeMagnitude = 0.3f;
    private float currentShakeDuration = 0f;
    private Vector3 originalPos;

    [Header("Bob Settings")]
    public float bobSpeed = 2f;
    public float bobAmount = 0.05f;
    private float bobTimer = 0f;

    [Header("Hit-Stop Settings")]
    public float hitStopDuration = 0.05f;
    private bool isHitStopped = false;

    [Header("Zoom Settings")]
    public float defaultZoom = 10f;
    public float runZoom = 12f;
    public float zoomSpeed = 2f;
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        originalPos = transform.position;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // ----- 1. CAMERA FOLLOW -----
        Vector3 desiredPos = target.position + offset;
        Vector3 smoothedPos = Vector3.Lerp(transform.position, desiredPos, smoothSpeed);
        transform.position = smoothedPos;

        // ----- 2. DYNAMIC ZOOM (run faster = zoom out) -----
        if (cam != null)
        {
            float speed = target.GetComponent<Rigidbody2D>().linearVelocity.magnitude;
            float targetZoom = Mathf.Lerp(defaultZoom, runZoom, speed / 8f);
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, zoomSpeed * Time.deltaTime);
        }

        // ----- 3. WALKING BOB -----
        if (Mathf.Abs(target.GetComponent<Rigidbody2D>().linearVelocity.x) > 0.5f)
        {
            bobTimer += Time.deltaTime * bobSpeed;
            float bobOffset = Mathf.Sin(bobTimer) * bobAmount;
            transform.position += new Vector3(0, bobOffset, 0);
        }
        else
        {
            bobTimer = 0f;
        }

        // ----- 4. SHAKE -----
        if (currentShakeDuration > 0f)
        {
            Vector3 shakeOffset = Random.insideUnitSphere * shakeMagnitude;
            shakeOffset.z = 0;
            transform.position += shakeOffset;
            currentShakeDuration -= Time.deltaTime;
        }
    }

    // ----- PUBLIC METHODS FOR FEEDBACK -----

    public void TriggerShake(float duration, float magnitude)
    {
        currentShakeDuration = duration;
        shakeMagnitude = magnitude;
    }

    public void TriggerHitStop(float duration)
    {
        if (!isHitStopped)
        {
            StartCoroutine(HitStopCoroutine(duration));
        }
    }

    IEnumerator HitStopCoroutine(float duration)
    {
        isHitStopped = true;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
        isHitStopped = false;
    }

    public void TriggerLandBump()
    {
        StartCoroutine(LandBumpCoroutine());
    }

    IEnumerator LandBumpCoroutine()
    {
        float bump = -0.1f;
        transform.position += new Vector3(0, bump, 0);
        yield return new WaitForSeconds(0.05f);
        transform.position -= new Vector3(0, bump, 0);
    }
}