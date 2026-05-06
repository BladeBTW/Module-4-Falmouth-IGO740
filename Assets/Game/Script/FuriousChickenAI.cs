using System.Collections;
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

    [Header("Attack")]
    public float attackRange = 1.3f;
    public int attackDamage = 10;
    public float attackCooldown = 1.5f;
    public float attackWindup = 0.35f;
    public string attackBoolParam = "Attacking";

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
    private float nextAttackTime;

    private bool isIdle = true;
    private bool isChasing;
    private bool isAttacking;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponent<Animator>();

        agent.speed = moveSpeed;
        agent.updateRotation = true;
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
            UpdateAnimator(0f);
            return;
        }

        if (isAttacking)
        {
            FacePlayer();
            UpdateAnimator(0f);
            return;
        }

        float distanceToPlayer = player != null
            ? Vector3.Distance(transform.position, player.position)
            : Mathf.Infinity;

        if (player != null && distanceToPlayer <= aggroRange)
            isChasing = true;
        else if (distanceToPlayer > loseRange)
            isChasing = false;

        if (isChasing)
        {
            if (distanceToPlayer <= attackRange)
            {
                TryAttack();
                return;
            }

            agent.isStopped = false;
            agent.SetDestination(player.position);
            UpdateAnimator(agent.velocity.magnitude);
            return;
        }

        PatrolUpdate();
    }

    private void TryAttack()
    {
        if (Time.time < nextAttackTime)
            return;

        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        nextAttackTime = Time.time + attackCooldown;

        if (AgentReady())
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        FacePlayer();

        if (animator != null && !string.IsNullOrEmpty(attackBoolParam))
            animator.SetBool(attackBoolParam, true);

        yield return new WaitForSeconds(attackWindup);

        if (player != null)
        {
            float dist = Vector3.Distance(transform.position, player.position);

            if (dist <= attackRange + 0.4f)
            {
                PlayerHealth health = player.GetComponent<PlayerHealth>();

                if (health == null)
                    health = player.GetComponentInParent<PlayerHealth>();

                if (health != null)
                    health.TakeDamage(attackDamage, gameObject);
            }
        }

        yield return new WaitForSeconds(0.25f);

        if (animator != null && !string.IsNullOrEmpty(attackBoolParam))
            animator.SetBool(attackBoolParam, false);

        isAttacking = false;
    }

    private void PatrolUpdate()
    {
        if (isIdle)
        {
            idleTimer -= Time.deltaTime;

            if (idleTimer <= 0f)
                PickRandomDestination();

            UpdateAnimator(0f);
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f)
            StartIdle();

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

            if (Vector3.Distance(candidate, spawnPosition) > patrolRadius)
                continue;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3f, agent.areaMask))
            {
                isIdle = false;
                agent.isStopped = false;
                agent.SetDestination(hit.position);
                return;
            }
        }

        StartIdle();
    }

    private void FacePlayer()
    {
        if (player == null)
            return;

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 8f);
    }

    private void SnapToNavMesh()
    {
        if (agent == null || !agent.enabled)
            return;

        if (agent.isOnNavMesh)
            return;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 3f, agent.areaMask))
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