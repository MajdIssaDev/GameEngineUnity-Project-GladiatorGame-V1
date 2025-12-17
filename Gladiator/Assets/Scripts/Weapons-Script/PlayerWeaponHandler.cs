using UnityEngine;

public class PlayerWeaponHandler : MonoBehaviour
{
    [Header("Setup")]
    // Drag your 'WeaponSocket' object (under the hand bone) here
    public Transform weaponSocket;

    [Header("Testing Only")]
    // Drag your Weapon Prefab (from Project window) here to test
    public GameObject weaponToEquip; 

    private GameObject currentWeaponInstance;

    void Start()
    {
        // For testing: If we put a weapon in the slot, equip it immediately
        if (weaponToEquip != null)
        {
            EquipWeapon(weaponToEquip);
        }
    }

    // We make this public so other scripts (like your future UI/Shop) can call it
    public void EquipWeapon(GameObject weaponPrefab)
    {
        // 1. If we are already holding something, destroy it first
        if (currentWeaponInstance != null)
        {
            Destroy(currentWeaponInstance);
        }

        // 2. Spawn the new weapon
        // We instantiate it at the socket's position and rotation
        currentWeaponInstance = Instantiate(weaponPrefab, weaponSocket.position, weaponSocket.rotation);

        // 3. "Glue" it to the socket
        currentWeaponInstance.transform.SetParent(weaponSocket);

        // 4. Reset local coordinates just in case
        // This ensures the handle snaps exactly to the socket center
        currentWeaponInstance.transform.localPosition = Vector3.zero;
        currentWeaponInstance.transform.localRotation = Quaternion.identity;
    }
}