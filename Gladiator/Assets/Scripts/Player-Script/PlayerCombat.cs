using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections; // Required for Coroutines

public class PlayerCombat : MonoBehaviour, ICombatReceiver
{
    [Header("References")]
    public Animator animator;
    public PlayerLocomotion movementScript;
    [SerializeField] Stats stats;
    
    // Reference to the Event Script
    private CombatAnimationEvents animationEvents;
    
    private RuntimeAnimatorController baseController;
    private WeaponDamage currentWeaponScript; 

    // States
    public bool isAttacking { get; private set; }
    public bool isHeavyAttacking { get; private set; } 
    private bool canCombo = false;      
    private bool inputQueued = false;   

    // Logic Variables
    private int comboStep = 0;
    private float lastAttackTime = 0f;
    private float maxAttackDuration = 5.0f; 
    private float nextAttackTime = 0f;      
    
    [Header("Block Settings")]
    public float blockCooldown = 0.5f; 
    public float blockStartupTime = 0.1f; // Delay before block logic kicks in
    
    private float nextBlockTime = 0f;
    private Coroutine activeBlockCoroutine; // To track and cancel the delay
    
    [Header("Energy Settings")] 
    public float lightAttackCost = 10f;
    public float heavyAttackCost = 25f;
    
    private HealthScript healthScript;

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (stats == null) stats = GetComponent<Stats>();
        healthScript = GetComponent<HealthScript>();
        
        animationEvents = animator.GetComponent<CombatAnimationEvents>();
        baseController = animator.runtimeAnimatorController; 
        isAttacking = false;
    }

    public void EquipNewWeapon(GameObject newWeaponObject, AnimatorOverrideController overrideController)
    {
        currentWeaponScript = newWeaponObject.GetComponent<WeaponDamage>();

        if (animationEvents != null)
        {
            animationEvents.SetCurrentWeapon(currentWeaponScript);
        }

        if (animator == null) animator = GetComponent<Animator>(); 
        if (baseController == null) baseController = animator.runtimeAnimatorController;

        if (overrideController != null)
            animator.runtimeAnimatorController = overrideController;
        else if (baseController != null)
            animator.runtimeAnimatorController = baseController;

        ResetCombatState();
    }
    
    void Update()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        
        // Failsafe
        if (isAttacking && Time.time > lastAttackTime + maxAttackDuration) OnFinishAttack();
        
        // --- 1. BLOCKING LOGIC ---
        if (healthScript != null)
        {
            if (Input.GetKeyDown(KeyCode.E) && !isAttacking)
            {
                if (Time.time >= nextBlockTime)
                {
                    // Visuals START NOW
                    animator.SetBool("Blocking", true);
                    if (movementScript != null) movementScript.isAttacking = true; // Stop moving
                    
                    // Logic STARTS LATER (Wait for animation)
                    if (activeBlockCoroutine != null) StopCoroutine(activeBlockCoroutine);
                    activeBlockCoroutine = StartCoroutine(EnableBlockRoutine());
                }
            }
            else if (Input.GetKeyUp(KeyCode.E))
            {
                // Cancel the routine if we released E too fast
                if (activeBlockCoroutine != null) StopCoroutine(activeBlockCoroutine);

                // Stop Logic
                healthScript.StopBlocking();
                
                // Stop Visuals
                animator.SetBool("Blocking", false);
                if (movementScript != null) movementScript.isAttacking = false; // Restore movement

                nextBlockTime = Time.time + blockCooldown;
            }
        }
        
        // Dont attack while blocking!
        if (healthScript != null && healthScript.IsBlocking) return;
        
        // --- 2. ATTACK LOGIC (With Energy Check) ---
        if (Input.GetButtonDown("Fire1"))
        {
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                // HEAVY ATTACK
                if (!isAttacking && Time.time >= nextAttackTime) 
                {
                    // Check Energy Cost
                    if (healthScript != null && healthScript.TrySpendEnergy(heavyAttackCost))
                    {
                        PerformHeavyAttack();
                    }
                    else
                    {
                        Debug.Log("Not enough energy for Heavy Attack!");
                    }
                }
            }
            else
            {
                // LIGHT ATTACK
                if (isAttacking)
                {
                    if (!isHeavyAttacking) inputQueued = true;
                }
                else if (Time.time >= nextAttackTime)
                {
                    // Check Energy Cost
                    if (healthScript != null && healthScript.TrySpendEnergy(lightAttackCost))
                    {
                        comboStep = 0;
                        PerformComboStep();
                    }
                    else
                    {
                        Debug.Log("Out of Energy!");
                    }
                }
            }
        }

        // COMBO QUEUE LOGIC
        if (inputQueued && canCombo)
        {
            // We also charge energy for the next hit in the combo!
            if (healthScript != null && healthScript.TrySpendEnergy(lightAttackCost))
            {
                PerformComboStep();
            }
            else
            {
                inputQueued = false; // Cancel combo if energy runs out mid-swing
            }
        }
    }

    // --- Block Delay Routine ---
    IEnumerator EnableBlockRoutine()
    {
        // Wait for the animation to actually raise the shield
        yield return new WaitForSeconds(0);
        
        // NOW turn on the actual blocking stats
        if (healthScript != null)
        {
            healthScript.StartBlocking(); 
        }
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
        animator.SetTrigger("Recoil");
        ResetCombatState();
    }

    private void ResetCombatState()
    {
        isAttacking = false;
        isHeavyAttacking = false;
        canCombo = false;
        inputQueued = false;
        comboStep = 0;
        
        animator.applyRootMotion = false; 

        if (movementScript != null) movementScript.isAttacking = false;
        
        if (animationEvents != null) animationEvents.CloseDamageWindow();
    }
}