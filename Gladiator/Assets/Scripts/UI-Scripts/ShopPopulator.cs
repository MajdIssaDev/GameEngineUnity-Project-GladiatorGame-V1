using UnityEngine;
using System.Collections.Generic;

public class ShopPopulator : MonoBehaviour
{
    [Header("Configuration")]
    public GameObject buttonPrefab; 
    public Transform gridContainer; 
    
    [Header("Inventory")]
    //A list of ScriptableObjects representing the inventory
    public List<WeaponData> weaponsToSell; 

    private void Start()
    {
        GenerateShop();
    }

    void GenerateShop()
    {
        //Destroy any placeholder or leftover buttons in the grid to prevent memory leaks and duplicate items
        foreach (Transform child in gridContainer) {
            Destroy(child.gameObject);
        }

        //Instantiate a new UI button for every weapon in the inventory list
        foreach (WeaponData weapon in weaponsToSell)
        {
            GameObject newButton = Instantiate(buttonPrefab, gridContainer);
            
            //Failsafe: Ensure the instantiated prefab is active
            newButton.SetActive(true); 

            ShopSlot slotScript = newButton.GetComponent<ShopSlot>();
            
            if (slotScript != null)
            {
                slotScript.Setup(weapon);
            }
        }
    }
}