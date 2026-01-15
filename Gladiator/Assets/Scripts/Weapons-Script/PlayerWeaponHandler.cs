using UnityEngine;
using System.Collections.Generic;

public class PlayerWeaponHandler : MonoBehaviour
{
    [Header("Dependencies")]
    public Transform weaponSocket;
    public PlayerCombat playerCombat;

    [Header("Testing")]
    // CHANGE 1: We now use the Data file instead of the raw Prefab
    public WeaponData weaponToEquip; 

    [System.Serializable]
    public struct WeaponOffsetConfig
    {
        public string weaponTag;
        public Vector3 positionOffset;
        public Vector3 rotationOffset;
    }

    [Header("Weapon Alignment Settings")]
    public List<WeaponOffsetConfig> weaponOffsets;

    private GameObject currentWeaponInstance;

    void Start()
    {
        // Testing logic
        if (weaponToEquip != null) EquipWeapon(weaponToEquip);
    }

    // CHANGE 2: Function now accepts WeaponData
    public void EquipWeapon(WeaponData newWeaponData)
    {
        if (newWeaponData == null) return;

        // Cleanup old weapon
        if (currentWeaponInstance != null) Destroy(currentWeaponInstance);

        // Spawn weapon using the PREFAB from the data
        currentWeaponInstance = Instantiate(newWeaponData.weaponPrefab, weaponSocket.position, weaponSocket.rotation);
        currentWeaponInstance.transform.SetParent(weaponSocket);

        // ---------------------------------------------------------
        // OFFSET LOGIC (Still works the same)
        // ---------------------------------------------------------
        
        Vector3 finalPos = Vector3.zero;
        Quaternion finalRot = Quaternion.identity;

        // We check the tag of the instantiated object
        WeaponOffsetConfig config = weaponOffsets.Find(x => x.weaponTag == currentWeaponInstance.tag);

        if (!string.IsNullOrEmpty(config.weaponTag))
        {
            finalPos = config.positionOffset;
            finalRot = Quaternion.Euler(config.rotationOffset);
        }

        currentWeaponInstance.transform.localPosition = finalPos;
        currentWeaponInstance.transform.localRotation = finalRot;

        // ---------------------------------------------------------
        // CHANGE 3: THE FIX
        // Now we can pass BOTH the physical object AND the animator override
        // ---------------------------------------------------------
        if (playerCombat != null)
        {
            // This fixes your "takes 2 parameters but sending 1" error
            playerCombat.EquipNewWeapon(currentWeaponInstance, newWeaponData.animatorOverride);
        }
        
        WeaponDamage damageScript = currentWeaponInstance.GetComponent<WeaponDamage>();
        if (damageScript != null)
        {
            damageScript.Initialize(newWeaponData);
        }
    }
}