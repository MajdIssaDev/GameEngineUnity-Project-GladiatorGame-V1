using UnityEngine;

public enum StatType { Strength, Defence, AttackSpeed, Regen, Health }

public class Stats : MonoBehaviour
{
    [Header("Identity")]
    public string characterName = "";

    [Header("Stats")] 
    public float strength = 1;      // +1 per upgrade
    public float defence = 0;       // +1 per upgrade
    public float regenSpeed = 1;    // +0.5 per upgrade
    public float attackSpeed = 1;   // +0.1 per upgrade
    
    [Header("Dependencies")]
    public HealthScript healthScript; 
    private float baseHealth = 100f; // ASSUMPTION: Level 1 has 100 HP
    
    private void Start()
    {
        if (healthScript == null) healthScript = GetComponent<HealthScript>();
    }

    // --- NEW FUNCTION: SCALES ENEMY TO TARGET LEVEL ---
    public void SetLevel(int targetLevel)
    {
        // 1. Reset everything to Base Level 1 stats first
        strength = 1;
        defence = 0;
        regenSpeed = 1;
        attackSpeed = 1;
        
        // Reset Health
        float calculatedMaxHealth = baseHealth; // Starts at 100

        // 2. Calculate how many upgrades we get
        // Level 1 = 0 upgrades. Level 5 = 4 upgrades.
        int pointsToSpend = targetLevel - 1;

        // 3. Randomly distribute the points
        for (int i = 0; i < pointsToSpend; i++)
        {
            // Roll a dice: 0=Str, 1=Def, 2=Regen, 3=AtkSpd, 4=Health
            int roll = Random.Range(0, 5); 

            switch (roll)
            {
                case 0: // Strength (+1)
                    strength += 1f;
                    break;
                case 1: // Defence (+1)
                    defence += 1f;
                    break;
                case 2: // Regen (+0.5)
                    regenSpeed += 0.5f;
                    break;
                case 3: // Attack Speed (+0.1)
                    // Cap attack speed so animations don't break (optional limit e.g. 3.0f)
                    if (attackSpeed < 3.0f) 
                        attackSpeed += 0.1f;
                    else 
                        // If capped, refund the point into Strength instead
                        strength += 1f; 
                    break;
                case 4: // Health (+10)
                    calculatedMaxHealth += 10f;
                    break;
            }
        }

        // 4. Apply the calculated Health to the HealthScript
        if (healthScript != null)
        {
            healthScript.setMaxHealth(calculatedMaxHealth);
            healthScript.setCurrentHealth(calculatedMaxHealth); // Heal to full
        }
    }

    public int GetLevel()
    {
        // 1. Calculate Standard Stat Upgrades
        float strUpgrades = strength - 1;
        float defUpgrades = defence;
        float regUpgrades = (regenSpeed - 1) * 2;
        float atkUpgrades = (attackSpeed - 1) * 10;

        // 2. Calculate Health Upgrades
        float healthUpgrades = 0;
        if (healthScript != null)
        {
            healthUpgrades = (healthScript.getMaxHealth() - baseHealth) / 10f;
        }

        // 3. Sum total
        float totalUpgrades = strUpgrades + defUpgrades + regUpgrades + atkUpgrades + healthUpgrades;

        // 4. Return Level (1 + Total)
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
                break;
                
            case StatType.Regen:
                regenSpeed += 0.5f;
                break;
                
            case StatType.Health:
                // Health is special because it lives in HealthScript
                if (healthScript != null)
                {
                    float newMax = healthScript.getMaxHealth() + 10;
                    healthScript.setMaxHealth(newMax);
                    
                    // Optional: Heal the player for the amount upgraded?
                    healthScript.currentHealth += 10; 
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
            case StatType.Health: 
                return healthScript != null ? healthScript.getMaxHealth() : 0;
            default: return 0;
        }
    }
}