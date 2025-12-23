using UnityEngine;
using UnityEngine.AI;

public class EnemyLocomotion : MonoBehaviour
{
    [Header("Components")]
    public NavMeshAgent agent;
    public Animator animator;
    public Transform playerTarget;

    [Header("Stats")]
    public float movementSpeed = 3.5f;
    public float rotationSpeed = 10f;
    
    [Header("Combat Settings")]
    public float stoppingDistance = 2.0f;
    [Tooltip("Distance at which enemy switches to Strafe Mode")]
    public float strafeDistance = 8.0f; 

    // --- SMART AI VARIABLES ---
    private float myCircleAngle; // The specific angle this enemy wants to stand at (0=Front, 180=Back)
    private float currentCooldown = 0f; // Timer to change position

    private void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponent<Animator>();

        if (playerTarget == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTarget = p.transform;
        }

        agent.speed = movementSpeed;
        agent.stoppingDistance = stoppingDistance;

        // --- FIX 1: PRIORITY RANDOMIZATION ---
        // Give each enemy a random priority (0 to 99). 
        // Lower numbers = Higher priority. 
        // This means "stronger" enemies push "weaker" ones out of the way, stopping the vibration/pushing war.
        agent.avoidancePriority = Random.Range(0, 99);

        // Assign a random starting angle around the player
        myCircleAngle = Random.Range(0f, 360f);
    }

    private void Update()
    {
        if (playerTarget == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        bool isStrafing = distanceToPlayer <= strafeDistance;

        // Send state to Animator
        if (animator != null) animator.SetBool("isStrafing", isStrafing);

        if (isStrafing)
        {
            // --- SMART FLANKING MODE ---
            agent.updateRotation = false; 
            HandleRotationTowardsPlayer();
            HandleStrafeAnimation();
            
            // Reposition logic: periodically change where we want to stand
            HandleSmartSurround(distanceToPlayer);
        }
        else
        {
            // --- CHASE MODE (Run straight at player) ---
            agent.updateRotation = true; 
            HandleChaseAnimation();
            
            // Just run directly to the player
            agent.SetDestination(playerTarget.position);
        }
    }

    // --- NEW: SMART POSITIONING ---
    void HandleSmartSurround(float distance)
    {
        // 1. Every few seconds, pick a new angle so we don't stay still forever
        currentCooldown -= Time.deltaTime;
        if (currentCooldown <= 0)
        {
            // Pick a new random slot around the player (e.g., move 45 degrees left or right)
            myCircleAngle += Random.Range(-45f, 45f);
            currentCooldown = Random.Range(2f, 5f);
        }

        // 2. Calculate the exact point on the ground based on our Angle
        // Formula: PlayerPos + (DirectionFromAngle * Radius)
        Vector3 offset = Quaternion.Euler(0, myCircleAngle, 0) * Vector3.forward * stoppingDistance;
        Vector3 targetPosition = playerTarget.position + offset;

        // 3. Move there
        agent.SetDestination(targetPosition);

        // 4. (Optional) If we are bumping into someone, rotate our angle faster to slide off them
        if (agent.velocity.magnitude < 0.1f && distance > stoppingDistance + 1f)
        {
            // We are stopped but not at the target -> probably blocked by another enemy
            myCircleAngle += 30f * Time.deltaTime; // Slide around the circle
        }
    }

    void HandleRotationTowardsPlayer()
    {
        Vector3 direction = (playerTarget.position - transform.position).normalized;
        direction.y = 0; 
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
        }
    }

    void HandleChaseAnimation()
    {
        float speed = agent.velocity.magnitude;
        animator.SetFloat("Speed", speed, 0.1f, Time.deltaTime);
        animator.SetFloat("InputX", 0f);
        animator.SetFloat("InputY", 0f);
    }

    void HandleStrafeAnimation()
    {
        Vector3 velocity = agent.velocity;
        Vector3 localVelocity = transform.InverseTransformDirection(velocity);
        animator.SetFloat("InputY", localVelocity.z, 0.1f, Time.deltaTime);
        animator.SetFloat("InputX", localVelocity.x, 0.1f, Time.deltaTime);
    }
}