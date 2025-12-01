using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class WanderingAI : MonoBehaviour
{
    [Header("Movement")]
    public float MoveSpeed = 5f;        // applied to NavMeshAgent

    [Header("Wandering")]
    public float wanderRadius = 10f;
    public float wanderInterval = 3f;   // idle time at destination
    public float arrivalRadius = 0.5f;  // extra tolerance on top of stoppingDistance

    private NavMeshAgent _agent;
    private Animator _animator;

    private float _idleTimer;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();

        if (_agent != null)
        {
            _agent.speed = MoveSpeed;
            _agent.updatePosition = true;
            _agent.updateRotation = true;
        }

        PickNewDestination();
    }

    void Update()
    {
        if (_agent == null)
            return;

        // --- 1. Arrival detection ---
        // Standard pattern: not calculating path AND close enough to target
        float targetDistance = _agent.stoppingDistance + arrivalRadius;

        bool arrived =
            !_agent.pathPending &&
            _agent.remainingDistance <= targetDistance &&
            _agent.remainingDistance != Mathf.Infinity;

        if (arrived)
        {
            _idleTimer += Time.deltaTime;

            // Stay put until wanderInterval has passed
            if (_idleTimer >= wanderInterval)
            {
                PickNewDestination();
                _idleTimer = 0f;
            }
        }
        else
        {
            _idleTimer = 0f;
        }

        // --- 2. Animator: Speed based on NavMeshAgent velocity ---
        if (_animator != null)
        {
            Vector3 vel = _agent.velocity;
            float horizontalSpeed = new Vector3(vel.x, 0f, vel.z).magnitude;

            // When idle: ~0  → idle (Speed <= 0.1)
            // When moving: ~MoveSpeed → walk (Speed > 0.1)
            _animator.SetFloat("Speed", horizontalSpeed);
            _animator.SetBool("AirBorne", false); // NavMeshAgent stays grounded
        }
    }

    private void PickNewDestination()
    {
        if (_agent == null)
            return;

        Vector3 randDirection = Random.insideUnitSphere * wanderRadius;
        randDirection += transform.position;

        NavMeshHit navHit;
        if (NavMesh.SamplePosition(randDirection, out navHit, wanderRadius, NavMesh.AllAreas))
        {
            _agent.isStopped = false;
            _agent.SetDestination(navHit.position);
        }
        // If sampling fails, we just don't move this frame; Update will try again
        // on the next idle cycle.
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, wanderRadius);
    }
}
