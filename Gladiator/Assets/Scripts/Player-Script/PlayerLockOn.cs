using UnityEngine;
using System.Collections.Generic;

public class PlayerLockOn : MonoBehaviour
{
    [Header("Settings")]
    public LayerMask enemyLayer;
    public float detectionRadius = 20f;
    public float maxLockOnDistance = 25f;

    public Transform CurrentTarget { get; private set; }
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
    }

    // --- NEW: Subscribe to the InputManager Event ---
    void OnEnable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnLockOnPressed += HandleLockOnToggle;
        }
    }

    // --- NEW: Always unsubscribe to prevent memory leaks ---
    void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnLockOnPressed -= HandleLockOnToggle;
        }
    }

    // --- NEW: The method called by the event ---
    private void HandleLockOnToggle()
    {
        if (CurrentTarget != null)
            Unlock(); 
        else
            FindTarget(); 
    }

    void Update()
    {
        // Break lock if target is dead, disabled, or too far
        if (CurrentTarget != null)
        {
            float dist = Vector3.Distance(transform.position, CurrentTarget.position);
            bool isStillEnemyLayer = (enemyLayer.value & (1 << CurrentTarget.gameObject.layer)) > 0;

            if (!CurrentTarget.gameObject.activeInHierarchy || !isStillEnemyLayer || dist > maxLockOnDistance)
            {
                Unlock();
            }
        }
    }

    void FindTarget()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, detectionRadius, enemyLayer);
        
        Transform bestTarget = null;
        float shortestDistance = Mathf.Infinity;

        foreach (Collider enemy in enemies)
        {
            Transform rootEnemy = enemy.transform.root;
            Vector3 directionToEnemy = rootEnemy.position - transform.position;
            float dSqrToTarget = directionToEnemy.sqrMagnitude;
            
            Vector3 viewportPos = mainCam.WorldToViewportPoint(rootEnemy.position);
            bool isOnScreen = viewportPos.x > 0 && viewportPos.x < 1 && viewportPos.y > 0 && viewportPos.y < 1;

            if (isOnScreen && dSqrToTarget < shortestDistance)
            {
                shortestDistance = dSqrToTarget;
                bestTarget = rootEnemy;
            }
        }

        CurrentTarget = bestTarget;
    }

    public void Unlock()
    {
        CurrentTarget = null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}