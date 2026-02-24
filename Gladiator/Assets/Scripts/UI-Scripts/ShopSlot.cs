using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ShopSlot : MonoBehaviour
{
    //We hide this from Inspector so you don't think you need to set it manually anymore
    [HideInInspector] public WeaponData weaponToSell; 
    
    [Header("UI References")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;
    public Button myButton;
    
    [Header("Audio")]
    public AudioClip BuySound;
    public AudioClip EquipSound;
    public AudioClip UnableBuy;
    
    [Header("Feedback")]
    public Color errorColor = Color.red; //The color to flash
    public float flashDuration = 0.15f;  //How long it stays red

    private ColorBlock originalColors;   //To remember what the button looked like before
    private Coroutine currentFlashRoutine;

    //--- 1. SETUP FUNCTION (Called by ShopPopulator) ---
    
    private void Awake()
    {
        //Remember what the button color was so we can reset it after it flashes red
        if (myButton != null)
        {
            originalColors = myButton.colors;
        }
    }
    
    public void Setup(WeaponData newData)
    {
        weaponToSell = newData;

        if (weaponToSell != null)
        {
            //Link this slot to a speceific weaapon and update visuals immediately
            if (nameText != null) nameText.text = weaponToSell.weaponName;
            
            //Check prices/owned status immediately
            UpdateButtonState();
        }
    }

    //--- 2. EVENT SUBSCRIPTION ---
    private void OnEnable()
    {
        //Subscribe to updates (Using OnEnable ensures it works if you close/open the shop)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnShopUpdated += UpdateButtonState;
            
            //If we are enabling the panel, force a refresh
            UpdateButtonState();
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnShopUpdated -= UpdateButtonState;
        }
    }

    //--- 3. STATE LOGIC ---
    void UpdateButtonState()
    {
        //Safety check: If Populator hasn't run yet, or GameManager is missing, stop
        if (weaponToSell == null || GameManager.Instance == null) return;

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
                myButton.interactable = true;
        }
    }

    public void OnClickButton()
    {
        if (weaponToSell == null || GameManager.Instance == null) return;

        bool isOwned = GameManager.Instance.ownedWeapons.Contains(weaponToSell);

        if (isOwned)
        {
            GameManager.Instance.equippedWeapon = weaponToSell;
            GameManager.Instance.RefreshShopUI();
            PlaySound(EquipSound);
        }
        else
        {
            if (GameManager.Instance.money >= weaponToSell.price)
            {
                GameManager.Instance.money -= weaponToSell.price;
                GameManager.Instance.ownedWeapons.Add(weaponToSell);
                GameManager.Instance.equippedWeapon = weaponToSell;
                
                GameManager.Instance.RefreshShopUI();
                PlaySound(BuySound);
            }
            else
            {
                PlaySound(UnableBuy);
                if (currentFlashRoutine != null) StopCoroutine(currentFlashRoutine);
                currentFlashRoutine = StartCoroutine(FlashErrorColor());
            }
        }
    }
    
    //--- THE FLASH LOGIC ---
    IEnumerator FlashErrorColor()
    {
        if (myButton == null) yield break;

        //1. Create a "Red" version of the button settings
        ColorBlock errorBlock = myButton.colors;
        errorBlock.normalColor = errorColor;
        errorBlock.highlightedColor = errorColor; //Also make it red even if mouse is over it
        errorBlock.pressedColor = errorColor;
        errorBlock.selectedColor = errorColor;

        //2. Apply Red
        myButton.colors = errorBlock;

        //3. Wait for 0.15 seconds
        yield return new WaitForSeconds(flashDuration);

        //4. Revert to Original colors
        myButton.colors = originalColors;
    }
    
    void PlaySound(AudioClip clip)
    {
        if (clip == null) return;

        Vector3 soundPosition;

        //1. Try to find the Main Camera
        if (Camera.main != null)
        {
            soundPosition = Camera.main.transform.position;
        }
        //2. Fallback: If no camera is found, play at the button's own position
        else 
        {
            soundPosition = transform.position;
        }

        //3. Play the sound (Requires Clip AND Position)
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(clip, soundPosition);
        }
    }
}