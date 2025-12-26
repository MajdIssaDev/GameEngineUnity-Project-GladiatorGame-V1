using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Linq; // Needed for list filtering

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

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
    
    // --- NEW: LIST OF ALL WEAPONS ENEMIES CAN USE ---
    public List<WeaponData> globalEnemyWeaponList; 
    // -----------------------------------------------

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

    private void Awake()
    {
        Instance = this;
        if (ownedWeapons.Count > 0 && equippedWeapon == null) 
            equippedWeapon = ownedWeapons[0];
    }

    private void Start()
    {
        ShowMainMenu();
    }

    private void Update()
    {
        if (moneyText != null) moneyText.text = "Gold: " + money;
    }

    // --- BUTTON EVENTS ---

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

    // --- GAME LOGIC ---

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
        if (camScript != null)
        {
            camScript.SetPlayerTarget(currentPlayerObject);
        }
        
        HealthScript pHealth = currentPlayerObject.GetComponent<HealthScript>();
        
        if (pHealth != null)
        {
            // 1. Assign the Slider FIRST so the script knows what UI to update
            pHealth.playerHudSlider = playerHealthBar; 

            // 2. THEN heal the player (which triggers the slider update)
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
                anim.SetFloat("Speed", 0f);   // Replaces "InputMagnitude"
                anim.SetFloat("InputY", 0f);  // Replaces "Vertical"
                anim.SetFloat("InputX", 0f);  // Replaces "Horizontal"
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

        GameObject bossToSpawn = bossEnemyTable[Random.Range(0, bossEnemyTable.Count)];
        Transform spawnPoint = enemySpawnPoints[Random.Range(0, enemySpawnPoints.Length)];
        GameObject newBoss = Instantiate(bossToSpawn, spawnPoint.position, spawnPoint.rotation);

        Stats bossStats = newBoss.GetComponent<Stats>();
        if (bossStats != null) bossStats.SetLevel(currentRound);

        // --- NEW: EQUIP BOSS WEAPON ---
        EquipEnemyBasedOnRound(newBoss, currentRound + 2); // Boss gets slightly better gear
    }

    void SpawnNormalWave()
    {
        if (normalEnemyTable.Count == 0) return;

        int count = Random.Range(1, 4); 
        List<Transform> availablePoints = new List<Transform>(enemySpawnPoints);

        if (count > availablePoints.Count) count = availablePoints.Count;
        enemiesAlive = count;

        for (int i = 0; i < count; i++)
        {
            GameObject enemyToSpawn = normalEnemyTable[Random.Range(0, normalEnemyTable.Count)];
            
            int index = Random.Range(0, availablePoints.Count);
            Transform selectedPoint = availablePoints[index];
            availablePoints.RemoveAt(index);
            
            GameObject newEnemy = Instantiate(enemyToSpawn, selectedPoint.position, selectedPoint.rotation);

            Stats enemyStats = newEnemy.GetComponent<Stats>();
            if (enemyStats != null) enemyStats.SetLevel(currentRound);

            // --- NEW: EQUIP ENEMY WEAPON ---
            EquipEnemyBasedOnRound(newEnemy, currentRound);
        }
    }

    // --- NEW HELPER FUNCTIONS FOR WEAPON TIERING ---

    void EquipEnemyBasedOnRound(GameObject enemy, int roundOrLevel)
    {
        EnemyWeaponHandler weaponHandler = enemy.GetComponent<EnemyWeaponHandler>();
        if (weaponHandler == null) return;

        // 1. Determine Tier logic
        // Formula: (Round - 1) / 5
        // Integer division drops the decimal, creating "steps" every 5 rounds.
        int targetTier = (roundOrLevel - 1) / 5;

        // Safety check: Ensure we don't go below Tier 0 (e.g., if Round 0 is passed in)
        if (targetTier < 0) targetTier = 0;

        // 2. Find a weapon matching that tier
        WeaponData weaponToGive = GetRandomWeaponByTier(targetTier);

        // 3. Equip it
        if (weaponToGive != null)
        {
            weaponHandler.EquipWeapon(weaponToGive);
        }
    }

    WeaponData GetRandomWeaponByTier(int tier)
    {
        // Find all weapons matching the tier
        List<WeaponData> potentialWeapons = globalEnemyWeaponList.Where(w => w.tier == tier).ToList();

        // If no weapons found for this tier (e.g., Tier 10 requested but we only made up to Tier 3)
        // Then get the highest available tier weapons instead.
        if (potentialWeapons.Count == 0 && globalEnemyWeaponList.Count > 0)
        {
            int maxTier = globalEnemyWeaponList.Max(w => w.tier);
            potentialWeapons = globalEnemyWeaponList.Where(w => w.tier == maxTier).ToList();
        }

        if (potentialWeapons.Count == 0) return null;

        return potentialWeapons[Random.Range(0, potentialWeapons.Count)];
    }

    // ----------------------------------------------

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
    }

    public void AddMoney(int amount)
    {
        money += amount;
    }
}