using UnityEngine;
using System.Collections.Generic;

public class PlayerLockOn : MonoBehaviour
{
    [Header("Settings")]
    public LayerMask enemyLayer;
    public float detectionRadius = 20f;
    public float maxLockOnDistance = 25f;
    public KeyCode lockKey = KeyCode.Q;

    // This is the variable your PlayerMovement script reads
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
            {
                Unlock(); // Press Q to cancel
            }
            else
            {
                FindTarget(); // Press Q to find
            }
        }

        // 2. Break lock if target is dead, disabled, or too far
        if (CurrentTarget != null)
        {
            float dist = Vector3.Distance(transform.position, CurrentTarget.position);
            if (!CurrentTarget.gameObject.activeInHierarchy || dist > maxLockOnDistance)
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
            Vector3 directionToEnemy = enemy.transform.position - transform.position;
            float dSqrToTarget = directionToEnemy.sqrMagnitude;
            
            // Check if enemy is generally in front of camera view (optional but feels better)
            Vector3 viewportPos = mainCam.WorldToViewportPoint(enemy.transform.position);
            bool isOnScreen = viewportPos.x > 0 && viewportPos.x < 1 && viewportPos.y > 0 && viewportPos.y < 1;

            if (isOnScreen && dSqrToTarget < shortestDistance)
            {
                shortestDistance = dSqrToTarget;
                bestTarget = enemy.transform;
            }
        }

        CurrentTarget = bestTarget;
    }

    public void Unlock()
    {
        CurrentTarget = null;
    }

    // Visualize Range in Editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}