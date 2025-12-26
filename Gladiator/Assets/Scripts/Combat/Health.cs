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
    
    void Start()
    {
        currentHealth = maxHealth;
        stats = GetComponent<Stats>();
        
        // Auto-find the floating bar (Common for enemies)
        enemyFloatingBar = GetComponentInChildren<EnemyHealthBar>();

        // Initialize UI
        UpdateHealthUI();
    }

    void FixedUpdate()
    {
        if (currentHealth > 0 && currentHealth < maxHealth)
        {
            currentHealth += stats.regenSpeed / 50;
            if (currentHealth > maxHealth)
            {
                currentHealth = maxHealth;
            }
            // Update UI while regenerating
            UpdateHealthUI();
        }
    }

    public void takeDamage(float damageAmount)
    {
        // 1. Calculate Mitigation
        float damage = damageAmount * (100 / (100 + stats.defence));
        
        // 2. Apply Damage
        currentHealth -= damage;
        
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
        
        // 3. Update Visuals
        UpdateHealthUI();

        // 4. Special Logic for Enemy Floating Text/Bar
        if (enemyFloatingBar != null)
        {
            enemyFloatingBar.OnTakeDamage(damage); 
        }
    }

    private void UpdateHealthUI()
    {
        // LOGIC: If I have a HUD slider assigned, I must be the player.
        // If this variable is null, this code is skipped (so enemies don't crash).
        if (playerHudSlider != null)
        {
            playerHudSlider.value = currentHealth / maxHealth;
        }
    }

    private void Die()
    {
        // 1. IMPORTANT: Check the Tag!
        if (gameObject.CompareTag("Enemy"))
        {
            // 2. Tell the Manager we died
            if (GameManager.Instance != null) 
            {
                GameManager.Instance.EnemyDefeated();
            }
             
            // 3. Delete the enemy
            Destroy(gameObject);
        }
        else
        {
            // Player death logic (Restart level, Game Over screen, etc.)
            Debug.Log("Player Died");
        }
    }

    // Setters/Getters
    public void setMaxHealth(float maxHealth) => this.maxHealth = maxHealth;
    public float getMaxHealth()
    {
        return maxHealth;
    } 
    public void setCurrentHealth(float currentHealth) => this.currentHealth = currentHealth;
    
    public void EnableIFrames()
    {
        isInvincible = true;
    }

    public void DisableIFrames()
    {
        isInvincible = false;
    }
}