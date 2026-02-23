using UnityEngine;
using System.Collections.Generic;

public class ShopPopulator : MonoBehaviour
{
    [Header("Configuration")]
    public GameObject buttonPrefab; //Drag your new Button Prefab here
    public Transform gridContainer; //Drag the "WeaponGrid" object here
    
    [Header("Inventory")]
    public List<WeaponData> weaponsToSell; //Drag all your ScriptableObjects here

    private void Start()
    {
        GenerateShop();
    }

    void GenerateShop()
    {
        //1. Clear existing
        foreach (Transform child in gridContainer) {
            Destroy(child.gameObject);
        }

        //2. Loop through list
        foreach (WeaponData weapon in weaponsToSell)
        {
            GameObject newButton = Instantiate(buttonPrefab, gridContainer);
            
            //Force the button to be visible/active.
            newButton.SetActive(true); 

            ShopSlot slotScript = newButton.GetComponent<ShopSlot>();
            
            if (slotScript != null)
            {
                slotScript.Setup(weapon);
            }
        }
    }
}