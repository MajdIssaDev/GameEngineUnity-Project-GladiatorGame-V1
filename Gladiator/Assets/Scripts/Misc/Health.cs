using UnityEngine;
using UnityEngine.UI; 
public enum DefenseType { None, Block, Parry }

public class HealthScript : MonoBehaviour
{
    [Header("Health Settings")]
    public float currentHealth;
    [SerializeField] private float maxHealth;
    public Stats stats;

    [Header("UI References")]
    public Slider playerHudSlider; // Player UI
    [SerializeField] private EnemyHealthBar enemyFloatingBar; // Enemy UI

    [Header("Energy System")]
    public float currentEnergy;
    public Slider energyHudSlider;
    
    [Header("Block / Parry Settings")]
    public bool IsBlocking = false;
    private float blockStartTime;
    private float parryWindow = 0.35f; // 0.35s window for Parry
    private float blockEnergyCost = 20f; 

    // Internal State
    public bool isInvincible = false;
    public bool IsDead = false; 
    private Animator animator;
    private Collider mainCollider;

    void Start()
    {
        animator = GetComponent<Animator>();
        mainCollider = GetComponent<Collider>();
        stats = GetComponent<Stats>();

        // Initialize Stats
        currentHealth = maxHealth;
        if (stats != null) currentEnergy = stats.maxEnergy;

        enemyFloatingBar = GetComponentInChildren<EnemyHealthBar>();
        UpdateHealthUI();
        UpdateEnergyUI();
    }

    void FixedUpdate()
    {
        if (stats == null) return;

        // 1. Health Regen
        if (!IsDead && currentHealth > 0 && currentHealth < maxHealth)
        {
            currentHealth += stats.regenSpeed / 50;
            if (currentHealth > maxHealth) currentHealth = maxHealth;
            UpdateHealthUI();
        }
        
        // 2. Energy Regen
        // Regen is 80% slower while holding block
        float regenMult = IsBlocking ? 0.2f : 1.0f; 
        
        if (!IsDead && currentEnergy < stats.maxEnergy)
        {
            currentEnergy += (stats.energyRegenRate * regenMult) * Time.fixedDeltaTime;
            if (currentEnergy > stats.maxEnergy) currentEnergy = stats.maxEnergy;
            UpdateEnergyUI();
        }
    }
    
    // --- BLOCK INPUTS (Called by PlayerCombat) ---
    public void StartBlocking()
    {
        if (IsBlocking) return;
        
        IsBlocking = true;
        blockStartTime = Time.time;
    }

    public void StopBlocking()
    {
        IsBlocking = false;
    }

    // --- DAMAGE LOGIC ---
    public void takeDamage(float damageAmount, GameObject attacker = null)
    {
        if (IsDead || isInvincible) return;

        // 1. BLOCK LOGIC
        if (IsBlocking && attacker != null)
        {
            Vector3 directionToAttacker = (attacker.transform.position - transform.position).normalized;
            float dot = Vector3.Dot(transform.forward, directionToAttacker);
            
            if (dot > 0.25f) // Frontal Cone
            {
                // A. PARRY
                if (Time.time - blockStartTime <= parryWindow)
                {
                    PerformParry(attacker);
                    return; 
                }
                
                // B. REGULAR BLOCK
                if (currentEnergy >= blockEnergyCost)
                {
                    PerformBlock(attacker);
                    return; 
                }
                else
                {
                    // C. GUARD BREAK (Recoil happens HERE only)
                    IsBlocking = false;
                    animator.SetBool("Blocking", false);
                    
                    // YOU get recoiled/stunned because you failed
                    animator.SetTrigger("Recoil"); 
                    Debug.Log("Guard Broken!");
                }
            }
        }

        // 2. TAKE DAMAGE
        float damage = damageAmount * (100 / (100 + stats.defence));
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }

