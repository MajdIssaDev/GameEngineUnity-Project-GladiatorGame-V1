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

        //Initialize angles to current camera view
        Vector3 angles = transform.eulerAngles;
        currentX = angles.y;
        currentY = NormalizeAngle(angles.x);
        currentDistance = defaultDistance;
    }

    void LateUpdate()
    {
        if (playerTransform == null) return;
        
        //Stop camera calculations if the game is paused so the player can't move the camera around while in menus
        if (Time.deltaTime <= float.Epsilon || Time.timeScale == 0f) 
        {
            return; 
        }

        Vector3 focusPoint = playerTransform.position + Vector3.up * heightOffset;
        bool isLocked = lockOnScript != null && lockOnScript.CurrentTarget != null;
        
        //1. HANDLE ROTATION INPUT & LOGIC
        if (isLocked)
        {
            //--- LOCKED MODE ---
            Vector3 targetCenter = lockOnScript.CurrentTarget.position;
            targetCenter.y += verticalAimBias;

            Vector3 aimDir = (targetCenter - transform.position).normalized;
            Quaternion targetRot = Quaternion.LookRotation(aimDir);
            
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);

            Vector3 currentEuler = transform.eulerAngles;
            currentX = currentEuler.y;
            currentY = NormalizeAngle(currentEuler.x);

            xVelocity = 0;
            yVelocity = 0;
        }
        else
        {
            //--- FREE MODE ---
            
            //Fetch mouse movement from our centralized InputManager to keep inputs decoupled from the camera logic
            float mouseX = InputManager.Instance != null ? InputManager.Instance.MouseInput.x : 0f;
            float mouseY = InputManager.Instance != null ? InputManager.Instance.MouseInput.y : 0f;

            float targetX = currentX + mouseX * mouseSensitivity;
            float targetY = currentY - mouseY * mouseSensitivity;

            //Clamp Vertical Look
            targetY = Mathf.Clamp(targetY, -50, 80);

            //Smooth Damp
            currentX = Mathf.SmoothDampAngle(currentX, targetX, ref xVelocity, 0f); 
            currentY = Mathf.SmoothDampAngle(currentY, targetY, ref yVelocity, 0f);

            //Apply rotation from the calculated angles
            transform.rotation = Quaternion.Euler(currentY, currentX, 0);
        }


        //2. CALCULATE POSITION (Always based on currentX/Y)
        Quaternion orbitalRotation = Quaternion.Euler(currentY, currentX, 0);

        Vector3 camRight = orbitalRotation * Vector3.right;
        Vector3 camBack = orbitalRotation * Vector3.back;
        
        Vector3 offsetVector = camRight * shoulderOffset;
        Vector3 desiredPos = focusPoint + (camBack * defaultDistance) + offsetVector;
        
        //3. COLLISION HANDLING
        
        //Use a SphereCast to detect walls between the player and the camera, pulling the camera closer to prevent clipping through geometry
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

        //Smart Offset Reduction
        float offsetRatio = Mathf.Clamp01(currentDistance / defaultDistance);
        Vector3 finalOffset = offsetVector * offsetRatio;

        transform.position = focusPoint + (camBack * currentDistance) + finalOffset;
    }

    private float NormalizeAngle(float angle)
    { 
        angle %= 360;
        if (angle > 180) return angle - 360;
        return angle;
    }
    
    public void SetPlayerTarget(GameObject newPlayer)
    {
        playerTransform = newPlayer.transform;
        lockOnScript = newPlayer.GetComponent<PlayerLockOn>();

        if (playerTransform != null)
        {
            currentX = playerTransform.eulerAngles.y;
            currentY = 20f; 
        }
    }
}