using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerLocomotion : MonoBehaviour
{
    [Header("References")]
    public Transform playerTransform;
    public Transform cameraTransform;
    public PlayerLockOn lockOnScript;
    public Animator animator;
    public CharacterController characterController;

    [Header("Movement Settings")]
    public float walkSpeed = 4f;
    public float runSpeed = 8f;
    public float rotationSpeed = 10f;
    public float gravity = -9.81f;

    [Header("Lock-On Modifiers")]
    [Tooltip("Multiplier for walking backwards while locked. 0.5 = 50% speed.")]
    [Range(0.1f, 1f)]
    public float lockedBackwardsSpeedPenalty = 0.5f; 

    [Header("Damping")]
    public float acceleration = 12f;
    public float deceleration = 16f;

    // --- STATE FLAGS ---
    [HideInInspector] public bool isAttacking = false; 

    // Internal
    private Vector3 verticalVelocity;
    private float currentSpeed;

    void Awake()
    {
        if (characterController == null) characterController = GetComponent<CharacterController>();
        if (playerTransform == null) playerTransform = transform;
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        bool isLocked = lockOnScript != null && lockOnScript.CurrentTarget != null;

        // ---------------------------------------------------------
        // 1. INPUT
        // ---------------------------------------------------------
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 inputDir = new Vector3(h, 0, v).normalized;

        // Force input to zero if attacking (stops walking)
        if (isAttacking) 
        {
            inputDir = Vector3.zero;
            h = 0;
            v = 0;
        }

        bool wantsToMove = inputDir.magnitude > 0.1f;
        
        // ---------------------------------------------------------
        // 2. CALCULATE TARGET SPEED (UPDATED)
        // ---------------------------------------------------------
        float finalTargetSpeed = 0f;

        if (wantsToMove)
        {
            if (isLocked)
            {
                // --- LOCKED MODE RULES ---
                // 1. Cannot Run (Ignore Shift)
                finalTargetSpeed = walkSpeed;

                // 2. Walking Backwards Penalty
                // If 'v' is negative, we are pulling back on the stick/S key
                if (v < -0.1f)
                {
                    finalTargetSpeed *= lockedBackwardsSpeedPenalty; 
                }
            }
            else
            {
                // --- FREE MODE RULES ---
                // Standard logic: Shift = Run
                bool isRunning = Input.GetKey(KeyCode.LeftShift);
                finalTargetSpeed = isRunning ? runSpeed : walkSpeed;
            }
        }

        // Use fast deceleration if attacking to prevent sliding
        float accelRate = (wantsToMove && !isAttacking) ? acceleration : deceleration * 2f;

        currentSpeed = Mathf.MoveTowards(currentSpeed, finalTargetSpeed, accelRate * Time.deltaTime);

        // Update Animator Strafe State
        if (animator != null) animator.SetBool("isStrafing", isLocked);

        // ---------------------------------------------------------
        // 3. MOVEMENT & ROTATION LOGIC
        // ---------------------------------------------------------
        Vector3 moveDirection = Vector3.zero;

        if (!isAttacking)
        {
            if (isLocked)
            {
                // --- LOCKED MODE (STRAFING) ---
                
                // Rotation: Face the enemy
                Vector3 targetDir = lockOnScript.CurrentTarget.position - playerTransform.position;
                targetDir.y = 0;
                if (targetDir != Vector3.zero)
                {
                    Quaternion lookRot = Quaternion.LookRotation(targetDir);
                    playerTransform.rotation = Quaternion.Slerp(playerTransform.rotation, lookRot, rotationSpeed * Time.deltaTime);
                }

                // Movement: Relative to Camera
                Vector3 camFwd = cameraTransform.forward;
                Vector3 camRight = cameraTransform.right;
                camFwd.y = 0; camRight.y = 0;
                camFwd.Normalize(); camRight.Normalize();

                moveDirection = (camFwd * v + camRight * h).normalized;
            }
            else
            {
                // --- FREE MODE (STANDARD) ---
                if (wantsToMove)
                {
                    float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
                    Quaternion targetRot = Quaternion.Euler(0, targetAngle, 0);
                    playerTransform.rotation = Quaternion.Slerp(playerTransform.rotation, targetRot, rotationSpeed * Time.deltaTime);

                    moveDirection = targetRot * Vector3.forward;
                }
            }
        }

        // Apply Speed
        Vector3 finalMove = moveDirection * currentSpeed;

        // ---------------------------------------------------------
        // 4. GRAVITY
        // ---------------------------------------------------------
        if (characterController.isGrounded)
        {
            if (verticalVelocity.y < 0f) verticalVelocity.y = -2f;
        }
        verticalVelocity.y += gravity * Time.deltaTime;

        // ---------------------------------------------------------
        // 5. APPLY FINAL MOVE
        // ---------------------------------------------------------
        characterController.Move((finalMove + verticalVelocity) * Time.deltaTime);

        // ---------------------------------------------------------
        // 6. ANIMATOR UPDATES
        // ---------------------------------------------------------
        if (animator != null)
        {
            if (isLocked)
            {
                // Send raw Input values for Strafe Blend Tree
                animator.SetFloat("InputX", h, 0.1f, Time.deltaTime);
                animator.SetFloat("InputY", v, 0.1f, Time.deltaTime);
            }
            else
            {
                // Send Speed for Free Move Blend Tree
                animator.SetFloat("Speed", currentSpeed);
                // Reset strafe params safely
                animator.SetFloat("InputX", 0);
                animator.SetFloat("InputY", 0);
            }
        }
    }
}