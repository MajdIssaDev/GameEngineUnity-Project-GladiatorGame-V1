using UnityEngine;
using System.Collections.Generic;

public class EnemyWeaponHandler : MonoBehaviour
{
    [Header("Dependencies")]
    public Transform rightHandSocket;
    public Animator enemyAnimator;
    //1. New Reference to the Combat Script
    public EnemyCombat combatScript;

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

    void Awake()
    { 
        //Automatically find the combat script if not assigned
        if (combatScript == null) 
            combatScript = GetComponent<EnemyCombat>();
            
        if (enemyAnimator == null)
            enemyAnimator = GetComponent<Animator>();
    }

    //Called ONLY by GameManager now
    public void EquipWeapon(WeaponData weaponData)
    {
        if (weaponData == null) return;
        if (rightHandSocket == null)
        {
            Debug.LogError("Enemy doesn't have a Right Hand Socket assigned!");
            return;
        }

        //1. Cleanup old weapon
        if (currentWeaponInstance != null) Destroy(currentWeaponInstance);

        //2. Instantiate and Parent
        if (weaponData.weaponPrefab != null)
        {
            currentWeaponInstance = Instantiate(weaponData.weaponPrefab, rightHandSocket.position, rightHandSocket.rotation);
            currentWeaponInstance.transform.SetParent(rightHandSocket);
        }
        else
        {
            return;
        }

        //3. Apply Offsets
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

        //4. Update Animator
        if (enemyAnimator != null && weaponData.animatorOverride != null)
        {
            enemyAnimator.runtimeAnimatorController = weaponData.animatorOverride;
        }
        
        //5. UPDATE ENEMY COMBAT [NEW]
        //We get the specific script component from the new weapon object
        WeaponDamage newWeaponScript = currentWeaponInstance.GetComponent<WeaponDamage>();
        
        if (combatScript != null && newWeaponScript != null)
        {
            combatScript.SetWeapon(newWeaponScript);
            newWeaponScript.Initialize(weaponData);
        }
    }
}