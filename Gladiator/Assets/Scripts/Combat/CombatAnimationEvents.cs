using UnityEngine;

public class CombatAnimationEvents : MonoBehaviour
{
    // We use the Interface here, so it works for Player OR Enemy
    private ICombatReceiver combatReceiver;
    
    // The specific weapon currently held by this character
    private WeaponDamage currentWeaponScript;

    private void Awake()
    {
        // This will find PlayerCombat OR EnemyCombat, as long as they implement the interface
        combatReceiver = GetComponentInParent<ICombatReceiver>();
    }

    // Call this from your Player/Enemy script when they equip a weapon
    public void SetCurrentWeapon(WeaponDamage newWeapon)
    {
        currentWeaponScript = newWeapon;
    }

    // --- ANIMATION EVENTS ---

    public void OpenComboWindow()
    {
        if (combatReceiver != null)
        {
            combatReceiver.OnComboWindowOpen();
        }
    }

    public void FinishAttack()
    {
        if (combatReceiver != null)
        {
            combatReceiver.OnFinishAttack();
        }
    }

    public void OpenDamageWindow()
    {
        // 1. Enable Hitbox (Generic for both)
        if (currentWeaponScript != null) 
            currentWeaponScript.EnableHitbox();

        // 2. Player-Specific Logic (Scare Enemies)
        // We keep the tag check here. If this script is on an Enemy, 
        // compareTag("Player") returns false, so enemies won't scare other enemies.
        if (transform.CompareTag("Player"))
        {
            Collider[] hitEnemies = Physics.OverlapSphere(transform.position, 5f);
            
            foreach (var hit in hitEnemies)
            {
                EnemyCombat enemy = hit.GetComponent<EnemyCombat>();
                if (enemy != null)
                {
                    enemy.ReactToIncomingAttack();
                }
            }
        }
    }

    public void CloseDamageWindow()
    {
        if (currentWeaponScript != null) 
        {
            currentWeaponScript.DisableHitbox();
        }
    }
}