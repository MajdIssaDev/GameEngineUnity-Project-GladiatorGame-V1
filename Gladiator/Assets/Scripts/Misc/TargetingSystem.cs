using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TargetingSystem : MonoBehaviour
{
    public Transform currentTarget;
    public LayerMask enemyLayer;
    public float detectionRadius = 20f;
    
    // Limits checking to objects visible on screen
    private Camera cam;

    void Start() {
        cam = Camera.main;
    }

    void Update() {
        // Toggle Lock-on with Q
        if (Input.GetKeyDown(KeyCode.Q)) {
            if (currentTarget == null) {
                AssignTarget();
            } else {
                Unlock();
            }
        }
    }

    void AssignTarget() {
        // 1. Find all colliders in range
        Collider[] enemies = Physics.OverlapSphere(transform.position, detectionRadius, enemyLayer);
        
        // 2. Filter logic: Pick the closest one to the center of the screen
        float closestAngle = Mathf.Infinity;
        Transform bestTarget = null;

        foreach (Collider enemy in enemies) {
            Vector3 directionToEnemy = enemy.transform.position - cam.transform.position;
            
            // Calculate angle between camera forward and enemy direction
            float angle = Vector3.Angle(cam.transform.forward, directionToEnemy);
            
            // Check if within a reasonable field of view (e.g., < 60 degrees)
            if (angle < 60 && angle < closestAngle) {
                closestAngle = angle;
                bestTarget = enemy.transform;
            }
        }

        if (bestTarget != null) {
            currentTarget = bestTarget;
        }
    }

    public void Unlock() {
        currentTarget = null;
    }

    // Visualize the range in Scene view
    void OnDrawGizmosSelected() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}