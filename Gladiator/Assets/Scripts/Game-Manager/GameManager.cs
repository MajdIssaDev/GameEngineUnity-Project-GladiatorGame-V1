using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Linq; 
using System; 
using UnityEngine.UI; 

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
    public GameObject pauseContainer;    
    public GameObject pauseMainPanel;    
    public GameObject settingsPanel;     
    
    private bool isPaused = false;
    
    [Header("UI Text")]
    public TextMeshProUGUI moneyText; 
    public TextMeshProUGUI roundText;
    
    [Header("UI Sliders")]
    public Slider playerHealthBar;
    public Slider playerEnergyBar;
    
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
    
    // ... (TogglePause, Resume, Settings logic remains unchanged) ...
    public void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            Time.timeScale = 0f;
            AudioListener.pause = true; 

            if (loseMenuPanel != null && loseMenuPanel.activeSelf)
            {
                menuToRestore = loseMenuPanel; 
                loseMenuPanel.SetActive(false); 
            }
            else if (mainMenuPanel != null && mainMenuPanel.activeSelf)
            {
                menuToRestore = mainMenuPanel; 
                mainMenuPanel.SetActive(false); 
            }
            else if (shopPanel != null && shopPanel.activeSelf)
            {
                menuToRestore = shopPanel; 
                shopPanel.SetActive(false); 
            }
            else
            {
                menuToRestore = null; 
            }

            if (pauseContainer != null) pauseContainer.SetActive(true);
            if (pauseMainPanel != null) pauseMainPanel.SetActive(true);
            if (settingsPanel != null) settingsPanel.SetActive(false);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            hudPanel.SetActive(false);
        }
        else
        {
            Time.timeScale = 1f;
            AudioListener.pause = false; 

            if (pauseContainer != null) pauseContainer.SetActive(false);

            if (menuToRestore != null)
            {
                menuToRestore.SetActive(true);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                menuToRestore = null; 
            }
            else
            {
                hudPanel.SetActive(true);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    public void OnResumeButtonPressed() { TogglePause(); }

    public void OnControlsButtonPressed()
    {
        if (pauseMainPanel != null && settingsPanel != null)
        {
            pauseMainPanel.SetActive(false); 
            settingsPanel.SetActive(true);   
        }
    }

    public void OnBackFromSettingsPressed()
    {
        if (pauseMainPanel != null && settingsPanel != null)
        {
            settingsPanel.SetActive(false);  
            pauseMainPanel.SetActive(true);  
        }
    }
    
    public void OnPlayButtonPressed()
    {
        currentRound = 0;
        money = 0; 
        ownedWeapons.Clear(); 
    
        if (defaultWeapon != null)
        {
            ownedWeapons.Add(defaultWeapon); 
            equippedWeapon = defaultWeapon;  
        }

        mainMenuPanel.SetActive(false);
        shopPanel.SetActive(false);
        loseMenuPanel.SetActive(false); 
        hudPanel.SetActive(true);

        GameObject[] existingEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in existingEnemies) Destroy(enemy);

        enemiesAlive = 0; 
        menuToRestore = null;
        
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
            
            HealthScript pHealth = currentPlayerObject.GetComponent<HealthScript>();
            if (pHealth != null)
            {
                pHealth.playerHudSlider = playerHealthBar; 
                pHealth.setCurrentHealth(pHealth.getMaxHealth());
                pHealth.energyHudSlider = playerEnergyBar;
                pHealth.currentEnergy = pHealth.stats.maxEnergy;
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
    
    public void GameOver()
    {
        Debug.Log("Game Over!");
        SetPlayerControls(false);
        hudPanel.SetActive(false);
        shopPanel.SetActive(false);
        
        if (loseMenuPanel != null) loseMenuPanel.SetActive(true);
        
        if (MainCamera != null)
        {
             Cursor.lockState = CursorLockMode.None;
             Cursor.visible = true;
        }
    }

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
            pHealth.energyHudSlider = playerEnergyBar;
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

        // 1. Always set level to currentRound
        Stats bossStats = newBoss.GetComponent<Stats>();
        if (bossStats != null) bossStats.SetLevel(currentRound);

        // 2. Bosses act as "2 rounds ahead" for better weapons
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

            // 1. Force Level = Round
            Stats enemyStats = newEnemy.GetComponent<Stats>();
            if (enemyStats != null) enemyStats.SetLevel(currentRound);

            // 2. Random weighted weapon
            EquipEnemyBasedOnRound(newEnemy, currentRound);
            
            EnemyAI ai = newEnemy.GetComponent<EnemyAI>();
            if (ai != null && currentPlayerObject != null)
            {
                ai.playerTarget = currentPlayerObject.transform;
            }
        }
    }

    // ========================================================================
    //             NEW: WEIGHTED RANDOM WEAPON SYSTEM
    // ========================================================================

// ========================================================================
    //             NEW: SLIDING WINDOW & DYNAMIC WEIGHT SYSTEM
    // ========================================================================

    void EquipEnemyBasedOnRound(GameObject enemy, int currentRound)
    {
        EnemyWeaponHandler weaponHandler = enemy.GetComponent<EnemyWeaponHandler>();
        if (weaponHandler == null) return;

        // 1. Calculate the Window based on User Rules
        // Round 1-5   (Cycle 0): Tier 0 - 0
        // Round 6-10  (Cycle 1): Tier 0 - 1
        // Round 11-15 (Cycle 2): Tier 1 - 2
        // Round 16-20 (Cycle 3): Tier 2 - 3
        // Round 21-25 (Cycle 4): Tier 3 - 4

        int cycle = (currentRound - 1) / 5;
        
        // The highest tier unlocked for this block
        int maxTier = Mathf.Clamp(cycle, 0, 4);
        
        // The lowest tier allowed for this block
        int minTier = maxTier - 1;
        if (cycle == 0) minTier = 0; // Special case for first 5 rounds
        minTier = Mathf.Clamp(minTier, 0, 4);

        // 2. Calculate "Progress" through the current block (0.0 to 1.0)
        // Example: Round 6 (Start of block) -> Progress 0%
        // Example: Round 10 (End of block)  -> Progress 100%
        float blockProgress = ((currentRound - 1) % 5) / 4.0f;

        WeaponData weaponToGive = GetDynamicWeightedWeapon(minTier, maxTier, blockProgress);

        if (weaponToGive != null) 
        {
            weaponHandler.EquipWeapon(weaponToGive);
        }
    }

    WeaponData GetDynamicWeightedWeapon(int minTier, int maxTier, float progress)
    {
        // 1. Filter valid weapons
        var validWeapons = globalEnemyWeaponList.Where(w => w.tier >= minTier && w.tier <= maxTier).ToList();

        if (validWeapons.Count == 0) return null;

        // 2. If Min == Max (e.g. Round 1-5), just pick random
        if (minTier == maxTier)
        {
            return validWeapons[UnityEngine.Random.Range(0, validWeapons.Count)];
        }

        // 3. Dynamic Weighting
        // As 'progress' goes from 0 to 1, we shift probability from MinTier to MaxTier.
        List<float> weights = new List<float>();
        float totalWeight = 0;

        // We use a steep curve so the transition feels significant
        // At start (0.0): 80% chance for Lower Tier
        // At end   (1.0): 80% chance for Higher Tier
        float maxTierChance = Mathf.Lerp(0.2f, 0.8f, progress); 
        float minTierChance = 1.0f - maxTierChance;

        foreach(var w in validWeapons)
        {
            float weight = 0;
            if (w.tier == maxTier) weight = maxTierChance;
            else if (w.tier == minTier) weight = minTierChance;
            
            // Safety: If you have multiple weapons of the same tier, split the chance
            // (Optional optimization, but keeps math simple for now)
            
            weights.Add(weight);
            totalWeight += weight;
        }

        // 4. Roll the Dice
        float randomValue = UnityEngine.Random.Range(0, totalWeight);
        float cursor = 0;

        for(int i = 0; i < validWeapons.Count; i++)
        {
            cursor += weights[i];
            if (randomValue <= cursor)
            {
                return validWeapons[i];
            }
        }

        return validWeapons[0];
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