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

    [Header("Roll Settings")]
    public float rollDuration = 0.8f; 
    public float rollCooldown = 1.0f;
    
    [Header("Capsule Settings")]
    public float normalHeight = 2.0f;
    public Vector3 normalCenter = new Vector3(0, 1.0f, 0);
    public float rollHeight = 1.0f;
    public Vector3 rollCenter = new Vector3(0, 0.5f, 0);

    [Header("Lock-On Modifiers")]
    [Range(0.1f, 1f)]
    public float lockedBackwardsSpeedPenalty = 0.5f; 

    [Header("Damping")]
    public float acceleration = 12f;
    public float deceleration = 16f;

    [HideInInspector] public bool isAttacking = false; 
    [HideInInspector] public bool isRolling = false;

    private Vector3 verticalVelocity;
    private float currentSpeed;
    private float rollTimer;
    private float lastRollTime = -10f;

    void Awake()
    {
        if (characterController == null) characterController = GetComponent<CharacterController>();
        if (playerTransform == null) playerTransform = transform;
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        if (lockOnScript == null) lockOnScript = GetComponent<PlayerLockOn>();
        if (animator != null) animator.applyRootMotion = false;
        
        normalHeight = characterController.height;
        normalCenter = characterController.center;
    }

    void Update()
    {
        // 1. GRAVITY
        // Only calculate gravity if we are NOT attacking.
        // During attacks, we trust the animation's Y movement completely.
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null && cc.enabled == false) return;
        if (!isAttacking)
        {
            if (characterController.isGrounded && verticalVelocity.y < 0)
            {
                verticalVelocity.y = -2f; // Stick to ground
            }
            verticalVelocity.y += gravity * Time.deltaTime;
        }
        else
        {
            // Reset velocity during attacks so it doesn't build up a massive 
            // downward force that hits immediately after the attack ends.
            verticalVelocity.y = 0;
        }

        // 2. INPUT
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryStartRoll();
        }

        // 3. ROOT MOTION TOGGLE
        if (animator != null)
        {
            bool shouldUseRootMotion = isRolling || isAttacking;
            if (animator.applyRootMotion != shouldUseRootMotion)
            {
                animator.applyRootMotion = shouldUseRootMotion;
            }
        }

        // 4. STATE LOGIC
        if (isRolling)
        {
            HandleRollingState();
        }
        else if (isAttacking)
        {
            HandleAttackState(); 
        }
        else
        {
            HandleStandardMovement();
        }
    }

    void TryStartRoll()
    {
        if (isAttacking) return;
        if (isRolling) return;
        if (Time.time < lastRollTime + rollCooldown) return;

        isRolling = true;
        rollTimer = rollDuration;
        lastRollTime = Time.time;
        
        characterController.height = rollHeight;
        characterController.center = rollCenter;

        if (animator != null) animator.SetTrigger("Roll");

        SnapRotationToInput();
    }

    void SnapRotationToInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(h, 0f, v).normalized;

        if (inputDir.magnitude > 0.1f)
        {
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
            playerTransform.rotation = Quaternion.Euler(0f, targetAngle, 0f);
        }
    }

    void HandleRollingState()
    {
        rollTimer -= Time.deltaTime;

        if (rollTimer <= 0)
        {
            isRolling = false;
            characterController.height = normalHeight;
            characterController.center = normalCenter;
            
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            currentSpeed = (new Vector3(h, 0, v).magnitude > 0.1f) ? walkSpeed : 0f;
        }
    }

    void HandleAttackState()
    {
        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
            animator.SetFloat("InputX", 0f);
            animator.SetFloat("InputY", 0f);
        }
    }

    void OnAnimatorMove()
    {
        if ((isRolling || isAttacking) && animator != null)
        {
            // deltaPosition is the "Change in position" for this frame derived from the animation
            Vector3 velocity = animator.deltaPosition;
            
            // --- FIX ---
            // Only apply manual gravity if we are ROLLING. 
            // If we are ATTACKING, we assume the animation handles the Y-axis (Jump attacks).
            // Adding gravity to a Jump Attack animation will cancel out the jump.
            if (!isAttacking) 
            {
                velocity.y += verticalVelocity.y * Time.deltaTime; 
            }
            
            characterController.Move(velocity);
            transform.rotation = animator.rootRotation;
        }
    }

    void HandleStandardMovement()
    {
        bool isLocked = lockOnScript != null && lockOnScript.CurrentTarget != null;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 inputDir = new Vector3(h, 0, v).normalized;
        bool wantsToMove = inputDir.magnitude > 0.1f;
        
        float finalTargetSpeed = 0f;
        if (wantsToMove)
        {
            if (isLocked)
            {
                finalTargetSpeed = walkSpeed;
                if (v < -0.1f) finalTargetSpeed *= lockedBackwardsSpeedPenalty; 
            }
            else
            {
                finalTargetSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
            }
        }

        currentSpeed = Mathf.MoveTowards(currentSpeed, finalTargetSpeed, acceleration * Time.deltaTime);

        Vector3 moveDirection = Vector3.zero;

        if (isLocked)
        {
            Vector3 targetDir = lockOnScript.CurrentTarget.position - playerTransform.position;
            targetDir.y = 0;
            if (targetDir != Vector3.zero) 
                playerTransform.rotation = Quaternion.Slerp(playerTransform.rotation, Quaternion.LookRotation(targetDir), rotationSpeed * Time.deltaTime);

            Vector3 camFwd = cameraTransform.forward; 
            Vector3 camRight = cameraTransform.right;
            camFwd.y = 0; camRight.y = 0; camFwd.Normalize(); camRight.Normalize();
            moveDirection = (camFwd * v + camRight * h).normalized;
        }
        else
        {
            if (wantsToMove)
            {
                float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
                Quaternion targetRot = Quaternion.Euler(0, targetAngle, 0);
                playerTransform.rotation = Quaternion.Slerp(playerTransform.rotation, targetRot, rotationSpeed * Time.deltaTime);
                moveDirection = targetRot * Vector3.forward;
            }
        }

        Vector3 finalMove = (moveDirection * currentSpeed) + verticalVelocity;
        characterController.Move(finalMove * Time.deltaTime);

        if (animator != null)
        {
            animator.SetBool("isStrafing", isLocked);
            if (isLocked)
            {
                animator.SetFloat("InputX", h, 0.1f, Time.deltaTime);
                animator.SetFloat("InputY", v, 0.1f, Time.deltaTime);
            }
            else
            {
                animator.SetFloat("Speed", currentSpeed);
                animator.SetFloat("InputX", 0);
                animator.SetFloat("InputY", 0);
            }
        }
    }
}