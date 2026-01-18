using UnityEngine;
using UnityEngine.UI; // Required for UI Slider

public class HealthScript : MonoBehaviour
{
    public float currentHealth;
    [SerializeField] private float maxHealth;
    [SerializeField] private Stats stats;

    [Header("UI References")]
    // 1. This is for the PLAYER (The slider on the screen)
    // Only the Player will have this assigned.
    public Slider playerHudSlider;

    // 2. This is for the ENEMY (The floating bar above head)
    // Only Enemies will have this component.
    [SerializeField] private EnemyHealthBar enemyFloatingBar;

    public bool isInvincible = false;
    private bool isDead = false; // Prevent dying twice

    // --- NEW REFERENCES ---
    private Animator animator;
    private Collider mainCollider;

    void Start()
    {
        currentHealth = maxHealth;
        stats = GetComponent<Stats>();
        animator = GetComponent<Animator>();
        mainCollider = GetComponent<Collider>();

        enemyFloatingBar = GetComponentInChildren<EnemyHealthBar>();
        UpdateHealthUI();
    }

    void FixedUpdate()
    {
        // Add !isDead check so corpses don't regenerate health
        if (!isDead && currentHealth > 0 && currentHealth < maxHealth)
        {
            currentHealth += stats.regenSpeed / 50;
            if (currentHealth > maxHealth) currentHealth = maxHealth;
            UpdateHealthUI();
        }
    }

    public void takeDamage(float damageAmount)
    {
        if (isDead || isInvincible) return; // Don't hit dead things

        float damage = damageAmount * (100 / (100 + stats.defence));
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }

        UpdateHealthUI();

        if (enemyFloatingBar != null)
        {
            enemyFloatingBar.OnTakeDamage(damage);
        }
    }

    private void UpdateHealthUI()
    {
        if (playerHudSlider != null)
        {
            playerHudSlider.value = currentHealth / maxHealth;
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // 1. Play Animation
        if (animator != null)
        {
            animator.SetTrigger("Die");
            animator.speed = Random.Range(0.9f, 1.1f); 
        }

        // 2. STOP COMBAT
        CombatAnimationEvents combatEvents = GetComponentInChildren<CombatAnimationEvents>();
        if (combatEvents != null) combatEvents.CloseDamageWindow();

        // Disable Attack Scripts
        var combatScript = GetComponent<PlayerCombat>();
        if (combatScript != null) combatScript.enabled = false;
        
        var enemyCombat = GetComponent<EnemyCombat>();
        if (enemyCombat != null) enemyCombat.enabled = false;

        // 3. DISABLE PHYSICS & NAVIGATION (The Fix)
        
        // Fix for "Stop called on inactive agent":
        if (mainCollider != null) mainCollider.enabled = false;

        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) 
        {
            // --- FIX STARTS HERE ---
            // Only try to stop the agent if it is actually ON and ON THE FLOOR
            if (agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }
            // -----------------------
        
            agent.enabled = false; 
        }
        
        // Disable ALL Colliders (Main Body + Arms/Limbs) so we can walk through them
        Collider[] allColliders = GetComponentsInChildren<Collider>();
        foreach (Collider c in allColliders)
        {
            c.enabled = false; 
            // Note: CharacterController is also a Collider, so this turns it off too!
        }

        // Remove Visuals
        if (enemyFloatingBar != null) Destroy(enemyFloatingBar.gameObject);

        // 4. HANDLE GAME LOGIC
        if (gameObject.CompareTag("Enemy"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.EnemyDefeated();
            }
            Destroy(gameObject, 5f); 
        }
        else
        {
            Debug.Log("Player Died");
            
            // Disable Movement Inputs
            var movement = GetComponent<PlayerLocomotion>();
            if (movement != null) movement.enabled = false;

            // Show Game Over Screen
            if (GameManager.Instance != null) 
            {
                GameManager.Instance.GameOver();
            }
        }
    }

    // Setters/Getters
    public void setMaxHealth(float maxHealth)
    {
        this.maxHealth = maxHealth;
        UpdateHealthUI();
    }

    public float getMaxHealth()
    {
        return maxHealth;
    }

    public void setCurrentHealth(float currentHealth) 
    {
        this.currentHealth = currentHealth;
        UpdateHealthUI();
    }

    public void EnableIFrames()
    {
        isInvincible = true;
    }

    public void DisableIFrames()
    {
        isInvincible = false;
    }
}