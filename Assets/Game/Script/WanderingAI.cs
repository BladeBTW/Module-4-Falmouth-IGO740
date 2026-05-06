using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class WanderingAI : MonoBehaviour
{
    [Header("Movement")]
    public float MoveSpeed = 5f;
    public float turnSpeed = 6f;

    [Header("Wandering")]
    public float wanderRadius = 10f;
    public float wanderInterval = 3f;
    public float arrivalRadius = 0.5f;

    [Header("NavMesh Safety")]
    public float snapDistance = 50f;
    public float retryInterval = 0.5f;

    private NavMeshAgent agent;
    private Animator animator;
    private float wanderTimer;
    private float retryTimer;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (agent != null)
        {
            agent.enabled = true;
            agent.speed = MoveSpeed;
            agent.updatePosition = true;

            // Important: we rotate manually now.
            agent.updateRotation = false;
        }
    }

    private void Start()
    {
        ForceSnapToNavMesh();
        PickNewDestination();
    }

    private void Update()
    {
        if (agent == null)
            return;

        if (!agent.enabled)
            agent.enabled = true;

        if (!agent.isOnNavMesh)
        {
            retryTimer += Time.deltaTime;

            if (retryTimer >= retryInterval)
            {
                retryTimer = 0f;
                ForceSnapToNavMesh();
            }

            UpdateAnimator(0f);
            return;
        }

        wanderTimer += Time.deltaTime;

        if (wanderTimer >= wanderInterval)
        {
            PickNewDestination();
            wanderTimer = 0f;
        }

        if (!agent.pathPending && agent.hasPath && agent.remainingDistance <= agent.stoppingDistance + arrivalRadius)
        {
            PickNewDestination();
            wanderTimer = 0f;
        }

        RotateOnlyWhileMoving();

        UpdateAnimator(agent.velocity.magnitude / Mathf.Max(0.01f, MoveSpeed));
    }

    private void RotateOnlyWhileMoving()
    {
        Vector3 velocity = agent.velocity;
        velocity.y = 0f;

        if (velocity.sqrMagnitude < 0.05f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(velocity.normalized, Vector3.up);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * turnSpeed
        );
    }

    private void ForceSnapToNavMesh()
    {
        if (agent == null || !agent.enabled)
            return;

        NavMeshHit hit;

        if (NavMesh.SamplePosition(transform.position, out hit, snapDistance, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            agent.isStopped = false;
        }
        else
        {
            Debug.LogWarning("[WanderingAI] Could not find NavMesh near: " + gameObject.name, this);
        }
    }

    private void PickNewDestination()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        Vector3 randomPoint = transform.position + Random.insideUnitSphere * wanderRadius;

        NavMeshHit hit;

        if (NavMesh.SamplePosition(randomPoint, out hit, wanderRadius, NavMesh.AllAreas))
        {
            agent.isStopped = false;
            agent.SetDestination(hit.position);
        }
    }

    private void UpdateAnimator(float speed)
    {
        if (animator == null)
            return;

        animator.SetFloat("Speed", speed);
        animator.SetBool("AirBorne", false);
    }
}