using UnityEngine;
using System.Collections.Generic;
using TMPro;

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
    
    [Header("Enemy Tables")]
    public List<GameObject> normalEnemyTable; 
    public List<GameObject> bossEnemyTable;   
    public Transform[] enemySpawnPoints; 
    
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
        // 1. Teleport Player back to spawn
        if (currentPlayerObject != null && playerSpawnPoint != null)
        {
            CharacterController cc = currentPlayerObject.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            currentPlayerObject.transform.position = playerSpawnPoint.position;
            currentPlayerObject.transform.rotation = playerSpawnPoint.rotation;

            if (cc != null) cc.enabled = true;

            // Update Weapon
            PlayerWeaponHandler handler = currentPlayerObject.GetComponent<PlayerWeaponHandler>();
            if (handler != null && equippedWeapon != null)
            {
                handler.EquipWeapon(equippedWeapon);
            }
        }

        // --- NEW: RE-ENABLE PLAYER CONTROLS ---
        SetPlayerControls(true); 
        // --------------------------------------

        // 2. Close shop, start fighting
        shopPanel.SetActive(false);
        hudPanel.SetActive(true);
        StartNextRound();
    }

    public void OnExitButtonPressed()
    {
        Application.Quit();
        Debug.Log("Game Exited");
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
            pHealth.playerHudSlider = playerHealthBar; 
        }
    }

    // --- HELPER FUNCTION TO FREEZE/UNFREEZE PLAYER ---
    void SetPlayerControls(bool isActive)
    {
        if (currentPlayerObject == null) return;

        // Toggle Movement Script
        PlayerLocomotion movement = currentPlayerObject.GetComponent<PlayerLocomotion>();
        if (movement != null) movement.enabled = isActive;

        // Toggle Combat Script
        PlayerCombat combat = currentPlayerObject.GetComponent<PlayerCombat>();
        if (combat != null) combat.enabled = isActive;

        // Optional: Force Idle animation if disabling
        if (!isActive)
        {
            Animator anim = currentPlayerObject.GetComponent<Animator>();
            if (anim != null) 
            {
                anim.SetFloat("InputMagnitude", 0f); // Or whatever drives your run animation
                anim.SetFloat("Vertical", 0f);
                anim.SetFloat("Horizontal", 0f);
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
        }
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
        
        // --- NEW: DISABLE CONTROLS ---
        SetPlayerControls(false); 
        // -----------------------------

        hudPanel.SetActive(false);
        shopPanel.SetActive(true);
    }

    public void AddMoney(int amount)
    {
        money += amount;
    }
}