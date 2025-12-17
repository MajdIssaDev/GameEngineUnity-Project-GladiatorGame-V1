using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject playerPrefab; // Drag Paladin Prefab here
    public Transform spawnPoint;    // Drag SpawnPoint here
    public GameObject menuPanel;    // Drag UI Menu here

    void Start()
    {
        // Show mouse so we can click "Start"
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        menuPanel.SetActive(true);
    }

    public void OnStartButton()
    {
        // 1. Spawn Player
        Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

        // 2. Hide Menu
        menuPanel.SetActive(false);
        
        // Note: The Player script handles locking the cursor automatically
    }
}