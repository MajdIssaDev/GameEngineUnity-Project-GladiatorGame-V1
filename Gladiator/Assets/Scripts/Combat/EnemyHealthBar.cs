using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [Header("Debug")]
    public bool debugAlwaysShow = false; // CHECK THIS to force it visible!

    [Header("UI Elements")]
    public Image fillImage;
    public CanvasGroup canvasGroup; 

    [Header("Behavior Settings")]
    public float visibleAfterHitTime = 5f;
    
    // References
    private Stats enemyStats; 
    private HealthScript healthScript; // Or ImpactReceiver
    private PlayerLockOn playerLockOn;
    private Transform mainCam;
    
    private float lastHitTime = -100f; 

    void Start()
    {
        // 1. Find scripts on the Enemy (Parent)
        enemyStats = GetComponentInParent<Stats>();
        healthScript = GetComponentInParent<HealthScript>();
        
        // 2. Find Camera
        if (Camera.main != null) mainCam = Camera.main.transform;

        // 3. Find Player Lock-On
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerLockOn = player.GetComponent<PlayerLockOn>();

        // 4. Default to hidden unless Debug is on
        if (canvasGroup != null) canvasGroup.alpha = debugAlwaysShow ? 1 : 0;
    }

    void LateUpdate()
    {
        if (healthScript == null || mainCam == null) return;

        // --- 1. HEALTH ---
        float maxHealth = healthScript.getMaxHealth();
        // Prevent divide by zero error
        if (maxHealth > 0)
        {
             float healthPercent = (float)healthScript.currentHealth / maxHealth;
             if (fillImage != null) fillImage.fillAmount = healthPercent;
        }

        // --- 2. ROTATION ---
        // Face the camera perfectly
        transform.LookAt(transform.position + mainCam.forward);

        // --- 3. VISIBILITY ---
        // If Debug Mode is ON, force visibility and skip logic
        if (debugAlwaysShow)
        {
            if (canvasGroup != null) canvasGroup.alpha = 1;
            return; 
        }

        // Normal Logic
        bool isLocked = false;
        if (playerLockOn != null && playerLockOn.CurrentTarget != null)
        {
            // Check if the locked target is part of this enemy's hierarchy
            isLocked = playerLockOn.CurrentTarget.transform == healthScript.transform;
        }

        bool recentlyHit = (Time.time - lastHitTime <= visibleAfterHitTime);

        if (canvasGroup != null)
        {
            // Show if Locked OR Hit, AND alive
            if ((isLocked || recentlyHit) && healthScript.currentHealth > 0)
                canvasGroup.alpha = 1;
            else
                canvasGroup.alpha = 0;
        }
    }

    public void OnTakeDamage()
    {
        lastHitTime = Time.time;
    }
    
}