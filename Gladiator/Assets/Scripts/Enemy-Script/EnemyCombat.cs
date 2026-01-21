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
    
    // Status Effects
    public bool isStunned { get; private set; } = false;
    private Coroutine currentSlowRoutine;
    
    public bool isAttacking { get; private set; }
    public bool isHeavyAttacking { get; private set; } 

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        aiScript = GetComponent<EnemyAI>();
        if (aiScript.playerTarget != null) playerTarget = aiScript.playerTarget;
        
        // Ensure we grab Stats if not assigned
        if (statsScript == null) statsScript = GetComponent<Stats>();
        
        isAttacking = false;
        isHeavyAttacking = false;
    }

    public void SetWeapon(WeaponDamage newWeapon)
    {
        currentWeaponDamage = newWeapon;
    }

    public bool CanAttack(float distanceToPlayer)
    {
        if (isStunned) return false; // NEW: Cannot attack while stunned
        
        if (distanceToPlayer > attackRange) return false;
        if (isAttacking) return false;
        if (Time.time < lastAttackTime + attackCooldown) return false;
        return true;
    }

    public void StartAttack()
    {
        if (!isAttacking) StartCoroutine(PerformAttackSequence());
    }
    
    
    IEnumerator StunRoutine(float duration)
    {
        isStunned = true;
        
        // 1. Play Animation
        animator.SetTrigger("Stun"); // Needs a Trigger named "Stun"
        // animator.SetBool("Stunned", true); // Optional looped state

        // 2. Stop Moving
        if (agent != null && agent.isActiveAndEnabled) agent.isStopped = true;
        
        // 3. Apply 50% Slow via Stats
        if (statsScript != null) statsScript.ApplySlowEffect(0.5f);

        yield return new WaitForSeconds(duration);

        // 4. Reset
        if (statsScript != null) statsScript.RemoveSlowEffect();
        if (agent != null && agent.isActiveAndEnabled) agent.isStopped = false;
        isStunned = false;
    }
    
    // --- NEW: SLOW LOGIC (Block Punishment) ---
    public void ApplyBlockSlow(float duration)
    {
        if (isStunned) return;
        animator.SetTrigger("Stun");
        
        if (currentSlowRoutine != null) StopCoroutine(currentSlowRoutine);
        currentSlowRoutine = StartCoroutine(BlockSlowRoutine(duration));
    }
    
    IEnumerator BlockSlowRoutine(float duration)
    {
        // Apply 30% Slow
        if (statsScript != null) statsScript.ApplySlowEffect(0.3f);

        yield return new WaitForSeconds(duration);

        // Reset
        if (statsScript != null) statsScript.RemoveSlowEffect();
        currentSlowRoutine = null;
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

        // Set Speed (Now automatically includes Slow Effects from Stats)
        float baseSpeed = (statsScript != null) ? statsScript.attackSpeed : 1.0f;

        isHeavyAttacking = Random.value < heavyAttackChance;
        
        if (currentWeaponDamage != null) 
            currentWeaponDamage.isHeavyAttack = isHeavyAttacking;

        if (isHeavyAttacking)
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
            animator.SetTrigger("Attack");
        }

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
        bool shouldCombo = Random.value < comboChance;

        if (shouldCombo && !isHeavyAttacking)
        {
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
        isAttacking = false;
        isHeavyAttacking = false; 
        
        lastAttackTime = Time.time;
        CloseDamageWindow();
    }

    public void ReactToIncomingAttack()
    {
        if (Time.time < lastDodgeTime + dodgeCooldown || isAttacking || isStunned) return;

        int level = statsScript != null ? statsScript.GetLevel() : 1;
        float currentDodgeChance = Mathf.Clamp(level * 5.0f, 0, maxDodgeChance);

        if (Random.Range(0, 100) < currentDodgeChance)
        {
            animator.SetTrigger("Roll");
            lastDodgeTime = Time.time;
            lastAttackTime = Time.time + 1.0f; 
        }
    }
    

    public void TriggerStunReaction()
    {
        animator.SetTrigger("Stun");
        
        // Disable Hitbox immediately so they don't damage you while stunned
        if (currentWeaponDamage != null) currentWeaponDamage.DisableHitbox();
        
        // Reset flags
        isAttacking = false;
        isHeavyAttacking = false;
        
        // Apply Stun
        ApplyStun(5); // Or 0.5f, whatever feels right
    }
    
    // 2. SAFETY RESET (Fixes "Frozen Forever" bug)
    private void OnEnable()
    {
        isStunned = false; 
        isAttacking = false;
        // Reset other states if needed
    }

    public void ApplyStun(float duration)
    {
        if (isStunned) return;
        StartCoroutine(StunRoutine(duration));
    }
}