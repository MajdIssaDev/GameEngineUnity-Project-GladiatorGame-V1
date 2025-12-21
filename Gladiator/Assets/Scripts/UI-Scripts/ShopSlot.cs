using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopSlot : MonoBehaviour
{
    public WeaponData weaponToSell; 
    
    [Header("UI References")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;
    public Button myButton;

    private void Start()
    {
        // 1. NUCLEAR FIX: If no weapon is assigned, DISABLE this script immediately.
        if (weaponToSell == null)
        {
            // Clear text so it doesn't look like a glitch
            if (nameText != null) nameText.text = "Empty";
            if (priceText != null) priceText.text = "";
            
            // Make button unclickable
            if (myButton != null) myButton.interactable = false;

            // Stop this script from ever running Update()
            this.enabled = false; 
            return;
        }

        // Setup visuals if we have data
        if (nameText != null) nameText.text = weaponToSell.weaponName;
        if (priceText != null) priceText.text = "$" + weaponToSell.price;
    }

    private void Update()
    {
        // 2. EXTRA SAFETY: If GameManager isn't ready yet, wait.
        if (GameManager.Instance == null) return;

        UpdateButtonState();
    }

    void UpdateButtonState()
    {
        // This is the line that was crashing (Line 47)
        bool isOwned = GameManager.Instance.ownedWeapons.Contains(weaponToSell);
        bool isEquipped = GameManager.Instance.equippedWeapon == weaponToSell;

        if (isEquipped)
        {
            if (priceText != null) priceText.text = "EQUIPPED";
            if (myButton != null) myButton.interactable = false; 
        }
        else if (isOwned)
        {
            if (priceText != null) priceText.text = "EQUIP";
            if (myButton != null) myButton.interactable = true;
        }
        else 
        {
            if (priceText != null) priceText.text = "$" + weaponToSell.price;
            if (myButton != null) 
                myButton.interactable = (GameManager.Instance.money >= weaponToSell.price);
        }
    }

    public void OnClickButton()
    {
        if (weaponToSell == null || GameManager.Instance == null) return;

        bool isOwned = GameManager.Instance.ownedWeapons.Contains(weaponToSell);

        if (isOwned)
        {
            GameManager.Instance.equippedWeapon = weaponToSell;
        }
        else
        {
            if (GameManager.Instance.money >= weaponToSell.price)
            {
                GameManager.Instance.money -= weaponToSell.price;
                GameManager.Instance.ownedWeapons.Add(weaponToSell);
                GameManager.Instance.equippedWeapon = weaponToSell;
            }
        }
    }
}