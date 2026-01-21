using UnityEngine;
using System.Collections.Generic;

public class WeaponDamage : MonoBehaviour
{
    [Header("References")]
    public PlayerCombat playerCombat;
    [SerializeField] Stats ownerStats;
    public GameObject characterHolder; 
    
    [Header("Settings")]
    public float damageAmount = 20;
    public float knockbackStrength = 5f;
    [HideInInspector] public bool isHeavyAttack = false;

    [Header("Audio")]
    public AudioClip[] hitSounds; 
    public AudioClip swingSound;
    public AudioClip blockSound;  
    public AudioClip parrySound;  

    [Header("Visuals")]
    public TrailRenderer weaponTrail;
    
    private Collider myCollider;
    private List<Collider> hitParts = new List<Collider>(); 
    private List<GameObject> damagedEnemies = new List<GameObject>(); 

    private void Start()
    {
        myCollider = GetComponent<BoxCollider>();
        if (myCollider != null) myCollider.enabled = false;

        if (ownerStats == null) ownerStats = GetComponentInParent<Stats>();
        if (playerCombat == null) playerCombat = GetComponentInParent<PlayerCombat>();
        
        if (characterHolder == null)
        {
            if (ownerStats != null) characterHolder = ownerStats.gameObject;
            else characterHolder = transform.root.gameObject;
        }

        if (weaponTrail != null)
        {
            weaponTrail.emitting = false; 
            weaponTrail.Clear();          
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. Safety Checks
        if (characterHolder != null)
        {
            if (other.gameObject == characterHolder) return;
            if (other.transform.root == characterHolder.transform) return;
        }
        else if (other.transform.root == transform.root) return;

        if (transform.root.CompareTag(other.transform.root.tag)) return; 

        Vector3 safeHitPoint = transform.position;

        // Prevent hitting the exact same bone twice
        if (hitParts.Contains(other)) return;
        hitParts.Add(other);
        
        // --- NEW: Check if we already hit this Enemy Root ---
        GameObject enemyRoot = other.transform.root.gameObject;
        bool isFirstHitOnEnemy = !damagedEnemies.Contains(enemyRoot);
        // ---------------------------------------------------

        // 2. Determine Outcome
        HealthScript health = other.GetComponentInParent<HealthScript>();
        HitReaction reaction = other.GetComponentInParent<HitReaction>();
        DefenseType defense = DefenseType.None;

        if (health != null)
        {
            Vector3 attackerPos = (characterHolder != null) ? characterHolder.transform.position : transform.position;
            defense = health.CheckDefense(attackerPos);
        }

        // 3. Handle Visuals & SOUNDS
        if (reaction != null)
        {
            switch (defense)
            {
                case DefenseType.Parry:
                    reaction.PlayParryVFX(safeHitPoint, transform.forward);
                    // Only play sound if this is the first interaction with this enemy
                    if (isFirstHitOnEnemy) PlaySoundEffect(parrySound, safeHitPoint); 
                    break;

                case DefenseType.Block:
                    reaction.PlayBlockVFX(safeHitPoint, transform.forward);
                    if (isFirstHitOnEnemy) PlaySoundEffect(blockSound, safeHitPoint);
                    break;

                case DefenseType.None:
                    reaction.HandleHit(other, safeHitPoint, transform.forward);
                    if (isFirstHitOnEnemy) PlayRandomHitSound(safeHitPoint);
                    break;
            }
        }
        else 
        {
            // Fallback for objects without HitReaction
            if (isFirstHitOnEnemy) PlayRandomHitSound(safeHitPoint);
        }

        // 4. Apply Damage Logic
        if (isFirstHitOnEnemy)
        {
            damagedEnemies.Add(enemyRoot); // Mark enemy as processed
            
            if (health != null)
            {
                if (health.isInvincible) return;
                
                // 1. Calculate Base Damage (Light vs Heavy)
                float baseDamage = isHeavyAttack ? damageAmount * 1.5f : damageAmount;
                
                // 2. Apply Strength Multiplier
                // Formula: 1 Point = +10% Damage. 
                // Example: Strength 5 = 50% boost -> Multiplier 1.5x
                float finalDamage = baseDamage;
                
                if (ownerStats != null) 
                {
                    float strengthMod = 1.0f + (ownerStats.strength * 0.10f);
                    finalDamage *= strengthMod;
                }
                
                // 3. Deal the Damage
                GameObject attackerRef = (characterHolder != null) ? characterHolder : transform.root.gameObject;
                health.takeDamage(finalDamage, attackerRef); 
            }

            // Apply Knockback only on non-defense hits
            if (defense == DefenseType.None)
            {
                ImpactReceiver enemyImpact = other.GetComponentInParent<ImpactReceiver>();
                if (enemyImpact != null)
                {
                    Vector3 pushDir = (other.transform.position - transform.position).normalized;
                    pushDir.y = 0;
                    enemyImpact.AddImpact(pushDir, knockbackStrength);
                }
            }
        }
    }

    // --- HELPERS ---

    void PlaySoundEffect(AudioClip clip, Vector3 position)
    {
        if (clip == null) return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(clip, position);
        }
        else 
        {
            AudioSource.PlayClipAtPoint(clip, position);
        }
    }

    void PlayRandomHitSound(Vector3 position)
    {
        if (hitSounds != null && hitSounds.Length > 0)
        {
            int index = Random.Range(0, hitSounds.Length);
            PlaySoundEffect(hitSounds[index], position);
        }
    }

    public void EnableHitbox() 
    {
        hitParts.Clear();
        damagedEnemies.Clear(); // Reset the list so we can hit them again next swing
        
        if (myCollider != null) myCollider.enabled = true;
        if (weaponTrail != null) weaponTrail.emitting = true;
        PlaySoundEffect(swingSound, transform.position); 
    }

    public void DisableHitbox()
    {
        if (myCollider != null) myCollider.enabled = false; 
        if (weaponTrail != null) weaponTrail.emitting = false;
    }
    
    public void Initialize(WeaponData data)
    {
        if (data.hitSounds != null && data.hitSounds.Length > 0) this.hitSounds = data.hitSounds;
        if (data.swingSound != null) this.swingSound = data.swingSound;
        if (data.blockSound != null) this.blockSound = data.blockSound;
        if (data.parrySound != null) this.parrySound = data.parrySound;
    }
}