using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 4f;
    public float runSpeed = 8f;
    public float acceleration = 12f;
    public float deceleration = 16f;
    public float rotationSpeed = 10f;
    public float gravity = -9.81f;

    [Header("References")]
    public Animator animator;
    public Transform cameraTransform;
    public PlayerLockOn lockOn;

    // --- NEW: Flag to stop movement ---
    [HideInInspector] 
    public bool isAttacking = false; 

    private CharacterController controller;
    private Vector3 verticalVelocity;
    private float currentSpeed;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnEnable()
    {
        currentSpeed = 0f;
        verticalVelocity = Vector3.zero;
        isAttacking = false;
    }

    void Update()
    {
        // --------------------------------------------------
        // 1. INPUT & STATE CHECK
        // --------------------------------------------------
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(h, 0f, v).normalized;

        // If we are attacking, force input to zero so we don't move
        if (isAttacking) 
        {
            input = Vector3.zero;
        }

        bool wantsToMove = input.magnitude > 0.1f;
        bool running = Input.GetKey(KeyCode.LeftShift);

        float targetSpeed = wantsToMove
            ? (running ? runSpeed : walkSpeed)
            : 0f;

        // --------------------------------------------------
        // 2. SPEED SMOOTHING
        // --------------------------------------------------
        float accel = wantsToMove ? acceleration : deceleration;
        
        // If attacking, stop instantly or decelerate fast? 
        // Let's use normal deceleration so you don't "slide" on attack start
        if (isAttacking) accel = deceleration * 2f; 

        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            targetSpeed,
            accel * Time.deltaTime
        );

        // --------------------------------------------------
        // 3. ROTATION (Disable rotation if attacking!)
        // --------------------------------------------------
        if (!isAttacking)
        {
            if (lockOn != null && lockOn.CurrentTarget != null)
            {
                // LOCK-ON ROTATION (face enemy)
                Vector3 toEnemy = lockOn.CurrentTarget.position - transform.position;
                toEnemy.y = 0f;

                if (toEnemy.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(toEnemy);
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        targetRot,
                        rotationSpeed * Time.deltaTime
                    );
                }
            }
            else if (wantsToMove)
            {
                // FREE ROTATION (camera-relative)
                float targetAngle = Mathf.Atan2(input.x, input.z) * Mathf.Rad2Deg
                                    + cameraTransform.eulerAngles.y;

                Quaternion targetRot = Quaternion.Euler(0f, targetAngle, 0f);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRot,
                    rotationSpeed * Time.deltaTime
                );
            }
        }

        // --------------------------------------------------
        // 4. MOVEMENT VECTOR
        // --------------------------------------------------
        Vector3 move = Vector3.zero;

        // Only calculate movement direction if we actually have speed
        if (currentSpeed > 0.1f)
        {
            if (lockOn != null && lockOn.CurrentTarget != null)
            {
                // STRAFING
                Vector3 forward = transform.forward * v;
                Vector3 right = transform.right * h;
                move = (forward + right).normalized;
            }
            else
            {
                // FREE MOVE
                move = transform.forward;
            }
        }

        move *= currentSpeed;

        // --------------------------------------------------
        // 5. GRAVITY (Always applies, even when attacking)
        // --------------------------------------------------
        if (controller.isGrounded)
        {
            if (verticalVelocity.y < 0f)
                verticalVelocity.y = -2f;
        }

        verticalVelocity.y += gravity * Time.deltaTime;

        // --------------------------------------------------
        // 6. APPLY MOVEMENT
        // --------------------------------------------------
        controller.Move((move + verticalVelocity) * Time.deltaTime);

        // --------------------------------------------------
        // 7. ANIMATION
        // --------------------------------------------------
        if (animator != null)
        {
            animator.SetFloat("Speed", currentSpeed);
        }
    }
}