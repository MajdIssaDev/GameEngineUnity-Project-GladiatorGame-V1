using System;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [Header("Continuous Inputs (Axes)")]
    //Use private setters so other scripts can read the input values but can't accidentally overwrite them
    public Vector2 MovementInput { get; private set; }
    public Vector2 RawMovementInput { get; private set; }
    public Vector2 MouseInput { get; private set; }

    [Header("Button States (Held)")]
    public bool IsRunning { get; private set; }
    public bool IsBlocking { get; private set; }
    public bool IsHeavyModifierHeld { get; private set; }

    //Broadcast discrete button presses as events so decoupled systems can react
    //without polling Input.GetKeyDown in their own Update loops
    public event Action OnRollPressed;      //Mapped to Space
    public event Action OnAttackPressed;    //Mapped to Fire1
    public event Action OnLockOnPressed;    //Mapped to Q
    public event Action OnPausePressed;     //Mapped to Escape

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        //Read both smoothed axes and raw axes
        MovementInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        RawMovementInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        MouseInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

        //2. Read Held Buttons
        IsRunning = Input.GetKey(KeyCode.LeftShift);
        IsBlocking = Input.GetKey(KeyCode.E);
        IsHeavyModifierHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        //Safely invoke events using the null-conditional operator to ensure we don't throw a
        //NullReferenceException if no scripts are currently listening
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