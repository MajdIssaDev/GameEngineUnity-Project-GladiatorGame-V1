using UnityEngine;
using Unity.Cinemachine; 

public class CameraTargeting : MonoBehaviour
{
    [Header("Settings")]
    public float scanRadius = 20f;
    public LayerMask enemyLayer;
    
    [Header("References")]
    public CinemachineTargetGroup targetGroup;
    public CinemachineCamera mainCamera; 

    public Transform CurrentTarget { get; private set; }
    private CinemachineInputAxisController axisController; 

    void Start()
    {
        if (mainCamera != null)
        {
            axisController = mainCamera.GetComponent<CinemachineInputAxisController>();
        }
    }

    void Update()
    {
        // 1. Manual Lock Input
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (CurrentTarget == null) FindClosestEnemy();
            else Unlock();
        }

        // 2. AUTO-SWITCH LOGIC
        if (CurrentTarget != null)
        {
            // A. Check if object was destroyed
            if (!CurrentTarget.gameObject.activeInHierarchy)
            {
                SwitchToNextTarget();
                return;
            }

            // B. Check if object is "Dead" (Health <= 0)
            // We use GetComponentInParent to find the script even if we are targeting a bone/limb
            HealthScript health = CurrentTarget.GetComponentInParent<HealthScript>();
            
            // If we found the script and the enemy is dead, SWITCH.
            if (health != null && health.IsDead)
            {
                SwitchToNextTarget();
            }
        }
    }

    void SwitchToNextTarget()
    {
        // 1. Remember who the dead guy is so we don't pick him again
        Transform deadTargetRoot = CurrentTarget;
        
        // If we can find the root object, use that as the ignore target
        if (CurrentTarget.root != null) deadTargetRoot = CurrentTarget.root;

        // 2. Scan for a NEW candidate
        Transform newEnemy = ScanForTarget(deadTargetRoot);

        // 3. Decide: Lock new enemy OR Unlock completely
        if (newEnemy != null) 
        {
            LockOn(newEnemy);
        }
        else 
        {
            Unlock();
        }
    }

    void FindClosestEnemy()
    {
        Transform nearest = ScanForTarget(null);
        if (nearest != null) LockOn(nearest);
    }

    Transform ScanForTarget(Transform ignoreRoot)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, scanRadius, enemyLayer);
        float closestDist = Mathf.Infinity;
        Transform bestCandidate = null;

        foreach (Collider hit in hits)
        {
            // 1. Get the root/health script of the thing we hit
            HealthScript health = hit.GetComponentInParent<HealthScript>();
            Transform hitRoot = (health != null) ? health.transform : hit.transform.root;

            // 2. Skip if it's the enemy we are currently trying to switch AWAY from
            if (ignoreRoot != null && hitRoot == ignoreRoot) continue;

            // 3. Skip if it is already dead
            if (health != null && health.IsDead) continue;

            // 4. Skip disabled objects
            if (!hit.gameObject.activeInHierarchy) continue;

            // 5. Compare Distance
            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                bestCandidate = hit.transform;
            }
        }

        return bestCandidate;
    }

    void LockOn(Transform enemy)
    {
        CurrentTarget = enemy;
        var targets = targetGroup.Targets;

        while (targets.Count < 2)
            targets.Add(new CinemachineTargetGroup.Target());

        // Player (Index 0)
        targets[0].Object = transform;
        targets[0].Weight = 1.5f;
        targets[0].Radius = 1.5f;

        // Enemy (Index 1)
        targets[1].Object = enemy;
        targets[1].Weight = 1.0f;
        targets[1].Radius = 2.0f;

        targetGroup.Targets = targets;

        if (axisController != null) axisController.enabled = false;
    }

    public void Unlock()
    {
        CurrentTarget = null;
        var targets = targetGroup.Targets;

        if (targets.Count >= 2)
        {
            targets[1].Object = null;
            targets[1].Weight = 0f;
        }

        if (targets.Count > 0) targets[0].Weight = 1f; 

        targetGroup.Targets = targets;

        if (axisController != null) axisController.enabled = true;
    }
}