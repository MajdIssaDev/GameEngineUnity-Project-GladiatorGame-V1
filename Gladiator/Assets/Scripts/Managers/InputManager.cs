using System;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [Header("Continuous Inputs (Axes)")]
    public Vector2 MovementInput { get; private set; }
    public Vector2 RawMovementInput { get; private set; }
    public Vector2 MouseInput { get; private set; }

    [Header("Button States (Held)")]
    public bool IsRunning { get; private set; }
    public bool IsBlocking { get; private set; }
    public bool IsHeavyModifierHeld { get; private set; }

    // --- Discrete Actions (Events) ---
    public event Action OnRollPressed;      // Mapped to Space
    public event Action OnAttackPressed;    // Mapped to Fire1
    public event Action OnLockOnPressed;    // Mapped to Q
    public event Action OnPausePressed;     // Mapped to Escape

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            // Optional: DontDestroyOnLoad(gameObject); if you want it to persist across scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        // 1. Read Axes (Continuous movement/looking)
        MovementInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        RawMovementInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        MouseInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

        // 2. Read Held Buttons
        IsRunning = Input.GetKey(KeyCode.LeftShift);
        IsBlocking = Input.GetKey(KeyCode.E);
        IsHeavyModifierHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        // 3. Read Discrete Presses (Fire Events)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnRollPressed?.Invoke();
        }

        if (Input.GetButtonDown("Fire1"))
        {
            OnAttackPressed?.Invoke();
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            OnLockOnPressed?.Invoke();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnPausePressed?.Invoke();
        }
    }
}