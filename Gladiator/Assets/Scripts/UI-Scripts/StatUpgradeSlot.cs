using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections; // Required for Coroutines

public class StatUpgradeSlot : MonoBehaviour
{
    [Header("Settings")]
    public StatType statType; 
    public float baseCost = 50f;
    public float costPerLevel = 10f;

    [Header("UI References")]
    public TextMeshProUGUI mainText; 
    public Button buyButton;

    [Header("Audio")]
    public AudioClip buySound;
    public AudioClip errorSound;

    [Header("Feedback")]
    public Color errorColor = Color.red; 
    public float flashDuration = 0.15f;  

    private Stats playerStats;
    private ColorBlock originalColors;   
    private Coroutine currentFlashRoutine;

    private void Awake()
    {
        //Save the original button colors so we can revert back after flashing
        if (buyButton != null) {
            originalColors = buyButton.colors;
        }
    }

    //--- ON ENABLE / DISABLE ---
    private void OnEnable()
    {
        if (GameManager.Instance != null) {
            GameManager.Instance.OnShopOpened += InitializeSlot;
            GameManager.Instance.OnShopUpdated += RefreshUI;
            
            //Failsafe: Try to find player immediately if event was missed
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) InitializeSlot(player);
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null) {
            GameManager.Instance.OnShopOpened -= InitializeSlot;
            GameManager.Instance.OnShopUpdated -= RefreshUI;
        }
    }
    // ---------------------------

    void InitializeSlot(GameObject player)
    {
        if (player == null) return;
        playerStats = player.GetComponent<Stats>();
        RefreshUI();
    }

    void RefreshUI()
    {
        if (GameManager.Instance == null || playerStats == null) return;
        if (mainText == null) return;

        //Calculate Cost
        float currentValue = playerStats.GetStatValue(statType);
        int upgradeCount = CalculateUpgradeCount(currentValue);
        int currentCost = (int)(baseCost + (upgradeCount * costPerLevel));

        //Update the text
        mainText.text = $"Upgrade {statType} for ${currentCost}\nCurrent: {currentValue:F1}";
        
        //if (buyButton != null) buyButton.interactable = (GameManager.Instance.money >= currentCost)
    }

    public void BuyUpgrade()
    {
        if (playerStats == null || GameManager.Instance == null) return;

        //Figure out how much the upgraded costs right now
        float currentValue = playerStats.GetStatValue(statType);
        int upgradeCount = CalculateUpgradeCount(currentValue);
        int currentCost = (int)(baseCost + (upgradeCount * costPerLevel));

        //Check if the player has enough gold
        if (GameManager.Instance.money >= currentCost)
        {
            //SUCCESS
            GameManager.Instance.money -= currentCost;
            playerStats.ApplyUpgrade(statType);
            
            GameManager.Instance.RefreshShopUI();
            PlaySound(buySound);
        }
        else
        {
            //FAILURE (Not enough gold)
            PlaySound(errorSound);
            
            //Flash Red
            if (currentFlashRoutine != null) StopCoroutine(currentFlashRoutine);
            currentFlashRoutine = StartCoroutine(FlashErrorColor());
        }
    }

    //--- HELPER FUNCTIONS ---

    IEnumerator FlashErrorColor() //Makes the button flash red if the player can't afford the upgrade
    {
        if (buyButton == null) yield break;

        //Create Red Block
        ColorBlock errorBlock = buyButton.colors;
        errorBlock.normalColor = errorColor;
        errorBlock.highlightedColor = errorColor; 
        errorBlock.pressedColor = errorColor;
        errorBlock.selectedColor = errorColor;

        //Apply Red
        buyButton.colors = errorBlock;

        //Wait
        yield return new WaitForSeconds(flashDuration);

        //Revert
        buyButton.colors = originalColors;
    }

    void PlaySound(AudioClip clip) //Plays UI sound effects through the global Audio Manager
    {
        if (clip == null) return;

        Vector3 pos = transform.position;
        if (Camera.main != null) pos = Camera.main.transform.position;
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(clip, pos);
        }
    }

    int CalculateUpgradeCount(float val) //Maps raw stat values back to int level to determine upgrade costs
    {
        switch (statType)
        {
            case StatType.Strength: return (int)(val - 1); 
            case StatType.Defence: return (int)val;
            case StatType.Regen: return (int)((val - 1f) / 0.5f);
            case StatType.AttackSpeed: return Mathf.RoundToInt((val - 1f) / 0.1f);
            case StatType.Health: return (int)((val - 100f) / 10f);
            default: return 0;
        }
    }
}