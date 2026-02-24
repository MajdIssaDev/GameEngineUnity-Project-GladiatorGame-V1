using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TargetingSystem : MonoBehaviour
{
    public Transform currentTarget;
    public LayerMask enemyLayer;
    public float detectionRadius = 20f;
    
    //Limits checking to objects visible on screen
    private Camera cam;

    void Start() {
        cam = Camera.main;
    }

    //Subscribe to the InputManager so we only run targeting logic when the button is
    //actually pressed, completely eliminating the need for an expensive Update() loop
    void OnEnable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnLockOnPressed += HandleTargetingInput;
        }
    }

    // --- Unsubscribe from the Lock-on event ---
    void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnLockOnPressed -= HandleTargetingInput;
        }
    }

    // --- Handler method replacing the Update check ---
    private void HandleTargetingInput()
    {
        if (currentTarget == null) {
            AssignTarget();
        } else {
            Unlock();
        }
    }

    //Removed the Update() method entirely since polling for input every frame
    //is inefficient compared to our new event-driven approach

    void AssignTarget() {
        //1. Find all colliders in range
        Collider[] enemies = Physics.OverlapSphere(transform.position, detectionRadius, enemyLayer);
        
        // Target Selection Algorithm: Find the enemy closest to the center of the screen by calculating the angle
        // between the camera's forward vector and the direction to the enemy
        float closestAngle = Mathf.Infinity;
        Transform bestTarget = null;

        foreach (Collider enemy in enemies) {
            Vector3 directionToEnemy = enemy.transform.position - cam.transform.position;
            
            float angle = Vector3.Angle(cam.transform.forward, directionToEnemy);
            
            //Restrict the lock-on to a 60-degree frontal cone so the player can't
            //accidentally lock onto enemies standing directly behind them
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

    //Draw a wireframe sphere in the Unity Scene view to easily visualize
    //and balance the detection radius without needing to guess the math
    void OnDrawGizmosSelected() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}