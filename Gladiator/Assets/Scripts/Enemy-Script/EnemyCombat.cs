using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyAI))] 
public class EnemyCombat : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public Stats statsScript;
    private EnemyAI aiScript; 

    [Header("Weapon System")]
    private WeaponDamage currentWeaponDamage; 

    [Header("Attack Settings")]
    public float attackRange = 2.5f;
    public float attackCooldown = 2.0f;
    public float minAttackDelay = 0.2f;
    
    [Header("Combat Probabilities")]
    [Range(0, 1)] public float heavyAttackChance = 0.3f; 
    [Range(0, 1)] public float comboChance = 0.6f;       
    
    [Header("Dodge Settings")]
    public float maxDodgeChance = 75.0f;
    public float dodgeCooldown = 3.0f;

    // Internal State
    private NavMeshAgent agent;
    private Transform playerTarget;
    private float lastAttackTime;
    private float lastDodgeTime;
    
    public bool isAttacking { get; private set; }

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        aiScript = GetComponent<EnemyAI>();
        if (aiScript.playerTarget != null) playerTarget = aiScript.playerTarget;
        if (statsScript == null) statsScript = GetComponent<Stats>();
        isAttacking = false;
    }

    public void SetWeapon(WeaponDamage newWeapon)
    {
        currentWeaponDamage = newWeapon;
    }

    public bool CanAttack(float distanceToPlayer)
    {
        if (distanceToPlayer > attackRange) return false;
        if (isAttacking) return false;
        if (Time.time < lastAttackTime + attackCooldown) return false;
        return true;
    }

    public void StartAttack()
    {
        if (!isAttacking) StartCoroutine(PerformAttackSequence());
    }

    IEnumerator PerformAttackSequence()
    {
        isAttacking = true;
        
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;     
            agent.velocity = Vector3.zero;
        }

        // Face Player
        if (playerTarget != null) 
        {
            Vector3 direction = (playerTarget.position - transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero) transform.rotation = Quaternion.LookRotation(direction);
        }

        yield return new WaitForSeconds(minAttackDelay);

        // Reset Triggers
        animator.ResetTrigger("Attack");
        animator.ResetTrigger("HeavyAttack");
        animator.ResetTrigger("AttackCombo");

        // Set Speed
        float baseSpeed = (statsScript != null) ? statsScript.attackSpeed : 1.0f;

        // Decide Attack Type
        bool isHeavy = Random.value < heavyAttackChance;
        if (currentWeaponDamage != null) currentWeaponDamage.isHeavyAttack = isHeavy;

        if (isHeavy)
        {
            // Heavy Logic
            float heavySpeed = (baseSpeed / 2.0f) + 0.5f;
            animator.SetFloat("AttackSpeedMultiplier", heavySpeed);
            animator.SetTrigger("HeavyAttack");
        }
        else
        {
            // Light Logic
            animator.SetFloat("AttackSpeedMultiplier", baseSpeed);
            if (currentWeaponDamage != null) currentWeaponDamage.isHeavyAttack = false;
            animator.SetTrigger("Attack");
            
            // [CHANGE] We do NOT wait for combo here anymore. 
            // We wait for the Animation Event "OpenComboWindow" to trigger it.
        }

        // Failsafe timer (waits for "FinishAttack" event)
        float safetyTimer = 4.0f; 
        while (isAttacking && safetyTimer > 0)
        {
            safetyTimer -= Time.deltaTime;
            yield return null; 
        }

        FinishAttack(); 
    }

    // --- ANIMATION EVENTS ---

    public void OpenComboWindow()
    {
        // This function is called by the Event at the end of the animation (recovery phase)
        
        // 1. Roll the dice
        bool shouldCombo = Random.value < comboChance;

        // 2. Only combo if we are NOT doing a heavy attack
        bool isHeavy = animator.GetCurrentAnimatorStateInfo(0).IsName("Heavy Attack"); 

        if (shouldCombo && !isHeavy)
        {
            // This trigger will interrupt the current animation and start the combo
            // Because we transition early, the "FinishAttack" event of the FIRST animation is skipped.
            // The logic will wait for the "FinishAttack" of the SECOND animation (the combo).
            animator.SetTrigger("AttackCombo");
        }
    }

    public void OpenDamageWindow()
    {
        if (currentWeaponDamage != null) currentWeaponDamage.EnableHitbox();
    }

    public void CloseDamageWindow()
    {
        if (currentWeaponDamage != null) currentWeaponDamage.DisableHitbox();
    }
    
    public void FinishAttack()
    {
        // This runs when the LAST animation in the chain finishes
        isAttacking = false;
        lastAttackTime = Time.time;
        CloseDamageWindow();
    }

    public void ReactToIncomingAttack()
    {
        if (Time.time < lastDodgeTime + dodgeCooldown || isAttacking) return;

        int level = statsScript != null ? statsScript.GetLevel() : 1;
        float currentDodgeChance = Mathf.Clamp(level * 5.0f, 0, maxDodgeChance);

        if (Random.Range(0, 100) < currentDodgeChance)
        {
            animator.SetTrigger("Roll");
            lastDodgeTime = Time.time;
            lastAttackTime = Time.time + 1.0f; 
        }
    }
}