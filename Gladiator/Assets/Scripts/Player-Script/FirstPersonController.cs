using UnityEngine;

public class FirstPersonController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5.0f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.0f;

    [Header("References")]
    public Transform cameraRoot; 
    public Animator animator; // <--- NEW: Drag your Animator here

    [Header("Look")]
    public float mouseSensitivity = 2.0f;
    public float lookXLimit = 85.0f;

    private CharacterController controller;
    private Vector3 velocity;
    private float xRotation = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // Attach Camera
        if (Camera.main != null)
        {
            Camera.main.transform.SetParent(cameraRoot);
            Camera.main.transform.localPosition = Vector3.zero;
            Camera.main.transform.localRotation = Quaternion.identity;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // --- Look Rotation ---
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -lookXLimit, lookXLimit);
        cameraRoot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // --- Movement ---
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * moveSpeed * Time.deltaTime);

        // --- Gravity ---
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; 
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // --- ANIMATION LOGIC (NEW) ---
        if (animator != null)
        {
            // Check if we are trying to move (magnitude > 0)
            bool isWalking = move.magnitude > 0.1f;

            // Update the Animator Bool
            animator.SetBool("Walk", isWalking);
        }
    }
}