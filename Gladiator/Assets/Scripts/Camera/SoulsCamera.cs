using UnityEngine;

public class SoulsCamera : MonoBehaviour
{
    [Header("References")]
    public Transform playerTransform;
    public PlayerLockOn lockOnScript;

    [Header("Settings")]
    public float mouseSensitivity = 2f;
    public float rotationSpeed = 10f;

    [Header("Lock On Smoothness")]
    public float lockSmoothTime = 0.15f; 

    [Header("Positioning & Offset")]
    public float defaultDistance = 4f;
    public float minDistance = 0.5f;
    public float heightOffset = 1.8f;
    public float shoulderOffset = 0.8f; 
    public float verticalAimBias = 0f;

    [Header("Collision")]
    public LayerMask collisionLayers;
    public float collisionRadius = 0.2f;
    public float collisionSmoothTime = 0.1f;

    // Internal state
    private float currentX;
    private float currentY;
    private float currentDistance;
    
    // Smooth Damp Velocities
    private float xVelocity;
    private float yVelocity;
    private float distVelocity;

    void Start()
    {
        if (playerTransform == null) return;

        // Initialize angles to current camera view
        Vector3 angles = transform.eulerAngles;
        currentX = angles.y;
        currentY = NormalizeAngle(angles.x);
        currentDistance = defaultDistance;
    }

    void LateUpdate()
    {
        if (playerTransform == null) return;

        Vector3 focusPoint = playerTransform.position + Vector3.up * heightOffset;
        bool isLocked = lockOnScript != null && lockOnScript.CurrentTarget != null;

        // ---------------------------------------------------------
        // 1. HANDLE ROTATION INPUT & LOGIC
        // ---------------------------------------------------------
        if (isLocked)
        {
            // --- LOCKED MODE ---
            // 1. Identify where we want to look (Enemy + Vertical Bias)
            Vector3 targetCenter = lockOnScript.CurrentTarget.position;
            targetCenter.y += verticalAimBias;

            Vector3 aimDir = (targetCenter - transform.position).normalized;
            Quaternion targetRot = Quaternion.LookRotation(aimDir);
            
            // 2. Smoothly rotate the transform towards that target
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);

            // 3. CRITICAL FIX: Sync the internal variables to the NEW actual rotation
            // This ensures that when we unlock, the "Orbit Logic" starts exactly where we are now.
            Vector3 currentEuler = transform.eulerAngles;
            currentX = currentEuler.y;
            currentY = NormalizeAngle(currentEuler.x);

            // 4. Kill velocity so momentum doesn't "fling" the camera when we unlock
            xVelocity = 0;
            yVelocity = 0;
        }
        else
        {
            // --- FREE MODE ---
            // Standard Orbit Logic
            float targetX = currentX + Input.GetAxis("Mouse X") * mouseSensitivity;
            float targetY = currentY - Input.GetAxis("Mouse Y") * mouseSensitivity;

            // Clamp Vertical Look
            targetY = Mathf.Clamp(targetY, -50, 80);

            // Smooth Damp
            currentX = Mathf.SmoothDampAngle(currentX, targetX, ref xVelocity, 0f); // 0f = Instant mouse response
            currentY = Mathf.SmoothDampAngle(currentY, targetY, ref yVelocity, 0f);

            // Apply rotation from the calculated angles
            transform.rotation = Quaternion.Euler(currentY, currentX, 0);
        }

        // ---------------------------------------------------------
        // 2. CALCULATE POSITION (Always based on currentX/Y)
        // ---------------------------------------------------------
        // Since we synced currentX/Y in the Locked block above, this 
        // position calculation will naturally follow the locked view.
        Quaternion orbitalRotation = Quaternion.Euler(currentY, currentX, 0);

        Vector3 camRight = orbitalRotation * Vector3.right;
        Vector3 camBack = orbitalRotation * Vector3.back;
        
        Vector3 offsetVector = camRight * shoulderOffset;
        Vector3 desiredPos = focusPoint + (camBack * defaultDistance) + offsetVector;

        // ---------------------------------------------------------
        // 3. COLLISION HANDLING
        // ---------------------------------------------------------
        Vector3 castDir = (desiredPos - focusPoint).normalized;
        float castDist = Vector3.Distance(focusPoint, desiredPos);
        RaycastHit hit;
        float targetDist = castDist;

        if (Physics.SphereCast(focusPoint, collisionRadius, castDir, out hit, castDist, collisionLayers))
        {
            targetDist = hit.distance - 0.1f;
        }

        currentDistance = Mathf.SmoothDamp(currentDistance, targetDist, ref distVelocity, collisionSmoothTime);
        if (currentDistance < minDistance) currentDistance = minDistance;

        // Smart Offset Reduction
        float offsetRatio = Mathf.Clamp01(currentDistance / defaultDistance);
        Vector3 finalOffset = offsetVector * offsetRatio;

        transform.position = focusPoint + (camBack * currentDistance) + finalOffset;
    }

    // Convert 0-360 angles to -180 to 180 so clamps work correctly
    private float NormalizeAngle(float angle)
    { 
        angle %= 360;
        if (angle > 180) return angle - 360;
        return angle;
    }
}