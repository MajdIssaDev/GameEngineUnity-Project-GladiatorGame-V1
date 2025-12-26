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
    public float stopDistance = 2.0f;
    [Tooltip("Distance at which the enemy switches from Running to Strafing.")]
    public float strafeDistance = 5.0f;
    [Tooltip("How fast the enemy turns to face the player while strafing.")]
    public float rotationSpeed = 5.0f;

    [Header("Optimization")]
    public float pathUpdateDelay = 0.2f;

    private NavMeshAgent agent;
    private float pathUpdateTimer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        
        if (animator == null)
            animator = GetComponent<Animator>();

        // AUTOMATIC SETUP IF PLAYER IS MISSING
        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTarget = player.transform;
        }

        SetupCrowdAvoidance();
    }

    void Update()
    {
        if (playerTarget == null) return;

        // 1. Update Navigation Destination
        MoveToPlayer();

        // 2. Handle Animation & Rotation Logic
        HandleAnimationAndRotation();
    }

    void MoveToPlayer()
    {
        // Optimization: Don't calculate a new path every single frame
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

        // Pass the boolean to the Animator
        animator.SetBool("isStrafing", isStrafing);

        if (isStrafing)
        {
            // --- STRAFING BEHAVIOR ---
            
            // 1. Disable Agent Auto-Rotation so we can face the player manually
            agent.updateRotation = false;
            
            // 2. Face the Player
            Vector3 directionToPlayer = (playerTarget.position - transform.position).normalized;
            directionToPlayer.y = 0; // Keep flat
            if (directionToPlayer != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(directionToPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
            }

            // 3. Calculate Local Velocity for InputX / InputY
            // We convert the world velocity into "local" space (relative to how we are facing)
            Vector3 relativeVelocity = transform.InverseTransformDirection(agent.velocity);
            
            // Normalize relative velocity by agent speed to get a value between -1 and 1
            float vX = relativeVelocity.x / agent.speed; // Sideways
            float vZ = relativeVelocity.z / agent.speed; // Forward/Back

            animator.SetFloat("InputX", vX, 0.1f, Time.deltaTime);
            animator.SetFloat("InputY", vZ, 0.1f, Time.deltaTime);
        }
        else
        {
            // --- CHASING BEHAVIOR ---
            
            // 1. Let the NavAgent handle rotation naturally
            agent.updateRotation = true;

            // 2. Just use magnitude for the "Running" speed
            // We use 0.1f damp time to smooth out the animation changes
            animator.SetFloat("Speed", agent.velocity.magnitude, 0.1f, Time.deltaTime);
            
            // Reset Strafe params so they don't get stuck
            animator.SetFloat("InputX", 0, 0.1f, Time.deltaTime);
            animator.SetFloat("InputY", 0, 0.1f, Time.deltaTime);
        }
    }

    void SetupCrowdAvoidance()
    {
        // Randomized priority helps agents flow around each other
        agent.avoidancePriority = Random.Range(30, 60);
        agent.autoBraking = true; 
    }
    
    // CALL THIS when an attack finishes to snap the enemy back to reality
    public void ResumeMovement()
    {
        // 1. Re-enable rotation so the agent controls direction again
        agent.updateRotation = true;
        
        // 2. Unpause the agent
        agent.isStopped = false;

        // 3. Force an immediate path calculation (bypass the optimization delay)
        pathUpdateTimer = pathUpdateDelay; 
        
        // 4. Ensure Strafe/Move logic runs this frame
        Update(); 
    }
}