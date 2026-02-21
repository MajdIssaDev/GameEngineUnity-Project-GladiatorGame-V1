using UnityEngine;
using System.Collections.Generic;

public class PlayerLockOn : MonoBehaviour
{
    [Header("Settings")]
    public LayerMask enemyLayer;
    public float detectionRadius = 20f;
    public float maxLockOnDistance = 25f;
    public KeyCode lockKey = KeyCode.Q;

    public Transform CurrentTarget { get; private set; }

    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
    }

    void Update()
    {
        // 1. Input to Toggle Lock
        if (Input.GetKeyDown(lockKey))
        {
            if (CurrentTarget != null)
                Unlock(); // Press Q to cancel
            else
                FindTarget(); // Press Q to find
        }

        // 2. Break lock if target is dead, disabled, or too far
        if (CurrentTarget != null)
        {
            float dist = Vector3.Distance(transform.position, CurrentTarget.position);
            
            // FIX 2: Check if the target's layer is STILL part of the enemyLayer mask
            // If you change the enemy's layer to "Default" when they die, this will trigger the unlock.
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
            // FIX 1: Use .root to ensure we target the main enemy parent, 
            // not a floating child UI collider or head hitbox.
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