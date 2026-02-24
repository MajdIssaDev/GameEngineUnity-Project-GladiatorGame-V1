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

    void OnEnable()
    {
        //Subscribe to the InputManager so we don't waste performance checking for 'Q' presses in Update every frame
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnLockOnPressed += HandleManualLockInput;
        }
    }

    void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnLockOnPressed -= HandleManualLockInput;
        }
    }

    private void HandleManualLockInput()
    {
        if (CurrentTarget == null) FindClosestEnemy();
        else Unlock();
    }

    void Update()
    {
        //Quality of Life: Auto-switch to the next closest enemy if our current target dies or gets disabled
        if (CurrentTarget != null)
        {
            if (!CurrentTarget.gameObject.activeInHierarchy)
            {
                SwitchToNextTarget();
                return;
            }

            HealthScript health = CurrentTarget.GetComponentInParent<HealthScript>();
            
            if (health != null && health.IsDead)
            {
                SwitchToNextTarget();
            }
        }
    }

    void SwitchToNextTarget()
    {
        Transform deadTargetRoot = CurrentTarget;
        if (CurrentTarget.root != null) deadTargetRoot = CurrentTarget.root;

        Transform newEnemy = ScanForTarget(deadTargetRoot);

        if (newEnemy != null) LockOn(newEnemy);
        else Unlock();
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
            HealthScript health = hit.GetComponentInParent<HealthScript>();
            Transform hitRoot = (health != null) ? health.transform : hit.transform.root;

            //Filer out invalid targets (the enemy that just died)
            if (ignoreRoot != null && hitRoot == ignoreRoot) continue;
            if (health != null && health.IsDead) continue;
            if (!hit.gameObject.activeInHierarchy) continue;

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

        //Add the enemy to the Cinemachine Target Group so the camera dynamically frames both the player and the enemy
        targets[0].Object = transform;
        targets[0].Weight = 1.5f;
        targets[0].Radius = 1.5f;

        targets[1].Object = enemy;
        targets[1].Weight = 1.0f;
        targets[1].Radius = 2.0f;

        targetGroup.Targets = targets;

        //Disable manual mouse camera controls so the player doesn't fight the auto-aim
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

        //Give camera control back to the mouse
        if (axisController != null) axisController.enabled = true;
    }
}