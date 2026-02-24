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
    private float baseHealth = 100f; 

    private float baseAttackSpeed;
    
    private void Start()
    {
        if (healthScript == null) healthScript = GetComponent<HealthScript>();
        
        //Cache the base attack speed to prevent permanent stat degradation when applying temporary debuffs
        baseAttackSpeed = attackSpeed; 
    }

    //Procedurally generate enemy stats based on the current wave round
    public void SetLevel(int targetLevel)
    {
        strength = 1;
        defence = 0;
        regenSpeed = 1;
        attackSpeed = 1;
        maxEnergy = 100f; 

        baseAttackSpeed = 1; 

        float calculatedMaxHealth = baseHealth;
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
                    //Cap attack speed at 3.0 to prevent animation clipping
                    if (attackSpeed < 3.0f) attackSpeed += 0.1f;
                    else strength += 1f; 
                    break;
                case 4: calculatedMaxHealth += 10f; break;
                case 5: maxEnergy += 10f; break; 
            }
        }
        
        baseAttackSpeed = attackSpeed;

        if (healthScript != null)
        {
            healthScript.setMaxHealth(calculatedMaxHealth);
            healthScript.setCurrentHealth(calculatedMaxHealth);
        }
    }

    //Reverse-engineer the current level from raw stat values
    public int GetLevel()
    {
        float strUpgrades = strength - 1;
        float defUpgrades = defence;
        float regUpgrades = (regenSpeed - 1) * 2;
        float atkUpgrades = (attackSpeed - 1) * 10;
        float energyUpgrades = (maxEnergy - 100) / 10f; 

        float healthUpgrades = 0;
        if (healthScript != null)
        {
            healthUpgrades = (healthScript.getMaxHealth() - baseHealth) / 10f;
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
            case StatType.Strength:
                strength += 1;
                break;
                
            case StatType.Defence:
                defence += 1;
                break;
                
            case StatType.AttackSpeed:
                attackSpeed += 0.1f; 
                baseAttackSpeed = attackSpeed; 
                break;
                
            case StatType.Regen:
                regenSpeed += 0.5f;
                break;
                
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
        //Calculate debuffs against the cached base speed; it prevents exponential stacking
        attackSpeed = baseAttackSpeed * (1.0f - percentage);
    }

    public void RemoveSlowEffect()
    {
        //Restore the original speed from the cache to cleanly end the debuff state
        attackSpeed = baseAttackSpeed;
    }
}