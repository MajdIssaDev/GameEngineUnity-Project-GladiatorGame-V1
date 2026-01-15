using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Linq; 
using System; // Required for Actions

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // --- NEW EVENTS ---
    // 1. Fires when Round Ends (Passes the Player Object to the UI)
    public event Action<GameObject> OnShopOpened;
    
    // 2. Fires when Money changes or Items are bought (Refreshes UI buttons)
    public event Action OnShopUpdated;
    // ------------------

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
    
    [Header("UI Text")]
    public TextMeshProUGUI moneyText; 
    public TextMeshProUGUI roundText;
    
    [Header("UI Sliders")]
    public UnityEngine.UI.Slider playerHealthBar;
    
    private GameObject currentPlayerObject;
    private GameObject MainCamera;

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
        // Still okay to keep this here for HUD, but the Shop uses events now
        if (moneyText != null) moneyText.text = "Gold: " + money;
    }

    public void OnPlayButtonPressed()
    {
        currentRound = 0;
        money = 0; 
        
        mainMenuPanel.SetActive(false);
        shopPanel.SetActive(false);
        hudPanel.SetActive(true);

        SpawnPlayer();
        StartNextRound();
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
            }
            MainCamera.GetComponent<SoulsCamera>().enabled = true;

            if (cc != null) cc.enabled = true;

            PlayerWeaponHandler handler = currentPlayerObject.GetComponent<PlayerWeaponHandler>();
            if (handler != null && equippedWeapon != null)
            {
                handler.EquipWeapon(equippedWeapon);
            }
        }

        SetPlayerControls(true); 
        shopPanel.SetActive(false);
        hudPanel.SetActive(true);
        StartNextRound();
    }

    public void OnExitButtonPressed()
    {
        Application.Quit();
    }

    void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        shopPanel.SetActive(false);
        hudPanel.SetActive(false);
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

        Stats bossStats = newBoss.GetComponent<Stats>();
        if (bossStats != null) bossStats.SetLevel(currentRound);

        EquipEnemyBasedOnRound(newBoss, currentRound + 2); 
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
        
        // 1. ACTIVATE SHOP PANEL FIRST (So scripts awake and listen)
        shopPanel.SetActive(true); 

        // 2. TRIGGER THE EVENT (Passes the player to the UI scripts)
        OnShopOpened?.Invoke(currentPlayerObject);

        // 3. Trigger initial update for buttons
        OnShopUpdated?.Invoke();
        MainCamera.GetComponent<SoulsCamera>().enabled = false;
    }

    public void AddMoney(int amount)
    {
        money += amount;
        // Notify UI that money changed
        OnShopUpdated?.Invoke();
    }
    
    // Helper to let UI scripts trigger a refresh manually
    public void RefreshShopUI()
    {
        OnShopUpdated?.Invoke();
    }
}