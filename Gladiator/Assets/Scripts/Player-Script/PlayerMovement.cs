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

    [Header("Roll Settings")]
    public float rollSpeed = 10f;
    public float rollDuration = 0.8f; 
    public float rollCooldown = 1.0f;

    [Header("References")]
    public Animator animator;
    public Transform cameraTransform;
    public PlayerLockOn lockOn;

    //--- FLAGS ---
    [HideInInspector] public bool isAttacking = false;
    [HideInInspector] public bool isRolling = false;

    private CharacterController controller;
    private Vector3 verticalVelocity;
    private float currentSpeed;
    
    //Roll Timers
    private float rollTimer;
    private float lastRollTime;
    private Vector3 fixedRollDirection;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        //Keep the player on the ground and apply gravity every frame
        if (controller.isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f;
        }
        verticalVelocity.y += gravity * Time.deltaTime;

        //Watch for the space bar to start a dodge roll
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryStartRoll();
        }

        //Decide if we are moving normally or currently rolling
        if (isRolling)
        {
            HandleRollingPhysics();
        }
        else
        {
            HandleStandardMovement();
        }
    }

    void TryStartRoll()
    {
        //Roll only if we are not on the ground and not already busy
        if (!controller.isGrounded) { Debug.Log("Can't roll: In Air"); return; }
        if (isAttacking) { Debug.Log("Can't roll: Attacking"); return; }
        if (isRolling) { return; }
        if (Time.time < lastRollTime + rollCooldown) { return; }

        Debug.Log("Starting Roll!"); // Check console for this

        // Start Roll
        isRolling = true;
        rollTimer = rollDuration;
        lastRollTime = Time.time;
        
        //Trigger Animation
        if (animator != null) animator.SetTrigger("Roll");

        //Figure out which way to roll based on the camera and WASD keys
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(h, 0f, v).normalized;

        if (inputDir.magnitude > 0.1f)
        {
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
            Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
            transform.rotation = targetRotation; // Face roll direction instantly
            fixedRollDirection = targetRotation * Vector3.forward;
        }
        else
        {
            fixedRollDirection = transform.forward; // Roll forward if no input
        }
    }

    void HandleRollingPhysics()
    {
        rollTimer -= Time.deltaTime;

        if (rollTimer <= 0)
        {
            //End the roll ocne the timer runs out
            isRolling = false;
            
            //Decide the player's speed right after the roll finishes
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            bool tryingToMove = new Vector3(h, 0, v).magnitude > 0.1f;
            
            if (tryingToMove)
                currentSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
            else
                currentSpeed = 0f;
            
            return;
        }

        //Apply the actual movment and gravity while rolling
        Vector3 finalMove = (fixedRollDirection * rollSpeed) + verticalVelocity;
        controller.Move(finalMove * Time.deltaTime);
    }

    void HandleStandardMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(h, 0f, v).normalized;

        if (isAttacking) input = Vector3.zero;

        bool wantsToMove = input.magnitude > 0.1f;
        bool running = Input.GetKey(KeyCode.LeftShift);

        float targetSpeed = wantsToMove ? (running ? runSpeed : walkSpeed) : 0f;
        float accel = (isAttacking) ? deceleration * 2f : (wantsToMove ? acceleration : deceleration);

        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accel * Time.deltaTime);

        //Rotate the player toward the movment direction or the locked-on enemy
        if (!isAttacking && wantsToMove)
        {
            if (lockOn != null && lockOn.CurrentTarget != null)
            {
                Vector3 toEnemy = lockOn.CurrentTarget.position - transform.position;
                toEnemy.y = 0f;
                if (toEnemy.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toEnemy), rotationSpeed * Time.deltaTime);
            }
            else
            {
                float targetAngle = Mathf.Atan2(input.x, input.z) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0f, targetAngle, 0f), rotationSpeed * Time.deltaTime);
            }
        }

        //Calculate the final movment direction
        Vector3 moveDir = Vector3.zero;
        if (currentSpeed > 0.1f)
        {
            //If we are locked on an enemy, move relative to them
             if (lockOn != null && lockOn.CurrentTarget != null)
             {
                 moveDir = (transform.forward * v + transform.right * h).normalized;
             }
             else
             {
                 //Else just move in the dicretion the player is facing
                 moveDir = transform.forward;
             }
        }

        controller.Move((moveDir * currentSpeed + verticalVelocity) * Time.deltaTime);

        if (animator != null) animator.SetFloat("Speed", currentSpeed);
    }
}