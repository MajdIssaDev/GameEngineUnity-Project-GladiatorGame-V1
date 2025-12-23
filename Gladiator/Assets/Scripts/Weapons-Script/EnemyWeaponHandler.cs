using UnityEngine;
using System.Collections.Generic;

public class EnemyWeaponHandler : MonoBehaviour
{
    [Header("Dependencies")]
    public Transform rightHandSocket; 
    public Animator enemyAnimator;    
    
    // --- NEW: Reference to the Combat Script ---
    public EnemyCombat enemyCombat; 
    // ------------------------------------------

    [System.Serializable]
    public struct WeaponOffsetConfig
    {
        public string weaponTag;
        public Vector3 positionOffset;
        public Vector3 rotationOffset;
    }

    [Header("Alignment Settings")]
    public List<WeaponOffsetConfig> weaponOffsets;

    private GameObject currentWeaponInstance;

    public void EquipWeapon(WeaponData weaponData)
    {
        if (weaponData == null) return;
        if (rightHandSocket == null)
        {
            Debug.LogError("Enemy doesn't have a Right Hand Socket assigned!");
            return;
        }

        // 1. Cleanup old weapon if existing
        if (currentWeaponInstance != null) Destroy(currentWeaponInstance);

        // 2. Instantiate and Parent
        currentWeaponInstance = Instantiate(weaponData.weaponPrefab, rightHandSocket.position, rightHandSocket.rotation);
        currentWeaponInstance.transform.SetParent(rightHandSocket);

        // 3. Apply Offsets
        Vector3 finalPos = Vector3.zero;
        Quaternion finalRot = Quaternion.identity;

        WeaponOffsetConfig config = weaponOffsets.Find(x => x.weaponTag == currentWeaponInstance.tag);

        if (!string.IsNullOrEmpty(config.weaponTag))
        {
            finalPos = config.positionOffset;
            finalRot = Quaternion.Euler(config.rotationOffset);
        }

        currentWeaponInstance.transform.localPosition = finalPos;
        currentWeaponInstance.transform.localRotation = finalRot;

        // 4. Update Animator
        if (enemyAnimator != null && weaponData.animatorOverride != null)
        {
            enemyAnimator.runtimeAnimatorController = weaponData.animatorOverride;
        }

        // --- NEW: LINK WEAPON TO COMBAT SCRIPT ---
        WeaponDamage damageScript = currentWeaponInstance.GetComponent<WeaponDamage>();
        
        if (enemyCombat != null && damageScript != null)
        {
            // Just pass the weapon so EnemyCombat can turn Hitboxes On/Off
            enemyCombat.SetWeapon(damageScript);
        }
    }
}