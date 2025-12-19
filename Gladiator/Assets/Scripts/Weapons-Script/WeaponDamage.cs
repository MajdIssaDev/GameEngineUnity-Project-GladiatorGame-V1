using UnityEngine;

public class WeaponDamage : MonoBehaviour
{
    [Header("Combat Stats")]
    public int damageAmount = 20;
    public float knockbackStrength = 5f; // How hard this weapon hits
    
    [SerializeField] Stats ownerStats;
    private Collider myCollider;

    private void Start()
    {
        myCollider = GetComponent<BoxCollider>();
        myCollider.enabled = false;
        // 1. Look up the hierarchy to find the Stats script on the Player or Enemy
        if (ownerStats == null)
        {
            ownerStats = GetComponentInParent<Stats>();
        }

        // 2. Debug check to ensure it was found
        if (ownerStats == null)
        {
            Debug.LogWarning($"Weapon {gameObject.name} could not find a Stats script in its parents!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("I touched: " + other.gameObject.name);
        // 1. Check if it's an enemy
        if (other.gameObject != this.gameObject)
        {
            if (other.CompareTag("Enemy"))
            {
                // Deal Damage (Custom logic later)
                Debug.Log("Hit Enemy!");

                ImpactReceiver enemyImpact = other.GetComponent<ImpactReceiver>();

                if (enemyImpact != null)
                {
                    // 1. Calculate the raw direction
                    Vector3 pushDirection = other.transform.position - transform.position;

                    // 2. THE FIX: Kill the Y (Vertical) component
                    pushDirection.y = 0;

                    // 3. Normalize it (Make the length 1.0)
                    pushDirection.Normalize();

                    // 4. Send the force
                    enemyImpact.AddImpact(pushDirection, knockbackStrength);
                }

                HealthScript healthScript = other.gameObject.GetComponent<HealthScript>();

                if (healthScript != null)
                {
                    healthScript.takeDamage(damageAmount * (1+(ownerStats.strength/10)));  //Example 10 strength =  double damage
                }
            }
        }
    }
    
    public void EnableHitbox() => myCollider.enabled = true;
    public void DisableHitbox() => myCollider.enabled = false;
}