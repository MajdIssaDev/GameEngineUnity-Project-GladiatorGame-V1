using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 4.0f;       
    public float runSpeed = 8.0f;        // Maximum running speed
    public float acceleration = 5.0f;    // Lower this to make the "speed up" take longer
    public float deceleration = 10.0f;   // How fast to stop
    public float turnSmoothTime = 0.1f;  
    public float gravity = -9.81f;

    [Header("References")]
    public Animator animator;
    public Transform cam;

    private CharacterController controller;
    private float turnSmoothVelocity;
    private Vector3 velocity;      
    private float currentSpeed;    // This holds the actual smooth speed (0.0 to 8.0)

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (cam == null) cam = Camera.main.transform;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // 1. Get Input
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;
        
        // Check if Shift is held
        bool isShiftHeld = Input.GetKey(KeyCode.LeftShift);

        // 2. Calculate Target Speed
        // If we are pressing keys:
        //    - If Shift is held -> Target is runSpeed (8)
        //    - If Shift NOT held -> Target is walkSpeed (4)
        // If no keys -> Target is 0
        float targetSpeed = 0f;
        if (direction.magnitude >= 0.1f)
        {
            targetSpeed = isShiftHeld ? runSpeed : walkSpeed;
        }
        
        // 3. Smooth Acceleration (The "Magic" Part)
        // This takes currentSpeed and slowly moves it towards targetSpeed
        // This creates the "start slow and speed up" effect
        if (direction.magnitude >= 0.1f)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.deltaTime);
        }

        // 4. Rotate and Move
        if (currentSpeed > 0.1f)
        {
            // Only rotate if we are actually pressing keys
            if (direction.magnitude >= 0.1f)
            {
                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);
            }

            // Move the character forward based on the smooth currentSpeed
            Vector3 moveDir = transform.forward * currentSpeed;
            controller.Move(moveDir * Time.deltaTime);
        }

        // 5. Gravity
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; 
        }
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // 6. Update Animator
        if (animator != null)
        {
            // Send the exact smooth speed value to the Animator
            // 0 = Idle, 4 = Walk, 8 = Run
            animator.SetFloat("Speed", currentSpeed);
        }
    }
}