using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject shopPanel;
    public GameObject hudPanel;
    public GameObject loseMenuPanel;
    
    [Header("Pause System")]
    public GameObject pauseContainer;    
    public GameObject pauseMainPanel;    
    public GameObject settingsPanel;     
    
    [Header("HUD Text")]
    public TextMeshProUGUI moneyTextShop;
    public TextMeshProUGUI moneyText; 
    public TextMeshProUGUI roundText;
    
    [Header("HUD Sliders")]
    public Slider playerHealthBar;
    public Slider playerEnergyBar;

    // We store the menu to restore when unpausing
    private GameObject menuToRestore = null;

    private void Awake()
    {
        // Standard Singleton pattern
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // --- HUD UPDATES ---
    public void UpdateMoney(int money)
    {
        if (moneyText != null) moneyText.text = "Gold: \n" + money;
        if (moneyTextShop != null) moneyTextShop.text = "Gold: \n" + money;
    }

    public void UpdateRound(int round)
    {
        if (roundText != null) roundText.text = "Round: " + round;
    }

    public void UpdateHealth(float currentHealth, float maxHealth)
    {
        if (playerHealthBar != null && maxHealth > 0)
        {
            playerHealthBar.value = currentHealth / maxHealth;
        }
    }

    public void UpdateEnergy(float currentEnergy, float maxEnergy)
    {
        if (playerEnergyBar != null && maxEnergy > 0)
        {
            playerEnergyBar.value = currentEnergy / maxEnergy;
        }
    }

    // --- PANEL MANAGEMENT ---
    public void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        shopPanel.SetActive(false);
        hudPanel.SetActive(false);
        loseMenuPanel.SetActive(false);
        pauseContainer.SetActive(false);
    }

    public void ShowHUD()
    {
        mainMenuPanel.SetActive(false);
        shopPanel.SetActive(false);
        loseMenuPanel.SetActive(false); 
        hudPanel.SetActive(true);
    }

    public void ShowShop()
    {
        hudPanel.SetActive(false);
        shopPanel.SetActive(true);
    }

    public void ShowGameOver()
    {
        hudPanel.SetActive(false);
        shopPanel.SetActive(false);
        if (loseMenuPanel != null) loseMenuPanel.SetActive(true);
    }

    // --- PAUSE LOGIC ---
    public void TogglePauseUI(bool isPaused)
    {
        if (isPaused)
        {
            if (loseMenuPanel.activeSelf) { menuToRestore = loseMenuPanel; loseMenuPanel.SetActive(false); }
            else if (mainMenuPanel.activeSelf) { menuToRestore = mainMenuPanel; mainMenuPanel.SetActive(false); }
            else if (shopPanel.activeSelf) { menuToRestore = shopPanel; shopPanel.SetActive(false); }
            else { menuToRestore = null; }

            pauseContainer.SetActive(true);
            pauseMainPanel.SetActive(true);
            settingsPanel.SetActive(false);
            hudPanel.SetActive(false);
        }
        else
        {
            pauseContainer.SetActive(false);

            if (menuToRestore != null)
            {
                menuToRestore.SetActive(true);
                menuToRestore = null; 
            }
            else
            {
                hudPanel.SetActive(true);
            }
        }
    }

    public void ShowSettings(bool show)
    {
        pauseMainPanel.SetActive(!show);
        settingsPanel.SetActive(show);
    }
    
    public bool IsHUDActive()
    {
        return hudPanel != null && hudPanel.activeSelf;
    }
}