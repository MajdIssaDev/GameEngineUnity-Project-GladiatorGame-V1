using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(EnemyCombat))]
public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    public Transform playerTarget; 
    public string playerTag = "Player"; 
    private EnemyCombat combatScript; 

    [Header("Movement Settings")]
    public float chaseSpeed = 3.5f;
    public float strafeSpeed = 2.0f; 

    [Header("Ranges")]
    public float strafeDistance = 8.0f; 
    public float minCrowdDistance = 2.0f; 

    [Header("Dodge / Reposition Logic")]
    public Vector2 waitInterval = new Vector2(2.0f, 5.0f); 
    public Vector2 moveDuration = new Vector2(0.5f, 1.2f); 

    // Internal State
    private NavMeshAgent agent;
    private Animator anim;
    private float distanceToPlayer;
    private float actionTimer = 0f;
    private bool isRepositioning = false;
    private float currentStrafeDir = 0f; 
    private bool inStrafeRange = false;
    
    // Debug helper to prevent spamming logs
    private bool lastAttackState = false;

    // Animator Hashes
    private int animSpeedID;
    private int animIsStrafingID;
    private int animInputXID;
    private int animInputYID;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        combatScript = GetComponent<EnemyCombat>();

        if (playerTarget == null)
        {
            GameObject foundPlayer = GameObject.FindGameObjectWithTag(playerTag);
            if (foundPlayer != null) playerTarget = foundPlayer.transform;
        }
        
        animSpeedID = Animator.StringToHash("Speed");
        animIsStrafingID = Animator.StringToHash("isStrafing");
        animInputXID = Animator.StringToHash("InputX");
        animInputYID = Animator.StringToHash("InputY");

        agent.avoidancePriority = Random.Range(30, 70);
        actionTimer = Random.Range(waitInterval.x, waitInterval.y);
    }

    void Update()
    {
        if (playerTarget == null) return;

        // 1. LOG ATTACK STATE CHANGES
        if (combatScript.isAttacking != lastAttackState)
        {
            if (combatScript.isAttacking) 
                Debug.Log($"<color=red>[{gameObject.name}] Started ATTACK Animation</color>");
            else 
                Debug.Log($"<color=green>[{gameObject.name}] Finished ATTACK. Resuming Movement.</color>");
            
            lastAttackState = combatScript.isAttacking;
        }

        // 2. FREEZE IF ATTACKING
        if (combatScript.isAttacking)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            anim.SetFloat(animSpeedID, 0); 
            return; 
        }

        distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        // 3. TRY ATTACK
        if (combatScript.CanAttack(distanceToPlayer))
        {
            combatScript.StartAttack();
            return; 
        }

        // 4. MOVEMENT LOGIC
        bool shouldStrafe = distanceToPlayer <= strafeDistance;

        if (shouldStrafe && !inStrafeRange) EnterStrafeMode();
        else if (!shouldStrafe && inStrafeRange) ExitStrafeMode();

        inStrafeRange = shouldStrafe;

        if (inStrafeRange)
            HandleCloseCombatBehavior();
        else
            HandleChasing();

        // 5. ANIMATION VELOCITY FIX
        float currentSpeed = agent.velocity.magnitude;
        if (agent.hasPath && agent.remainingDistance > agent.stoppingDistance)
        {
             if (currentSpeed < 0.1f) currentSpeed = agent.speed; 
        }

        anim.SetFloat(animSpeedID, currentSpeed, 0.1f, Time.deltaTime);
    }

    void HandleChasing()
    {
        agent.isStopped = false;
        agent.speed = chaseSpeed;
        agent.updateRotation = true; 
        agent.stoppingDistance = combatScript.attackRange - 0.2f; 
        agent.SetDestination(playerTarget.position);

        isRepositioning = false; 
        anim.SetBool(animIsStrafingID, false);
    }

    void HandleCloseCombatBehavior()
    {
        anim.SetBool(animIsStrafingID, true);
        RotateTowards(playerTarget.position);
        agent.updateRotation = false; 

        actionTimer -= Time.deltaTime;

        if (isRepositioning)
        {
            if (actionTimer <= 0) StopRepositioning();
            else MoveToSide();
        }
        else
        {
            agent.isStopped = true; 
            agent.velocity = Vector3.zero;

            anim.SetFloat(animInputXID, 0f, 0.2f, Time.deltaTime);
            anim.SetFloat(animInputYID, 0f);

            if (actionTimer <= 0) StartRepositioning();
        }
    }

    void StartRepositioning()
    {
        isRepositioning = true;
        agent.isStopped = false;
        agent.speed = strafeSpeed;
        
        currentStrafeDir = Random.Range(0, 2) == 0 ? -1f : 1f;
        actionTimer = Random.Range(moveDuration.x, moveDuration.y);

        // LOG STRAFE DIRECTION
        string dirName = currentStrafeDir == -1f ? "LEFT" : "RIGHT";
        Debug.Log($"<color=cyan>[{gameObject.name}] Strafing {dirName} (InputX: {currentStrafeDir})</color>");
    }

    void StopRepositioning()
    {
        isRepositioning = false;
        actionTimer = Random.Range(waitInterval.x, waitInterval.y);
        
        // LOG IDLE
        Debug.Log($"<color=cyan>[{gameObject.name}] Strafe IDLE (Waiting {actionTimer:F1}s)</color>");
    }

    void MoveToSide()
    {
        Vector3 offset = transform.right * currentStrafeDir * 2f;
        Vector3 dest = playerTarget.position + offset;
        agent.SetDestination(dest);
        
        anim.SetFloat(animInputXID, currentStrafeDir, 0.1f, Time.deltaTime);
    }

    void RotateTowards(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        direction.y = 0; 
        if(direction != Vector3.zero) 
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }
    
    void EnterStrafeMode()
    {
        // LOG MODE CHANGE
        Debug.Log($"<color=yellow>[{gameObject.name}] Switched to STRAFE Mode (Blend Tree ON)</color>");

        agent.isStopped = true;
        agent.updateRotation = false;
        anim.SetBool(animIsStrafingID, true);
        actionTimer = Random.Range(waitInterval.x, waitInterval.y);
        isRepositioning = false;
    }

    void ExitStrafeMode()
    {
        // LOG MODE CHANGE
        Debug.Log($"<color=yellow>[{gameObject.name}] Switched to CHASE Mode (Blend Tree OFF)</color>");

        anim.SetBool(animIsStrafingID, false);
        agent.updateRotation = true;
        isRepositioning = false;
    }
}