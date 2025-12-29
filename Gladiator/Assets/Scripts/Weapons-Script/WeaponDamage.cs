using UnityEngine;
using System.Collections.Generic;

public class WeaponDamage : MonoBehaviour
{
    [Header("References")]
    public PlayerCombat playerCombat;
    [SerializeField] Stats ownerStats;
    
    [Header("Settings")]
    public float damageAmount = 20;
    public float knockbackStrength = 5f;
    [HideInInspector] public bool isHeavyAttack = false;

    private Collider myCollider;
    
    // Track Hitboxes (Bones) separately from Enemies (Health)
    private List<Collider> hitParts = new List<Collider>(); 
    private List<GameObject> damagedEnemies = new List<GameObject>(); 

    private void Start()
    {
        myCollider = GetComponent<BoxCollider>();
        myCollider.enabled = false;
        if (ownerStats == null) ownerStats = GetComponentInParent<Stats>();
        if (playerCombat == null) playerCombat = GetComponentInParent<PlayerCombat>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Ignore self
        if (other.transform.root == transform.root) return;
        if (transform.root.CompareTag(other.transform.root.tag)) return; //so enemies dont damage enemies

        // --- 1. TRACK PARTS (Visuals) ---
        // If we already hit THIS specific arm collider, skip it.
        if (hitParts.Contains(other)) return;
        hitParts.Add(other);

        HitReaction reaction = other.GetComponentInParent<HitReaction>();
        
        if (reaction != null)
        {
            // Pass the collider 'other' so the script knows EXACTLY which bone we hit
            // (We wrote this support in the previous step)
            reaction.HandleHit(other, other.ClosestPoint(transform.position), transform.forward);
        }

        // --- 2. TRACK DAMAGE (Health) ---
        GameObject enemyRoot = other.transform.root.gameObject;
        
        // Only damage the enemy if we haven't damaged this specific enemy instance yet
        if (!damagedEnemies.Contains(enemyRoot))
        {
            damagedEnemies.Add(enemyRoot);
            
            // Apply Damage
            HealthScript health = other.GetComponentInParent<HealthScript>();
            if (health != null)
            {
                if (health.isInvincible) return;
                
                float finalDamage = isHeavyAttack ? damageAmount * 1.5f : damageAmount;
                if (ownerStats != null) finalDamage += ownerStats.strength;
                health.takeDamage(finalDamage);
            }

            // Apply Knockback
            ImpactReceiver enemyImpact = other.GetComponentInParent<ImpactReceiver>();
            if (enemyImpact != null)
            {
                Vector3 pushDir = (other.transform.position - transform.position).normalized;
                pushDir.y = 0;
                enemyImpact.AddImpact(pushDir, knockbackStrength);
            }
        }
    }

    public void EnableHitbox() 
    {
        hitParts.Clear();
        damagedEnemies.Clear();
        myCollider.enabled = true;
    }

    public void DisableHitbox()
    {
        // Add this check!
        // If the damageCollider variable is null, return immediately to prevent crash.
        if (myCollider == null) return; 

        myCollider.enabled = false; 
    }
}