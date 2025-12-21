using UnityEngine;
using System.Collections.Generic;

public class WeaponDamage : MonoBehaviour
{
    [Header("Combat Stats")]
    public float damageAmount = 20;
    public float knockbackStrength = 5f;
    
    // --- NEW VARIABLE ---
    [HideInInspector] 
    public bool isHeavyAttack = false; // PlayerCombat will toggle this
    
    [SerializeField] Stats ownerStats;
    private Collider myCollider;
    private List<GameObject> hitEnemies = new List<GameObject>();

    private void Start()
    {
        myCollider = GetComponent<BoxCollider>();
        myCollider.enabled = false;
        
        if (ownerStats == null) ownerStats = GetComponentInParent<Stats>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == this.gameObject) return;

        if (other.CompareTag("Enemy"))
        {
            if (hitEnemies.Contains(other.gameObject)) return;
            hitEnemies.Add(other.gameObject);

            // --- Knockback Logic ---
            ImpactReceiver enemyImpact = other.GetComponent<ImpactReceiver>();
            if (enemyImpact != null)
            {
                Vector3 pushDirection = other.transform.position - transform.position;
                pushDirection.y = 0;
                pushDirection.Normalize();
                enemyImpact.AddImpact(pushDirection, knockbackStrength);
            }

            // --- Damage Logic ---
            HealthScript healthScript = other.gameObject.GetComponent<HealthScript>();
            if (healthScript != null)
            {
                // 1. Calculate Standard Damage
                float finalDamage = damageAmount * (1 + (ownerStats.strength / 10));

                // 2. CHECK: If Heavy, multiply by 1.5 (50% more)
                if (isHeavyAttack)
                {
                    finalDamage *= 1.5f;
                    Debug.Log("BOOM! Heavy Hit!"); // Optional: Check console to confirm
                }

                healthScript.takeDamage(finalDamage);
            }
        }
    }
    
    public void EnableHitbox() 
    {
        hitEnemies.Clear(); 
        myCollider.enabled = true;
    }

    public void DisableHitbox() 
    {
        myCollider.enabled = false;
    }
}