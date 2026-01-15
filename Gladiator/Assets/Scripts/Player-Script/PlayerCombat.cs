using UnityEngine;

// 1. Add the Interface here
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

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (stats == null) stats = GetComponent<Stats>();
        
        // Grab the generic event script
        animationEvents = animator.GetComponent<CombatAnimationEvents>();

        baseController = animator.runtimeAnimatorController; 
        isAttacking = false;
    }

    public void EquipNewWeapon(GameObject newWeaponObject, AnimatorOverrideController overrideController)
    {
        currentWeaponScript = newWeaponObject.GetComponent<WeaponDamage>();

        // Pass the weapon to the generic event script
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
        // Failsafe
        if (isAttacking && Time.time > lastAttackTime + maxAttackDuration)
        {
            OnFinishAttack(); // Call the interface method directly
        }

        // Input Handling
        if (Input.GetButtonDown("Fire1"))
        {
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                if (!isAttacking && Time.time >= nextAttackTime) PerformHeavyAttack();
            }
            else
            {
                if (isAttacking)
                {
                    if (!isHeavyAttacking) inputQueued = true;
                }
                else if (Time.time >= nextAttackTime)
                {
                    comboStep = 0;
                    PerformComboStep();
                }
            }
        }

        // Combo Logic
        if (inputQueued && canCombo)
        {
            PerformComboStep();
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

    // --- INTERFACE IMPLEMENTATION ---
    // These names match the interface exactly

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

    private void ResetCombatState()
    {
        isAttacking = false;
        isHeavyAttacking = false;
        canCombo = false;
        inputQueued = false;
        comboStep = 0;
        
        animator.applyRootMotion = false; 

        if (movementScript != null) movementScript.isAttacking = false;
        
        // Safety: ensure hitbox closes
        if (animationEvents != null) animationEvents.CloseDamageWindow();
    }
}