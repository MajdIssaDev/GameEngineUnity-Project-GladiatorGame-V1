using UnityEngine;
using System.Collections.Generic;

public class PlayerWeaponHandler : MonoBehaviour
{
    [Header("Dependencies")]
    public Transform weaponSocket;
    public PlayerCombat playerCombat;

    [Header("Testing")]
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
        //Testing logic
        if (weaponToEquip != null) EquipWeapon(weaponToEquip);
    }

    public void EquipWeapon(WeaponData newWeaponData)
    {
        if (newWeaponData == null) return;

        //Cleanup old weapon
        if (currentWeaponInstance != null) Destroy(currentWeaponInstance);

        //Spawn weapon using the PREFAB from the data
        currentWeaponInstance = Instantiate(newWeaponData.weaponPrefab, weaponSocket.position, weaponSocket.rotation);
        currentWeaponInstance.transform.SetParent(weaponSocket);

       
        Vector3 finalPos = Vector3.zero;
        Quaternion finalRot = Quaternion.identity;

        //We check the tag of the instantiated object
        WeaponOffsetConfig config = weaponOffsets.Find(x => x.weaponTag == currentWeaponInstance.tag);

        if (!string.IsNullOrEmpty(config.weaponTag))
        {
            finalPos = config.positionOffset;
            finalRot = Quaternion.Euler(config.rotationOffset);
        }

        currentWeaponInstance.transform.localPosition = finalPos;
        currentWeaponInstance.transform.localRotation = finalRot;


        if (playerCombat != null)
        {
            playerCombat.EquipNewWeapon(currentWeaponInstance, newWeaponData.animatorOverride);
        }
        
        WeaponDamage damageScript = currentWeaponInstance.GetComponent<WeaponDamage>();
        if (damageScript != null)
        {
            damageScript.Initialize(newWeaponData);
        }
    }
}