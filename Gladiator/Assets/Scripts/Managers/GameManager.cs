using UnityEngine;
using System.Collections.Generic;
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
    public bool isGameOver = false;

    [Header("Spawning")]
    public GameObject playerPrefab;
    public Transform playerSpawnPoint; 
    
    [Header("Enemy Configuration")]
    public List<GameObject> normalEnemyTable; 
    public List<GameObject> bossEnemyTable;   
    public Transform[] enemySpawnPoints; 
    public List<WeaponData> globalEnemyWeaponList; 
    
    [Header("Starting Gear")]
    public WeaponData defaultWeapon;
    
    private GameObject currentPlayerObject;
    private GameObject MainCamera;
    
    public bool isPaused = false;

    private void Awake()
    {
        Instance = this;
        if (ownedWeapons.Count > 0 && equippedWeapon == null) 
            equippedWeapon = ownedWeapons[0];
    }
    
    private void OnEnable()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OnPausePressed += HandlePauseInput;
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OnPausePressed -= HandlePauseInput;
    }

    private void Start()
    {
        if (UIManager.Instance != null) UIManager.Instance.ShowMainMenu();
        MainCamera = GameObject.Find("Main Camera");
    }

    private void Update()
    {
        if (UIManager.Instance != null) UIManager.Instance.UpdateMoney(money);
    }
    
    private void HandlePauseInput()
    {
        if (!isGameOver)
        {
            TogglePause();
        }
    }
    
    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        AudioListener.pause = isPaused; 

        if (UIManager.Instance != null)
        {
            UIManager.Instance.TogglePauseUI(isPaused);
        }

        if (isPaused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // ONLY lock the cursor if we are returning directly to the gameplay (HUD is active).
            // If a menu like the Shop was restored, keep the cursor unlocked.
            if (UIManager.Instance != null && UIManager.Instance.IsHUDActive()) 
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }

    public void OnResumeButtonPressed() { TogglePause(); }

    public void OnControlsButtonPressed()
    {
        if (UIManager.Instance != null) UIManager.Instance.ShowSettings(true);
    }

    public void OnBackFromSettingsPressed()
    {
        if (UIManager.Instance != null) UIManager.Instance.ShowSettings(false);
    }
    
    public void OnPlayButtonPressed()
    {
        currentRound = 0;
        money = 0; 
        isGameOver = false;
        ownedWeapons.Clear(); 
    
        if (defaultWeapon != null)
        {
            ownedWeapons.Add(defaultWeapon); 
            equippedWeapon = defaultWeapon;  
        }

        if (UIManager.Instance != null) UIManager.Instance.ShowHUD();

        GameObject[] existingEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in existingEnemies) Destroy(enemy);

        enemiesAlive = 0; 
        
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
                // UI assignments removed here! HealthScript updates UI automatically now.
                pHealth.setCurrentHealth(pHealth.getMaxHealth());
                pHealth.ResetEnergyToMax();
            }

            if (MainCamera != null) MainCamera.GetComponent<SoulsCamera>().enabled = true;
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
        
        if (UIManager.Instance != null) UIManager.Instance.ShowHUD();
        
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
    
    public void GameOver()
    {
        Debug.Log("Game Over!");
        isGameOver = true;
        SetPlayerControls(false);
        
        if (UIManager.Instance != null) UIManager.Instance.ShowGameOver();
        
        if (MainCamera != null)
        {
             Cursor.lockState = CursorLockMode.None;
             Cursor.visible = true;
        }
    }

    public void StartNextRound()
    {
        currentRound++;
        if (UIManager.Instance != null) UIManager.Instance.UpdateRound(currentRound);
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
            pHealth.setCurrentHealth(pHealth.getMaxHealth());
            pHealth.ResetEnergyToMax();
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

    void EquipEnemyBasedOnRound(GameObject enemy, int currentRound)
    {
        EnemyWeaponHandler weaponHandler = enemy.GetComponent<EnemyWeaponHandler>();
        if (weaponHandler == null) return;

        int cycle = (currentRound - 1) / 5;
        int maxTier = Mathf.Clamp(cycle, 0, 4);
        int minTier = maxTier - 1;
        if (cycle == 0) minTier = 0; 
        minTier = Mathf.Clamp(minTier, 0, 4);

        float blockProgress = ((currentRound - 1) % 5) / 4.0f;
        WeaponData weaponToGive = GetDynamicWeightedWeapon(minTier, maxTier, blockProgress);

        if (weaponToGive != null) 
        {
            weaponHandler.EquipWeapon(weaponToGive);
        }
    }

    WeaponData GetDynamicWeightedWeapon(int minTier, int maxTier, float progress)
    {
        var validWeapons = globalEnemyWeaponList.Where(w => w.tier >= minTier && w.tier <= maxTier).ToList();
        if (validWeapons.Count == 0) return null;

        if (minTier == maxTier)
        {
            return validWeapons[UnityEngine.Random.Range(0, validWeapons.Count)];
        }

        List<float> weights = new List<float>();
        float totalWeight = 0;

        float maxTierChance = Mathf.Lerp(0.2f, 0.8f, progress); 
        float minTierChance = 1.0f - maxTierChance;

        foreach(var w in validWeapons)
        {
            float weight = 0;
            if (w.tier == maxTier) weight = maxTierChance;
            else if (w.tier == minTier) weight = minTierChance;
            
            weights.Add(weight);
            totalWeight += weight;
        }

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
        
        if (UIManager.Instance != null) UIManager.Instance.ShowShop();
        
        OnShopOpened?.Invoke(currentPlayerObject);
        OnShopUpdated?.Invoke();
        
        if (MainCamera != null) MainCamera.GetComponent<SoulsCamera>().enabled = false;
        
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