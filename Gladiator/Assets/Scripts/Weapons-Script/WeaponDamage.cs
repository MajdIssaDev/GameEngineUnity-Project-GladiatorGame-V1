using UnityEngine;
using System.Collections.Generic;

public class WeaponDamage : MonoBehaviour
{
    [Header("References")]
    public PlayerCombat playerCombat; // Assign the ATTACKER'S combat script
    [SerializeField] Stats ownerStats;
    
    [Header("Combat Stats")]
    public float damageAmount = 20;
    public float knockbackStrength = 5f;
    [HideInInspector] public bool isHeavyAttack = false; 
    
    private Collider myCollider;
    private List<GameObject> hitEnemies = new List<GameObject>();

    private void Start()
    {
        myCollider = GetComponent<BoxCollider>();
        myCollider.enabled = false;
        if (ownerStats == null) ownerStats = GetComponentInParent<Stats>();
        if (playerCombat == null) playerCombat = GetComponentInParent<PlayerCombat>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Don't hit yourself
        if (other.transform.root == transform.root) return;

        // Check if we hit something new
        if (hitEnemies.Contains(other.gameObject)) return;
        hitEnemies.Add(other.gameObject);

        // --- 1. HANDLE HIT REACTION & BLOCKING ---
        bool attackWasBlocked = false;

        // Try to find the HitReaction script on the victim (Player OR Enemy)
        HitReaction victimReaction = other.GetComponent<HitReaction>();
        
        if (victimReaction != null)
        {
            // Calculate EXACT hit point on the collider surface
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            Vector3 attackDir = (other.transform.position - transform.position).normalized;

            // Trigger the reaction and check if they blocked
            attackWasBlocked = victimReaction.HandleHit(hitPoint, attackDir);

            if (attackWasBlocked)
            {
                Debug.Log("Attack Blocked by " + other.name);
                // If THIS weapon belongs to the Player, trigger the Player's recoil
                if (playerCombat != null) playerCombat.TriggerRecoil();
                
                // Stop damage logic here
                return; 
            }
        }

        // --- 2. APPLY DAMAGE & KNOCKBACK (If not blocked) ---
        
        // Knockback
        ImpactReceiver enemyImpact = other.GetComponent<ImpactReceiver>();
        if (enemyImpact != null)
        {
            Vector3 pushDir = (other.transform.position - transform.position).normalized;
            pushDir.y = 0; // Keep horizontal
            enemyImpact.AddImpact(pushDir, knockbackStrength);
        }

        // Damage
        HealthScript health = other.GetComponent<HealthScript>();
        if (health != null)
        {
            float finalDamage = damageAmount;
            if (ownerStats != null) finalDamage += ownerStats.strength; // Simplified math for example
            
            if (isHeavyAttack) finalDamage *= 1.5f;

            health.takeDamage(finalDamage);
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