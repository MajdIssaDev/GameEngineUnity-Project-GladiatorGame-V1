using UnityEngine;
using System.Collections.Generic;

public class PlayerWeaponHandler : MonoBehaviour
{
    [Header("Dependencies")]
    public Transform weaponSocket;
    public PlayerCombat playerCombat;

    [Header("Testing")]
    public GameObject weaponToEquip; 

    // 1. DEFINE A STRUCT TO HOLD DATA
    [System.Serializable]
    public struct WeaponOffsetConfig
    {
        public string weaponTag;
        public Vector3 positionOffset;
        public Vector3 rotationOffset;
    }

    // 2. CREATE A LIST TO EDIT IN INSPECTOR
    [Header("Weapon Alignment Settings")]
    public List<WeaponOffsetConfig> weaponOffsets;

    private GameObject currentWeaponInstance;

    void Start()
    {
        if (weaponToEquip != null) EquipWeapon(weaponToEquip);
    }

    public void EquipWeapon(GameObject weaponPrefab)
    {
        // Cleanup old weapon
        if (currentWeaponInstance != null) Destroy(currentWeaponInstance);

        // Spawn weapon
        currentWeaponInstance = Instantiate(weaponPrefab, weaponSocket.position, weaponSocket.rotation);
        currentWeaponInstance.transform.SetParent(weaponSocket);

        // ---------------------------------------------------------
        // 3. APPLY CUSTOM OFFSETS BASED ON TAG
        // ---------------------------------------------------------
        
        // Default values
        Vector3 finalPos = Vector3.zero;
        Quaternion finalRot = Quaternion.identity;

        // Search the list for a matching tag
        WeaponOffsetConfig config = weaponOffsets.Find(x => x.weaponTag == weaponPrefab.tag);

        // If we found a match (checking if the tag is not null/empty to verify)
        if (!string.IsNullOrEmpty(config.weaponTag))
        {
            finalPos = config.positionOffset;
            finalRot = Quaternion.Euler(config.rotationOffset);
        }

        // Apply the settings
        currentWeaponInstance.transform.localPosition = finalPos;
        currentWeaponInstance.transform.localRotation = finalRot;

        // ---------------------------------------------------------

        // Notify combat script
        playerCombat.EquipNewWeapon(currentWeaponInstance);
    }
}