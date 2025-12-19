using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public PlayerLocomotion movementScript; 
    
    [SerializeField] Stats stats;
    private WeaponDamage currentWeaponScript; 
    private bool isAttacking = false;
    private float nextAttackTime = 0f; // Track when the next attack is allowed

    void Start()
    {
        // Automatically find the Stats script if not assigned
        if (stats == null) stats = GetComponent<Stats>();
    }

    public void EquipNewWeapon(GameObject newWeaponObject)
    {
        currentWeaponScript = newWeaponObject.GetComponent<WeaponDamage>();
    }

    void Update()
    {
        // Check if enough time has passed based on attack speed cooldown
        if (Input.GetButtonDown("Fire1") && !isAttacking && Time.time >= nextAttackTime) 
        {
            PerformAttack();
        }
    }

    void PerformAttack()
    {
        if (movementScript == null)
        {
            Debug.LogError("CRITICAL: Movement Script is NOT assigned!");
            return;
        }

        isAttacking = true; 
        movementScript.isAttacking = true; 

        // 1. Update Animation Speed
        // Assumes you created a 'AttackSpeedMultiplier' float parameter in your Animator
        if (stats != null)
        {
            animator.SetFloat("AttackSpeedMultiplier", stats.attackSpeed);
            
            // 2. Set Cooldown Timer
            // Formula: Higher attack speed = lower delay between clicks
            float attackDelay = 1f / stats.attackSpeed; 
            nextAttackTime = Time.time + attackDelay;
        }

        // 3. Trigger Animation
        if (currentWeaponScript != null)
        {
            // Use the specific triggers for different weapon types
            string weaponTag = currentWeaponScript.gameObject.tag;
            
            if (weaponTag == "Spear") animator.SetTrigger("SpearAttack");
            else if (weaponTag == "Sword") animator.SetTrigger("SpearAttack");
            else if (weaponTag == "Axe") animator.SetTrigger("SpearAttack");
            else animator.SetTrigger("SpearAttack");
        }
    }

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