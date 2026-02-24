using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class PlayerCombat : MonoBehaviour, ICombatReceiver
{
    [Header("References")]
    public Animator animator;
    public PlayerLocomotion movementScript;
    [SerializeField] Stats stats;
    
    private CombatAnimationEvents animationEvents;
    private RuntimeAnimatorController baseController;
    private WeaponDamage currentWeaponScript; 

    //States
    public bool isAttacking { get; private set; }
    public bool isStunned { get; private set; } = false;
    public bool isHeavyAttacking { get; private set; } 
    private bool canCombo = false;      
    private bool inputQueued = false;   

    //Logic Variables
    private int comboStep = 0;
    private float lastAttackTime = 0f;
    private float maxAttackDuration = 5.0f; 
    private float nextAttackTime = 0f;      
    
    [Header("Block Settings")]
    public float blockCooldown = 0.5f; 
    public float blockStartupTime = 0.1f; 
    
    private float nextBlockTime = 0f;
    private Coroutine activeBlockCoroutine; 
    
    private bool requireNewBlockPress = false; 
    
    [Header("Energy Settings")] 
    public float lightAttackCost = 10f;
    public float heavyAttackCost = 25f;
    
    private HealthScript healthScript;
    private Coroutine activeStunCoroutine;

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (stats == null) stats = GetComponent<Stats>();
        healthScript = GetComponent<HealthScript>();
        
        animationEvents = animator.GetComponent<CombatAnimationEvents>();
        baseController = animator.runtimeAnimatorController; 
        isAttacking = false;
    }

    //--- Subscribe to the Attack event ---
    void OnEnable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnAttackPressed += HandleAttackInput;
        }
    }

    //--- Unsubscribe from the Attack event ---
    void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnAttackPressed -= HandleAttackInput;
        }
    }

    public void EquipNewWeapon(GameObject newWeaponObject, AnimatorOverrideController overrideController)
    {
        currentWeaponScript = newWeaponObject.GetComponent<WeaponDamage>();

        if (animationEvents != null) animationEvents.SetCurrentWeapon(currentWeaponScript);

        if (animator == null) animator = GetComponent<Animator>(); 
        if (baseController == null) baseController = animator.runtimeAnimatorController;

        if (overrideController != null) animator.runtimeAnimatorController = overrideController;
        else if (baseController != null) animator.runtimeAnimatorController = baseController;

        ResetCombatState();
    }
    
    void Update()
    {
        //Don't process combat inputs if clicking on UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        
        if (isAttacking && Time.time > lastAttackTime + maxAttackDuration) OnFinishAttack();
        
        //--- 1. TRACK KEY RELEASES ALWAYS ---
        //--- Using InputManager for continuous hold checks ---
        bool isHoldingBlock = InputManager.Instance != null && InputManager.Instance.IsBlocking;
        
        if (!isHoldingBlock)
        {
            requireNewBlockPress = false;
        }

        //--- 2. IRONCLAD STUN CHECK ---
        if (isStunned) 
        {
            //Ruthlessly enforce the movement lock every single frame we are stunned
            if (movementScript != null) movementScript.isStunned = true;
            return; //EXIT HERE. Impossible for block release to do anything else.
        }
        
        //--- 3. BLOCKING LOGIC ---
        if (healthScript != null)
        {
            bool isBlockingAnim = animator.GetBool("Blocking");

            if (isHoldingBlock && !isAttacking && !requireNewBlockPress)
            {
                if (!isBlockingAnim && Time.time >= nextBlockTime)
                {
                    animator.SetBool("Blocking", true);
                    if (movementScript != null) movementScript.isAttacking = true; 
                    
                    if (activeBlockCoroutine != null) StopCoroutine(activeBlockCoroutine);
                    activeBlockCoroutine = StartCoroutine(EnableBlockRoutine());
                }
            }
            else if (!isHoldingBlock && isBlockingAnim) 
            {
                ForceStopBlocking();
                nextBlockTime = Time.time + blockCooldown;
            }
        }
        
        if (healthScript != null && healthScript.IsBlocking) return;
        
        //--- 4. QUEUED ATTACK LOGIC ---
        //(The actual attack triggering is now handled in HandleAttackInput)
        if (inputQueued && canCombo)
        {
            if (healthScript != null && healthScript.TrySpendEnergy(lightAttackCost)) PerformComboStep();
            else inputQueued = false; 
        }
    }

    //--- This method fires ONLY when the left mouse button is clicked ---
    private void HandleAttackInput()
    {
        //Ignore attacks if clicking on UI, blocking, or stunned
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (healthScript != null && healthScript.IsBlocking) return;
        if (isStunned) return;

        //--- Ask InputManager if the modifier is held down ---
        bool isHeavyModifierDown = InputManager.Instance != null && InputManager.Instance.IsHeavyModifierHeld;

        if (isHeavyModifierDown)
        {
            if (!isAttacking && Time.time >= nextAttackTime) 
            {
                if (healthScript != null && healthScript.TrySpendEnergy(heavyAttackCost)) PerformHeavyAttack();
                else Debug.Log("Not enough energy for Heavy Attack!");
            }
        }
        else
        {
            if (isAttacking)
            {
                if (!isHeavyAttacking) inputQueued = true;
            }
            else if (Time.time >= nextAttackTime)
            {
                if (healthScript != null && healthScript.TrySpendEnergy(lightAttackCost))
                {
                    comboStep = 0;
                    PerformComboStep();
                }
                else Debug.Log("Out of Energy!");
            }
        }
    }

    private void ForceStopBlocking()
    {
        if (activeBlockCoroutine != null) StopCoroutine(activeBlockCoroutine);
        if (healthScript != null) healthScript.StopBlocking();
        
        animator.SetBool("Blocking", false);
        
        //Ensures we NEVER accidentally unlock movement if we are stunned
        if (movementScript != null && !isStunned) 
        {
            movementScript.isAttacking = false; 
        }
    }

    IEnumerator EnableBlockRoutine()
    {
        yield return new WaitForSeconds(0);
        if (healthScript != null) healthScript.StartBlocking(); 
    }

    void LateUpdate()
    {
        if (isHeavyAttacking)
        {
            if (!animator.applyRootMotion) animator.applyRootMotion = true;
        }
    }

    void PerformComboStep()
    {
        isAttacking = true;
        isHeavyAttacking = false; 
        canCombo = false;    
        inputQueued = false; 
        lastAttackTime = Time.time;
        
        animator.applyRootMotion = false; 

        if (movementScript != null) movementScript.isAttacking = true;
        if (currentWeaponScript != null) currentWeaponScript.isHeavyAttack = false;

        if (stats != null) animator.SetFloat("AttackSpeedMultiplier", stats.attackSpeed);

        animator.ResetTrigger("Attack");
        animator.ResetTrigger("AttackCombo");
        animator.ResetTrigger("HeavyAttack");

        if (comboStep == 0)
        {
            animator.SetTrigger("Attack");
            comboStep = 1; 
        }
        else
        {
            animator.SetTrigger("AttackCombo");
            comboStep = 0; 
        }
    }
    
    public void BreakGuard()
    {
        if (isStunned) return; 

        ForceStopBlocking();
        
        requireNewBlockPress = true; 
        nextBlockTime = Time.time + 1.5f; 

        animator.ResetTrigger("Attack");
        animator.ResetTrigger("AttackCombo");
        animator.ResetTrigger("HeavyAttack");
        
        animator.SetTrigger("Stun"); 
        
        isStunned = true; 
        if (movementScript != null) movementScript.isStunned = true;

        if (activeStunCoroutine != null) StopCoroutine(activeStunCoroutine);
        activeStunCoroutine = StartCoroutine(StunTimerRoutine(1.5f)); 
    }

    private IEnumerator StunTimerRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        
        isStunned = false; 
        if (movementScript != null) movementScript.isStunned = false;
        
        ResetCombatState(); 
    }
    
    void PerformHeavyAttack()
    {
        isAttacking = true;
        isHeavyAttacking = true;
        canCombo = false;
        inputQueued = false;
        comboStep = 0;
        lastAttackTime = Time.time;
        
        if (movementScript != null) movementScript.isAttacking = true;

        if (stats != null)
        {
            animator.SetFloat("AttackSpeedMultiplier", (stats.attackSpeed / 2) + 0.5f);
            nextAttackTime = Time.time + ((1f / stats.attackSpeed) + 0.5f);
        }

        if (currentWeaponScript != null) currentWeaponScript.isHeavyAttack = true;
        
        animator.ResetTrigger("Attack");
        animator.ResetTrigger("AttackCombo");
        animator.SetTrigger("HeavyAttack");
    }

    public void OnComboWindowOpen()
    {
        if (!isHeavyAttacking) canCombo = true;
    }

    public void OnFinishAttack()
    {
        if (Time.time - lastAttackTime < 0.2f) return;
        ResetCombatState();
        nextAttackTime = Time.time + 0.2f; 
    }
    
    public void OnInterrupted()
    {
        if (isStunned) return; 

        ForceStopBlocking();
        
        requireNewBlockPress = true;
        nextBlockTime = Time.time + 0.5f; 

        animator.ResetTrigger("Attack");
        animator.ResetTrigger("AttackCombo");
        animator.ResetTrigger("HeavyAttack");

        animator.SetTrigger("Recoil");
        
        isStunned = true; 
        if (movementScript != null) movementScript.isStunned = true;

        if (activeStunCoroutine != null) StopCoroutine(activeStunCoroutine);
        activeStunCoroutine = StartCoroutine(StunTimerRoutine(0.5f)); 
    }

    public void ResetCombatState()
    {
        if (isStunned) return; 

        isAttacking = false;
        isHeavyAttacking = false;
        canCombo = false;
        inputQueued = false;
        comboStep = 0;
        
        animator.applyRootMotion = false; 

        if (movementScript != null)
        {
            if (animator != null && !animator.GetBool("Blocking")) 
            {
                movementScript.isAttacking = false;
            }
        }
        
        if (animationEvents != null) animationEvents.CloseDamageWindow();
    }
}