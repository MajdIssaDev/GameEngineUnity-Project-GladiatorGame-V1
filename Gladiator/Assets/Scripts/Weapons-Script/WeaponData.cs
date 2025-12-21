using UnityEngine;

// This adds a menu option: Right Click -> Create -> Combat -> Weapon Data
[CreateAssetMenu(fileName = "New Weapon", menuName = "Combat/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Shop Info")]
    public string weaponName;      // e.g., "Spartan Spear"
    public int price;              // e.g., 500 Gold

    [Header("Visuals")]
    public GameObject weaponPrefab; // DRAG YOUR "Spear1h" PREFAB HERE
    
    // The animation file. Leave empty for Axe, assign "Spear_Animator" for Spear.
    public AnimatorOverrideController animatorOverride; 
}