using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public PlayerLocomotion movementScript; 
    
    [SerializeField] Stats stats;
    private WeaponDamage currentWeaponScript; 
    
    // Store the default (Axe/Unarmed) controller here so we can switch back to it
    private RuntimeAnimatorController baseController; 

    private bool isAttacking = false;
    private float nextAttackTime = 0f; 

    void Start()
    {
        if (stats == null) stats = GetComponent<Stats>();
        
        // SAVE the original controller (The one assigned in Inspector)
        baseController = animator.runtimeAnimatorController;
    }

    // UPDATED: Now requires the weapon's Override Controller (can be null)
    public void EquipNewWeapon(GameObject newWeaponObject, AnimatorOverrideController overrideController)
    {
        currentWeaponScript = newWeaponObject.GetComponent<WeaponDamage>();

        // 2. CHECK: Does this weapon have a special animation file?
        if (overrideController != null)
        {
            // YES (Spear) -> Use the special animations
            animator.runtimeAnimatorController = overrideController;
        }
        else
        {
            // NO (Axe) -> REVERT to the default animations we saved at Start()
            animator.runtimeAnimatorController = baseController;
        }
    }
    void Update()
    {
        if (Input.GetButtonDown("Fire1") && !isAttacking && Time.time >= nextAttackTime) 
        {
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                PerformHeavyAttack();
            }
            else
            {
                PerformNormalAttack();
            }
        }
    }

    void PerformHeavyAttack()
    {
        if (movementScript == null) return;

        isAttacking = true; 
        movementScript.isAttacking = true; 

        if (stats != null)
        {
            animator.SetFloat("AttackSpeedMultiplier", (stats.attackSpeed / 2) + 0.5f);
            float attackDelay = (1f / stats.attackSpeed) + 0.5f; 
            nextAttackTime = Time.time + attackDelay;
        }

        if (currentWeaponScript != null) currentWeaponScript.isHeavyAttack = true;

        animator.SetTrigger("HeavyAttack");
    }

    void PerformNormalAttack()
    {
        if (movementScript == null) return;

        isAttacking = true; 
        movementScript.isAttacking = true; 

        if (stats != null)
        {
            animator.SetFloat("AttackSpeedMultiplier", stats.attackSpeed);
            float attackDelay = 1f / stats.attackSpeed; 
            nextAttackTime = Time.time + attackDelay;
        }
        
        if (currentWeaponScript != null) currentWeaponScript.isHeavyAttack = false;
        
        // --- SIMPLIFIED LOGIC ---
        // We don't need if/else tags anymore. 
        // If we hold a Spear, the animator is already swapped, so "Attack" plays the poke.
        // If we hold an Axe, the animator is default, so "Attack" plays the swing.
        animator.SetTrigger("Attack"); 
    }

    // --- Animation Events ---
    public void OpenDamageWindow()
    {
        if (currentWeaponScript != null) currentWeaponScript.EnableHitbox();
    }

    public void CloseDamageWindow()
    {
        if (currentWeaponScript != null) currentWeaponScript.DisableHitbox();
    }

    public void FinishAttack()
    {
        isAttacking = false; 
        if (movementScript != null) movementScript.isAttacking = false;
    }
}