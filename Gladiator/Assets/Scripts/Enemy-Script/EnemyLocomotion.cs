using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class EnemyLocomotion : MonoBehaviour
{
    [Header("Targeting")]
    public Transform playerTarget;
    public Animator animator;

    [Header("Movement Settings")]
    [Tooltip("How close to get before stopping completely.")]
    public float stopDistance = 1.3f;
    [Tooltip("Distance at which the enemy switches from Running to Strafing.")]
    public float strafeDistance = 5.0f;
    [Tooltip("How fast the enemy turns to face the player while strafing.")]
    public float rotationSpeed = 5.0f;

    [Header("Optimization")]
    public float pathUpdateDelay = 0.2f;

    private NavMeshAgent agent;
    private CharacterController cc; // Cached for performance
    private float pathUpdateTimer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        cc = GetComponent<CharacterController>(); // Cache this once!
        
        if (animator == null)
            animator = GetComponent<Animator>();

        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTarget = player.transform;
        }

        SetupCrowdAvoidance();
    }

    void Update()
    {
        // Safe check using the cached reference
        if (cc != null && !cc.enabled) return;
        if (playerTarget == null) return;

        MoveToPlayer();
        HandleAnimationAndRotation();
    }

    void MoveToPlayer()
    {
        // Only calculate paths if the agent is fully active and on the mesh
        if (!agent.isOnNavMesh || !agent.isActiveAndEnabled) return;

        pathUpdateTimer += Time.deltaTime;
        if (pathUpdateTimer >= pathUpdateDelay)
        {
            agent.SetDestination(playerTarget.position);
            pathUpdateTimer = 0;
        }

        agent.stoppingDistance = stopDistance;
    }

    void HandleAnimationAndRotation()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        bool isStrafing = distanceToPlayer <= strafeDistance;

        animator.SetBool("isStrafing", isStrafing);

        if (isStrafing)
        {
            // --- STRAFING BEHAVIOR ---
            agent.updateRotation = false;
            
            Vector3 directionToPlayer = (playerTarget.position - transform.position).normalized;
            directionToPlayer.y = 0; 
            
            if (directionToPlayer != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(directionToPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
            }

            Vector3 relativeVelocity = transform.InverseTransformDirection(agent.velocity);
            
            float vX = relativeVelocity.x / agent.speed; 
            float vZ = relativeVelocity.z / agent.speed; 

            animator.SetFloat("InputX", vX, 0.1f, Time.deltaTime);
            animator.SetFloat("InputY", vZ, 0.1f, Time.deltaTime);
        }
        else
        {
            // --- CHASING BEHAVIOR ---
            agent.updateRotation = true;

            animator.SetFloat("Speed", agent.velocity.magnitude, 0.1f, Time.deltaTime);
            
            animator.SetFloat("InputX", 0, 0.1f, Time.deltaTime);
            animator.SetFloat("InputY", 0, 0.1f, Time.deltaTime);
        }
    }

    void SetupCrowdAvoidance()
    {
        agent.avoidancePriority = Random.Range(30, 60);
        agent.autoBraking = true; 
    }
    
    // CALL THIS when an attack/dodge finishes
    public void ResumeMovement()
    {
        // 1. Safety Check: Only touch the agent if it's safely on the NavMesh
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.updateRotation = true;
            agent.isStopped = false;
            
            // Clear out any stale paths from before the dodge
            agent.ResetPath(); 
        }
        else if (agent != null && !agent.isOnNavMesh)
        {
            // Failsafe: If the dodge pushed them off the mesh, snap them back
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 2.0f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
                agent.updateRotation = true;
                agent.isStopped = false;
            }
        }

        // 2. Force an immediate path calculation on the NEXT normal frame
        pathUpdateTimer = pathUpdateDelay; 
        
        // 3. REMOVED the manual Update() call. Let Unity handle the frame naturally!
    }
}