        UpdateHealthUI();
        if (enemyFloatingBar != null) enemyFloatingBar.OnTakeDamage(damage);
    }

    void PerformParry(GameObject attacker)
    {
        // 1. DEFENDER REACTION (You)
        // You requested "Blocked" trigger for the blocker
        animator.SetTrigger("Blocked");
        
        // Reward Energy
        currentEnergy += 20;
        UpdateEnergyUI();

        // 2. ATTACKER REACTION (Enemy)
        var enemyCombat = attacker.GetComponent<EnemyCombat>();
        if (enemyCombat != null)
        {
            enemyCombat.TriggerStunReaction(); // Plays "Stunned" animation
        }
    }

    void PerformBlock(GameObject attacker)
    {
        // 1. DEFENDER REACTION (You)
        // No Recoil. Just the "Blocked" animation.
        animator.SetTrigger("Blocked");

        // Cost Energy
        currentEnergy -= blockEnergyCost;
        UpdateEnergyUI();
        
        // 2. ATTACKER REACTION (Enemy)
        var enemyCombat = attacker.GetComponent<EnemyCombat>();
        if (enemyCombat != null)
        {
            enemyCombat.ApplyBlockSlow(3.0f);  // Slows them down
        }
    }

    // --- UI UPDATES ---
    private void UpdateEnergyUI()
    {
        if (energyHudSlider != null && stats != null)
        {
            energyHudSlider.value = currentEnergy / stats.maxEnergy;
        }
    }

    private void UpdateHealthUI()
    {
        if (playerHudSlider != null)
        {
            playerHudSlider.value = currentHealth / maxHealth;
        }
    }

    // --- DEATH LOGIC ---
    private void Die()
    {
        if (IsDead) return;
        IsDead = true;

        if (animator != null)
        {
            animator.SetTrigger("Die");
            animator.speed = Random.Range(0.9f, 1.1f); 
        }

        // Stop Combat Logic
        var combatEvents = GetComponentInChildren<CombatAnimationEvents>();
        if (combatEvents != null) combatEvents.CloseDamageWindow();

        var combatScript = GetComponent<PlayerCombat>();
        if (combatScript != null) combatScript.enabled = false;
        
        var enemyCombat = GetComponent<EnemyCombat>();
        if (enemyCombat != null) enemyCombat.enabled = false;

        // Disable Physics/NavMesh
        if (mainCollider != null) mainCollider.enabled = false;

        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) 
        {
            if (agent.isActiveAndEnabled && agent.isOnNavMesh) agent.isStopped = true;
            agent.enabled = false; 
        }
        
        Collider[] allColliders = GetComponentsInChildren<Collider>();
        foreach (Collider c in allColliders) c.enabled = false; 

        if (enemyFloatingBar != null) Destroy(enemyFloatingBar.gameObject);
        
        gameObject.layer = LayerMask.NameToLayer("Default");
        
        // Handle Game Manager
        if (gameObject.CompareTag("Enemy"))
        {
            if (GameManager.Instance != null) GameManager.Instance.EnemyDefeated();
            Destroy(gameObject, 5f); 
        }
        else
        {
            var movement = GetComponent<PlayerLocomotion>();
            if (movement != null) movement.enabled = false;

            if (GameManager.Instance != null) GameManager.Instance.GameOver();
        }
    }
    
    public DefenseType CheckDefense(Vector3 attackerPos)
    {
        if (!IsBlocking) return DefenseType.None;
        if (currentEnergy < 20f) return DefenseType.None; // Ensure this matches 'blockEnergyCost'

        // Check Angle
        Vector3 dir = (attackerPos - transform.position).normalized;
        float dot = Vector3.Dot(transform.forward, dir);
        
        if (dot > 0.25f)
        {
            // Check Parry Timer
            if (Time.time - blockStartTime <= 0.25f) // Ensure this matches 'parryWindow'
            {
                return DefenseType.Parry;
            }
            
            return DefenseType.Block;
        }

        return DefenseType.None;
    }
    
    public bool TrySpendEnergy(float amount)
    {
        if (currentEnergy >= amount)
        {
            currentEnergy -= amount;
            UpdateEnergyUI(); // Update the slider immediately
            return true; // Success: You have enough energy
        }
        
        return false; // Fail: Not enough energy
    }

    // Setters
    public void setMaxHealth(float val) { maxHealth = val; UpdateHealthUI(); }
    public float getMaxHealth() { return maxHealth; }
    public void setCurrentHealth(float val) { currentHealth = val; UpdateHealthUI(); }
    public void EnableIFrames() { isInvincible = true; }
    public void DisableIFrames() { isInvincible = false; }
}