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
    
    [Header("Audio")]
    public AudioClip[] hitSounds;

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

        // --- FIX START: Define a safe hit point to use everywhere ---
        // We use the weapon's current position as the impact point.
        // This avoids the "Convex MeshCollider" error entirely.
        Vector3 safeHitPoint = transform.position;
        // --------------------------------------------------------

        // --- 1. TRACK PARTS (Visuals) ---
        // If we already hit THIS specific arm collider, skip it.
        if (hitParts.Contains(other)) return;
        hitParts.Add(other);

        HitReaction reaction = other.GetComponentInParent<HitReaction>();
        
        if (reaction != null)
        {
            // Pass the safeHitPoint instead of calculating ClosestPoint
            reaction.HandleHit(other, safeHitPoint, transform.forward);
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
                
                // Use the safeHitPoint for sound as well
                PlayHitSound(safeHitPoint);
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
    
    public void Initialize(WeaponData data)
    {
        // If the specific weapon has sounds, use them.
        if (data.hitSounds != null && data.hitSounds.Length > 0)
        {
            this.hitSounds = data.hitSounds;
        }
    }
    
    void PlayHitSound(Vector3 position)
    {
        if (hitSounds.Length > 0)
        {
            int index = Random.Range(0, hitSounds.Length);
            
            // AudioSource.PlayClipAtPoint creates a temporary object at that spot to play the sound
            AudioSource.PlayClipAtPoint(hitSounds[index], position);
        }
    }
}