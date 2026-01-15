using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Combat/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Shop Info")]
    public string weaponName;      
    public int price;              

    [Header("Difficulty Settings")]
    [Tooltip("1 = Early Game, 2 = Mid Game, etc.")]
    public int tier = 1;

    [Header("Visuals")]
    public GameObject weaponPrefab; 
    public AnimatorOverrideController animatorOverride; 
    
    [Header("Audio")]
    public AudioClip[] hitSounds;
}