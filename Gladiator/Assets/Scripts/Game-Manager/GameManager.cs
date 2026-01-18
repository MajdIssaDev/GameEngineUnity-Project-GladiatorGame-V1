using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Linq; 
using System; 

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public event Action<GameObject> OnShopOpened;
    public event Action OnShopUpdated;

    [Header("Player Data")]
    public int money = 0;
    public List<WeaponData> ownedWeapons = new List<WeaponData>();
    public WeaponData equippedWeapon; 

    [Header("Game Loop")]
    public int currentRound = 0;
    private int enemiesAlive = 0;

    [Header("Spawning")]
    public GameObject playerPrefab;
    public Transform playerSpawnPoint; 
    
    [Header("Enemy Configuration")]
    public List<GameObject> normalEnemyTable; 
    public List<GameObject> bossEnemyTable;   
    public Transform[] enemySpawnPoints; 
    public List<WeaponData> globalEnemyWeaponList; 

    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject shopPanel;
    public GameObject hudPanel;
    public GameObject loseMenuPanel;
    
    [Header("Pause System")]
    public GameObject pauseContainer;    // The transparent background (Parent)
    public GameObject pauseMainPanel;    // The buttons (Resume, Settings, Exit)
    public GameObject settingsPanel;     // The settings sub-menu
    
    // Internal variable
    private bool isPaused = false;
    
    [Header("UI Text")]
    public TextMeshProUGUI moneyText; 
    public TextMeshProUGUI roundText;
    
    [Header("UI Sliders")]
    public UnityEngine.UI.Slider playerHealthBar;
    
    [Header("Starting Gear")]
    public WeaponData defaultWeapon;
    
    private GameObject currentPlayerObject;
    private GameObject MainCamera;
    
    private GameObject menuToRestore = null;

    private void Awake()
    {
        Instance = this;
        if (ownedWeapons.Count > 0 && equippedWeapon == null) 
            equippedWeapon = ownedWeapons[0];
    }

    private void Start()
    {
        ShowMainMenu();
        MainCamera = GameObject.Find("Main Camera");
    }

    private void Update()
    {
        if (moneyText != null) moneyText.text = "Gold: " + money;
        
        if (Input.GetKeyDown(KeyCode.Escape) && !loseMenuPanel.activeSelf)
        {
            TogglePause();
        }
    }
    
    public void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            // =========================
            //      PAUSING THE GAME
            // =========================
            Time.timeScale = 0f;
            AudioListener.pause = true; // Silence Audio

            // 1. "SMART" CHECK: Is the Shop (or other menu) open?
            if (loseMenuPanel != null && loseMenuPanel.activeSelf)
            {
                menuToRestore = loseMenuPanel; // Remember it!
                loseMenuPanel.SetActive(false); // Hide it for now
            }
            else if (mainMenuPanel != null && mainMenuPanel.activeSelf)
            {
                menuToRestore = mainMenuPanel; // Remember it!
                mainMenuPanel.SetActive(false); // Hide it for now
            }
            else if (shopPanel != null && shopPanel.activeSelf)
            {
                menuToRestore = shopPanel; // Remember it!
                shopPanel.SetActive(false); // Hide it for now
            }
            else
            {
                menuToRestore = null; // Nothing was open, just normal gameplay
            }

            // 2. Show Pause Menu
            if (pauseContainer != null) pauseContainer.SetActive(true);
            
            // 3. Reset Hierarchy (Always show Main Pause buttons first)
            if (pauseMainPanel != null) pauseMainPanel.SetActive(true);
            if (settingsPanel != null) settingsPanel.SetActive(false);

            // 4. Unlock Cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // 5. Hide HUD
            hudPanel.SetActive(false);
        }
        else
        {
            // =========================
            //      RESUMING THE GAME
            // =========================
            Time.timeScale = 1f;
            AudioListener.pause = false; // Restore Audio

            // 1. Hide Pause Menu
            if (pauseContainer != null) pauseContainer.SetActive(false);

            // 2. DECIDE WHAT TO SHOW
            if (menuToRestore != null)
            {
                // A. We had a menu open (like Shop), bring it back!
                menuToRestore.SetActive(true);
                
                // IMPORTANT: Keep Cursor UNLOCKED for the shop
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                
                // Clear the memory
                menuToRestore = null; 
            }
            else
            {
                // B. No menu was open, go back to Action
                hudPanel.SetActive(true);
                
                // Lock Cursor (Only if we are fully back in the game)
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    // 1. FOR THE "RESUME" BUTTON
    public void OnResumeButtonPressed()
    {
        // Simply calling TogglePause will handle unfreezing time, 
        // hiding the menu, and locking the cursor back.
        TogglePause();
    }

    // 2. FOR THE "SETTINGS" BUTTON
    public void OnControlsButtonPressed()
    {
        if (pauseMainPanel != null && settingsPanel != null)
        {
            pauseMainPanel.SetActive(false); // Hide the buttons (Resume/Exit)
            settingsPanel.SetActive(true);   // Show the sliders/options
        }
    }

    // 3. FOR THE "BACK" BUTTON (Inside Settings Panel)
    public void OnBackFromSettingsPressed()
    {
        if (pauseMainPanel != null && settingsPanel != null)
        {
            settingsPanel.SetActive(false);  // Hide Settings
            pauseMainPanel.SetActive(true);  // Show Main Pause Menu again
        }
    }
    
    public void OnPlayButtonPressed()
    {
        // 1. Reset Game Variables
        currentRound = 0;
        money = 0; 
    
        // --- RESET WEAPONS ---
        ownedWeapons.Clear(); // Delete all bought weapons
    
        if (defaultWeapon != null)
        {
            ownedWeapons.Add(defaultWeapon); // Add the starter sword back
            equippedWeapon = defaultWeapon;  // Equip it
        }
        // ---------------------

        // 2. Cleanup UI
        mainMenuPanel.SetActive(false);
        shopPanel.SetActive(false);
        loseMenuPanel.SetActive(false); 
        hudPanel.SetActive(true);

        // 3. Destroy All Enemies
        GameObject[] existingEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in existingEnemies)
        {
            Destroy(enemy);
        }

        // 4. Reset Enemy Count
        enemiesAlive = 0; 
        menuToRestore = null;
        
        // 5. Spawn Player 
        // (This AUTOMATICALLY resets stats because it spawns a fresh prefab)
        SpawnPlayer();
    
        StartNextRound();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OnNextRoundButtonPressed()
    {
        if (currentPlayerObject != null && playerSpawnPoint != null)
        {
            CharacterController cc = currentPlayerObject.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            currentPlayerObject.transform.position = playerSpawnPoint.position;
            currentPlayerObject.transform.rotation = playerSpawnPoint.rotation;
            
            // Refill HP for next round
            HealthScript pHealth = currentPlayerObject.GetComponent<HealthScript>();
            if (pHealth != null)
            {
                pHealth.playerHudSlider = playerHealthBar; 
                pHealth.setCurrentHealth(pHealth.getMaxHealth());
            }

            MainCamera.GetComponent<SoulsCamera>().enabled = true;
            if (cc != null) cc.enabled = true;

            PlayerWeaponHandler handler = currentPlayerObject.GetComponent<PlayerWeaponHandler>();
            if (handler != null && equippedWeapon != null)
            {
                handler.EquipWeapon(equippedWeapon);
            }
        }
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SetPlayerControls(true); 
        shopPanel.SetActive(false);
        hudPanel.SetActive(true);
        StartNextRound();
    }

    public void OnExitButtonPressed()
    {
        // 1. Log message to prove button works
        Debug.Log("Exit Button Pressed");

        // 2. If running in the Unity Editor, stop playing
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        shopPanel.SetActive(false);
        hudPanel.SetActive(false);
        loseMenuPanel.SetActive(false);
        pauseContainer.SetActive(false);
    }
    
    // --- 4. NEW FUNCTION CALLED BY HEALTH SCRIPT ---
    public void GameOver()
    {
        Debug.Log("Game Over!");

        // Stop the player from moving/attacking (if they aren't already stopped by death logic)
        SetPlayerControls(false);

        // Hide Gameplay UI
        hudPanel.SetActive(false);
        shopPanel.SetActive(false);
        
        // Show Lose Menu
        if (loseMenuPanel != null)
        {
            loseMenuPanel.SetActive(true);
        }
        
        // Optional: Stop the camera or unlock the cursor so the player can click buttons
        if (MainCamera != null)
        {
             // MainCamera.GetComponent<SoulsCamera>().enabled = false; 
             Cursor.lockState = CursorLockMode.None;
             Cursor.visible = true;
        }
    }
    // -----------------------------------------------

    public void StartNextRound()
    {
        currentRound++;
        if (roundText != null) roundText.text = "Round: " + currentRound;
        SpawnEnemies();
    }

    public void SpawnPlayer()
    {
        if (currentPlayerObject != null) Destroy(currentPlayerObject);

        currentPlayerObject = Instantiate(playerPrefab, playerSpawnPoint.position, playerSpawnPoint.rotation);

        PlayerWeaponHandler handler = currentPlayerObject.GetComponent<PlayerWeaponHandler>();
        if (handler != null && equippedWeapon != null)
        {
            handler.EquipWeapon(equippedWeapon);
        }

        SoulsCamera camScript = FindObjectOfType<SoulsCamera>();
        if (camScript != null) camScript.SetPlayerTarget(currentPlayerObject);
        
        HealthScript pHealth = currentPlayerObject.GetComponent<HealthScript>();
        if (pHealth != null)
        {
            pHealth.playerHudSlider = playerHealthBar; 
            pHealth.setCurrentHealth(pHealth.getMaxHealth());
        }
    }

    void SetPlayerControls(bool isActive)
    {
        if (currentPlayerObject == null) return;
        PlayerLocomotion movement = currentPlayerObject.GetComponent<PlayerLocomotion>();
        if (movement != null) movement.enabled = isActive;
        PlayerCombat combat = currentPlayerObject.GetComponent<PlayerCombat>();
        if (combat != null) combat.enabled = isActive;

        if (!isActive)
        {
            Animator anim = currentPlayerObject.GetComponent<Animator>();
            if (anim != null) 
            {
                anim.SetFloat("Speed", 0f);   
                anim.SetFloat("InputY", 0f);  
                anim.SetFloat("InputX", 0f);  
            }
        }
    }

    // ... (Keep existing Enemy Spawning Logic) ...
    void SpawnEnemies()
    {
        if (currentRound % 5 == 0) SpawnBoss();
        else SpawnNormalWave();
    }

    void SpawnBoss()
    {
        if (bossEnemyTable.Count == 0) return;
        enemiesAlive = 1; 

        GameObject bossToSpawn = bossEnemyTable[UnityEngine.Random.Range(0, bossEnemyTable.Count)];
        Transform spawnPoint = enemySpawnPoints[UnityEngine.Random.Range(0, enemySpawnPoints.Length)];
        GameObject newBoss = Instantiate(bossToSpawn, spawnPoint.position, spawnPoint.rotation);

        Stats bossStats = newBoss.GetComponent<Stats>();
        if (bossStats != null) bossStats.SetLevel(currentRound);

        EquipEnemyBasedOnRound(newBoss, currentRound + 2); 
        EnemyAI ai = newBoss.GetComponent<EnemyAI>();
        if (ai != null && currentPlayerObject != null)
        {
            ai.playerTarget = currentPlayerObject.transform;
        }
    }

    void SpawnNormalWave()
    {
        if (normalEnemyTable.Count == 0) return;

        int count = UnityEngine.Random.Range(1, 4); 
        List<Transform> availablePoints = new List<Transform>(enemySpawnPoints);

        if (count > availablePoints.Count) count = availablePoints.Count;
        enemiesAlive = count;

        for (int i = 0; i < count; i++)
        {
            GameObject enemyToSpawn = normalEnemyTable[UnityEngine.Random.Range(0, normalEnemyTable.Count)];
            
            int index = UnityEngine.Random.Range(0, availablePoints.Count);
            Transform selectedPoint = availablePoints[index];
            availablePoints.RemoveAt(index);
            
            GameObject newEnemy = Instantiate(enemyToSpawn, selectedPoint.position, selectedPoint.rotation);

            Stats enemyStats = newEnemy.GetComponent<Stats>();
            if (enemyStats != null) enemyStats.SetLevel(currentRound);

            EquipEnemyBasedOnRound(newEnemy, currentRound);
            EnemyAI ai = newEnemy.GetComponent<EnemyAI>();
            if (ai != null && currentPlayerObject != null)
            {
                ai.playerTarget = currentPlayerObject.transform;
            }
        }
    }

    void EquipEnemyBasedOnRound(GameObject enemy, int roundOrLevel)
    {
        EnemyWeaponHandler weaponHandler = enemy.GetComponent<EnemyWeaponHandler>();
        if (weaponHandler == null) return;

        int targetTier = (roundOrLevel - 1) / 5;
        if (targetTier < 0) targetTier = 0;

        WeaponData weaponToGive = GetRandomWeaponByTier(targetTier);

        if (weaponToGive != null) weaponHandler.EquipWeapon(weaponToGive);
    }

    WeaponData GetRandomWeaponByTier(int tier)
    {
        List<WeaponData> potentialWeapons = globalEnemyWeaponList.Where(w => w.tier == tier).ToList();

        if (potentialWeapons.Count == 0 && globalEnemyWeaponList.Count > 0)
        {
            int maxTier = globalEnemyWeaponList.Max(w => w.tier);
            potentialWeapons = globalEnemyWeaponList.Where(w => w.tier == maxTier).ToList();
        }

        if (potentialWeapons.Count == 0) return null;

        return potentialWeapons[UnityEngine.Random.Range(0, potentialWeapons.Count)];
    }

    public void EnemyDefeated()
    {
        enemiesAlive--;
        Debug.Log("Enemy Down! Enemies remaining: " + enemiesAlive);
        AddMoney(50); 

        if (enemiesAlive <= 0)
        {
            RoundOver();
        }
    }

    void RoundOver()
    {
        Debug.Log("Round Complete!");
        SetPlayerControls(false); 
        hudPanel.SetActive(false);
        
        shopPanel.SetActive(true); 
        OnShopOpened?.Invoke(currentPlayerObject);
        OnShopUpdated?.Invoke();
        MainCamera.GetComponent<SoulsCamera>().enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

    }

    public void AddMoney(int amount)
    {
        money += amount;
        OnShopUpdated?.Invoke();
    }
    
    public void RefreshShopUI()
    {
        OnShopUpdated?.Invoke();
    }
}