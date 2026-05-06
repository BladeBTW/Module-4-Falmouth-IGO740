using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class FuriousChickenAI : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Animator animator;

    [Header("Detection")]
    public float aggroRange = 10f;
    public float loseRange = 15f;

    [Header("Movement")]
    public float patrolRadius = 10f;
    public float minWalkDistance = 2f;
    public float maxWalkDistance = 6f;
    public float moveSpeed = 3.5f;

    [Header("Idle")]
    public float minIdleTime = 1f;
    public float maxIdleTime = 4f;

    private NavMeshAgent agent;
    private Vector3 spawnPosition;

    private float idleTimer;
    private bool isIdle = true;
    private bool isChasing = false;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponent<Animator>();

        agent.speed = moveSpeed;
    }

    private void Start()
    {
        spawnPosition = transform.position;

        SnapToNavMesh();
        StartIdle();
    }

    private void Update()
    {
        if (!AgentReady())
        {
            UpdateAnimator(0);
            return;
        }

        float distanceToPlayer = player != null
            ? Vector3.Distance(transform.position, player.position)
            : Mathf.Infinity;

        // --- CHASE LOGIC ---
        if (player != null && distanceToPlayer <= aggroRange)
        {
            isChasing = true;
        }
        else if (distanceToPlayer > loseRange)
        {
            isChasing = false;
        }

        if (isChasing)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
            UpdateAnimator(agent.velocity.magnitude);
            return;
        }

        // --- PATROL / IDLE ---
        if (isIdle)
        {
            idleTimer -= Time.deltaTime;

            if (idleTimer <= 0f)
                PickRandomDestination();

            UpdateAnimator(0);
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f)
        {
            StartIdle();
        }

        UpdateAnimator(agent.velocity.magnitude);
    }

    private void StartIdle()
    {
        isIdle = true;
        idleTimer = Random.Range(minIdleTime, maxIdleTime);

        if (AgentReady())
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    private void PickRandomDestination()
    {
        if (!AgentReady())
            return;

        for (int i = 0; i < 10; i++)
        {
            Vector2 random = Random.insideUnitCircle.normalized * Random.Range(minWalkDistance, maxWalkDistance);
            Vector3 candidate = transform.position + new Vector3(random.x, 0f, random.y);

            // stay near spawn
            if (Vector3.Distance(candidate, spawnPosition) > patrolRadius)
                continue;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                isIdle = false;
                agent.isStopped = false;
                agent.SetDestination(hit.position);
                return;
            }
        }

        StartIdle();
    }

    private void SnapToNavMesh()
    {
        if (agent == null || !agent.enabled)
            return;

        if (agent.isOnNavMesh)
            return;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 20f, NavMesh.AllAreas))
            agent.Warp(hit.position);
    }

    private bool AgentReady()
    {
        return agent != null && agent.enabled && agent.isOnNavMesh;
    }

    private void UpdateAnimator(float speed)
    {
        if (animator == null)
            return;

        animator.SetFloat("Speed", speed);
        animator.SetBool("AirBorne", false);
    }
}