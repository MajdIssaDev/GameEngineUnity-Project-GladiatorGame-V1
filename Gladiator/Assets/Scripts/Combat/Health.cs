using UnityEngine;

public class HealthScript : MonoBehaviour
{
    public float currentHealth;
    [SerializeField] private float maxHealth;
    [SerializeField] private Stats stats;
    [SerializeField] private EnemyHealthBar healthBar;
    
    void awake()
    {
        
    }
    
    void Start()
    {
        currentHealth = maxHealth;
        stats = GetComponent<Stats>();
        healthBar = GetComponentInChildren<EnemyHealthBar>();
    }

    void FixedUpdate()
    {
        if (currentHealth > 0 && currentHealth < maxHealth)
        {
            currentHealth += stats.regenSpeed/50;
            if (currentHealth > maxHealth)
            {
                currentHealth = maxHealth;
            }
        }
    }

    public void takeDamage(float damageAmount)
    {
        float damage = damageAmount * (100/(100 + stats.defence)); // capped at 1
        gameObject.GetComponent<HealthScript>().currentHealth -= damage;
        if (this.gameObject.GetComponent<HealthScript>().currentHealth <= 0)
        {
            this.gameObject.GetComponent<HealthScript>().currentHealth = 0;
            //other.gameObject.GetComponent<PlayerLocomotion>().enabled = false;
            Debug.Log($"{gameObject.name} is dead.");
        }
        
        // 2. Tell it to wake up
        if (healthBar != null)
        {
            healthBar.OnTakeDamage(); 
        }
    }

    public void setMaxHealth(float maxHealth)
    {
        this.maxHealth = maxHealth;
    }

    public float getMaxHealth()
    {
        return maxHealth;
    }

    public void setCurrentHealth(float currentHealth)
    {
        this.currentHealth = currentHealth;
    }
}
