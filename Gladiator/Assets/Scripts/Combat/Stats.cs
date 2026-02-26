using UnityEngine;
public enum StatType { Strength, Defence, AttackSpeed, Regen, Health, Energy }

public class Stats : MonoBehaviour
{
    [Header("Identity")]
    public string characterName = "";

    [Header("Stats")] 
    public float strength = 1;     
    public float defence = 0;      
    public float regenSpeed = 1;   
    public float attackSpeed = 1;  
    
    //--- Energy Stat ---
    public float maxEnergy = 100f; 
    public float energyRegenRate = 10f; //Energy per second
    
    [Header("Dependencies")]
    public HealthScript healthScript; 

    // --- Caching Variables ---
    private float initialStrength;
    private float initialDefence;
    private float initialRegenSpeed;
    private float initialAttackSpeed;
    private float initialMaxEnergy;
    private float initialMaxHealth;
    
    // This is used for the slow effect debuff to remember current max speed
    private float currentBaseAttackSpeed; 
    private bool isInitialized = false;

    private void Awake()
    {
        InitializeBaseStats();
    }

    private void Start()
    {
        // Fallback in case Awake missed it, though Awake runs first.
        InitializeBaseStats(); 
    }

    private void InitializeBaseStats()
    {
        if (isInitialized) return;

        if (healthScript == null) healthScript = GetComponent<HealthScript>();

        // Cache the inspector values the moment the object is created
        initialStrength = strength;
        initialDefence = defence;
        initialRegenSpeed = regenSpeed;
        initialAttackSpeed = attackSpeed;
        initialMaxEnergy = maxEnergy;
        
        if (healthScript != null) initialMaxHealth = healthScript.getMaxHealth();
        else initialMaxHealth = 100f;

        currentBaseAttackSpeed = attackSpeed;
        isInitialized = true;
    }

    //Procedurally generate enemy stats based on the current wave round
    public void SetLevel(int targetLevel)
    {
        InitializeBaseStats(); // Safety check

        // Reset to the PREFAB'S base stats, not hardcoded 1s and 0s
        strength = initialStrength;
        defence = initialDefence;
        regenSpeed = initialRegenSpeed;
        attackSpeed = initialAttackSpeed;
        maxEnergy = initialMaxEnergy;
        
        float calculatedMaxHealth = initialMaxHealth;
        int pointsToSpend = targetLevel - 1;

        for (int i = 0; i < pointsToSpend; i++)
        {
            int roll = Random.Range(0, 6); 

            switch (roll)
            {
                case 0: strength += 1f; break;
                case 1: defence += 1f; break;
                case 2: regenSpeed += 0.5f; break;
                case 3: 
                    //Cap attack speed relative to base, or hard cap at 3.0
                    if (attackSpeed < 3.0f) attackSpeed += 0.1f;
                    else strength += 1f; 
                    break;
                case 4: calculatedMaxHealth += 10f; break;
                case 5: maxEnergy += 10f; break; 
            }
        }
        
        currentBaseAttackSpeed = attackSpeed;

        if (healthScript != null)
        {
            healthScript.setMaxHealth(calculatedMaxHealth);
            healthScript.setCurrentHealth(calculatedMaxHealth);
        }
    }

    //Reverse-engineer the current level from raw stat values
    public int GetLevel()
    {
        InitializeBaseStats();

        // Calculate upgrades based on the initial prefab values, not hardcoded 1s and 100s
        float strUpgrades = strength - initialStrength;
        float defUpgrades = defence - initialDefence;
        float regUpgrades = (regenSpeed - initialRegenSpeed) * 2;
        float atkUpgrades = (attackSpeed - initialAttackSpeed) * 10;
        float energyUpgrades = (maxEnergy - initialMaxEnergy) / 10f; 

        float healthUpgrades = 0;
        if (healthScript != null)
        {
            healthUpgrades = (healthScript.getMaxHealth() - initialMaxHealth) / 10f;
        }

        float totalUpgrades = strUpgrades + defUpgrades + regUpgrades + atkUpgrades + healthUpgrades + energyUpgrades;
        return 1 + (int)Mathf.Round(totalUpgrades);
    }

    public string GetTitle()
    {
        return $"Lv. {GetLevel()} {characterName}";
    }
    
    public void ApplyUpgrade(StatType type)
    {
        switch (type)
        {
            case StatType.Strength: strength += 1; break;
            case StatType.Defence: defence += 1; break;
            case StatType.AttackSpeed:
                attackSpeed += 0.1f; 
                currentBaseAttackSpeed = attackSpeed; 
                break;
            case StatType.Regen: regenSpeed += 0.5f; break;
            case StatType.Health:
                if (healthScript != null)
                {
                    float newMax = healthScript.getMaxHealth() + 50;
                    healthScript.setMaxHealth(newMax);
                    healthScript.currentHealth += 50; 
                }
                break;
        }
    }
    
    public float GetStatValue(StatType type)
    {
        switch (type)
        {
            case StatType.Strength: return strength;
            case StatType.Defence: return defence;
            case StatType.AttackSpeed: return attackSpeed;
            case StatType.Regen: return regenSpeed;
            case StatType.Energy: return maxEnergy;
            case StatType.Health: 
                return healthScript != null ? healthScript.getMaxHealth() : 0;
            default: return 0;
        }
    }
    
    public void ApplySlowEffect(float percentage)
    {
        attackSpeed = currentBaseAttackSpeed * (1.0f - percentage);
    }

    public void RemoveSlowEffect()
    {
        attackSpeed = currentBaseAttackSpeed;
    }
}