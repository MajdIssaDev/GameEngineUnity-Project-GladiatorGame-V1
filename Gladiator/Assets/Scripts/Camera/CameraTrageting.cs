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
    private CinemachineInputAxisController axisController; // To disable mouse

    void Start()
    {
        if (mainCamera != null)
        {
            axisController = mainCamera.GetComponent<CinemachineInputAxisController>();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (CurrentTarget == null) FindClosestEnemy();
            else Unlock();
        }

        // Auto-unlock if enemy dies or is disabled
        if (CurrentTarget != null && !CurrentTarget.gameObject.activeInHierarchy)
        {
            Unlock();
        }
    }

    void FindClosestEnemy()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, scanRadius, enemyLayer);
        float closestDist = Mathf.Infinity;
        Transform closestEnemy = null;

        foreach (Collider hit in hits)
        {
            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestEnemy = hit.transform;
            }
        }

        if (closestEnemy != null) LockOn(closestEnemy);
    }

    void LockOn(Transform enemy)
    {
        CurrentTarget = enemy;
        var targets = targetGroup.Targets;

        while (targets.Count < 2)
            targets.Add(new CinemachineTargetGroup.Target());

        // Player (index 0)
        targets[0].Object = transform;
        targets[0].Weight = 1.5f;
        targets[0].Radius = 1.5f;

        // Enemy (index 1)
        targets[1].Object = enemy;
        targets[1].Weight = 1.0f;
        targets[1].Radius = 2.0f;

        targetGroup.Targets = targets;

        if (axisController != null)
            axisController.enabled = false;
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

        // Restore Player focus
        if (targets.Count > 0) targets[0].Weight = 1f; 

        targetGroup.Targets = targets;

        // RE-ENABLE mouse rotation
        if (axisController != null) axisController.enabled = true;
    }
}