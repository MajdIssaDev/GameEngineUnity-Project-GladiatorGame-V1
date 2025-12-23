using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public EnemyLocomotion locomotion;
    public Stats stats; // For attack speed scaling
    
    // The weapon script (assigned automatically when weapon spawns)
    private WeaponDamage currentWeaponDamage; 

    [Header("Combat Settings")]
    public float attackRange = 2.5f; // How close to be to start swinging
    public float minCooldown = 2.0f;
    public float maxCooldown = 4.0f;
    
    [Header("Surprise Mechanic")]
    [Range(0, 100)] public int surpriseAttackChance = 30; // 30% chance to attack instantly on hit
    public float hitStunDuration = 0.5f; // Normal delay when hit

    [Header("Alive Movement (Shuffling)")]
    public float shuffleSpeed = 1.0f;
    public float shuffleInterval = 3.0f; // Change direction every 3 seconds

    private float attackTimer;
    private bool isAttacking;
    private float lastShuffleTime;
    private Vector3 shuffleOffset;

    void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (locomotion == null) locomotion = GetComponent<EnemyLocomotion>();
        if (stats == null) stats = GetComponent<Stats>();

        // Start with a random cooldown so they don't all attack at once
        ResetAttackTimer();
    }

    void Update()
    {
        if (locomotion.playerTarget == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, locomotion.playerTarget.position);

        // Decrease cooldown timer
        if (attackTimer > 0) attackTimer -= Time.deltaTime;

        // --- LOGIC GATE ---
        // Only trigger combat logic if we are within range
        if (distanceToPlayer <= attackRange)
        {
            // 2. Handle Attacking
            if (attackTimer <= 0 && !isAttacking)
            {
                PerformAttack();
            }
        }
        else
        {
            // If out of range, reset shuffle so we run straight to player
            shuffleOffset = Vector3.zero;
        }
    }

    // --- MOVEMENT LOGIC ---
    void HandleCombatShuffling()
    {
        // Every few seconds, pick a new random direction to "pace"
        if (Time.time > lastShuffleTime + shuffleInterval)
        {
            // Pick a random direction (Left/Right/Back) - rarely forward
            float randomX = Random.Range(-1.5f, 1.5f);
            float randomZ = Random.Range(-0.5f, 0.5f); // Slight forward/back
            
            shuffleOffset = new Vector3(randomX, 0, randomZ);
            lastShuffleTime = Time.time;
            
            // Randomize interval slightly for natural feel
            shuffleInterval = Random.Range(2.0f, 4.0f); 
        }

        // Apply this offset to the Locomotion target logic
        // We do this by telling the NavMesh to go to (Player + Offset) instead of just (Player)
        if (locomotion.agent != null && locomotion.agent.enabled)
        {
            Vector3 targetPos = locomotion.playerTarget.position + shuffleOffset;
            locomotion.agent.SetDestination(targetPos);
        }
    }

    // --- ATTACK LOGIC ---
    void PerformAttack()
    {
        isAttacking = true;
        
        // Stop moving while attacking (optional, but looks better for heavy swings)
        if (locomotion.agent != null) locomotion.agent.isStopped = true;

        // Set Animation Speed based on Stats (like Player)
        float speedMult = (stats != null) ? stats.attackSpeed : 1f;
        animator.SetFloat("AttackSpeedMultiplier", speedMult);

        // Trigger Animation
        animator.SetTrigger("Attack");

        // Cooldown is reset via Animation Event "FinishAttack" or manually here as backup
    }

    // --- CALLED BY HEALTH / HIT REACTION SCRIPT ---
    public void OnTakeDamage()
    {
        // Roll the dice: Surprise Attack or Stun?
        int roll = Random.Range(0, 100);

        if (roll < surpriseAttackChance)
        {
            // SURPRISE! Remove cooldown instantly
            // "Enraged" behavior
            attackTimer = 0f;
        }
        else
        {
            // Normal Reaction: Add delay (flinch)
            attackTimer += hitStunDuration;
        }
    }

    // --- WEAPON SETUP (Called by EnemyWeaponHandler) ---
    public void SetWeapon(WeaponDamage newWeapon)
    {
        currentWeaponDamage = newWeapon;
    }

    // --- ANIMATION EVENTS (Must match PlayerCombat events) ---
    
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
        
        // Resume moving
        if (locomotion.agent != null) locomotion.agent.isStopped = false;

        // Set next cooldown
        ResetAttackTimer();
    }

    void ResetAttackTimer()
    {
        attackTimer = Random.Range(minCooldown, maxCooldown);
    }
}