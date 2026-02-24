using UnityEngine;

public class CombatAnimationEvents : MonoBehaviour
{
    //Using single script that handles animation events for both Player and Enemies without duplicating code
    private ICombatReceiver combatReceiver;
    
    //The specific weapon currently held by this character
    private WeaponDamage currentWeaponScript;
    
    private void Awake()
    {
        //This will find PlayerCombat OR EnemyCombat as long as they implement the interface
        combatReceiver = GetComponentInParent<ICombatReceiver>();
    }

    //Dynamically update the weapon reference when a new weapon is equipped so hitboxes always match the current gear
    public void SetCurrentWeapon(WeaponDamage newWeapon)
    {
        currentWeaponScript = newWeapon;
    }

    //--- ANIMATION EVENTS ---

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
        //1. Enable Hitbox
        if (currentWeaponScript != null) 
            currentWeaponScript.EnableHitbox();

        //2. Make nearby enemies evaluate a dodge reaction when the player swings
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