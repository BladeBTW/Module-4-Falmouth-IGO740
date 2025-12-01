using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class WanderingAI : MonoBehaviour
{
    [Header("Movement")]
    public float MoveSpeed = 5f;        // should match player MoveSpeed for similar feel

    [Header("Wandering")]
    public float wanderRadius = 10f;
    public float wanderInterval = 3f;   // idle time at destination
    public float arrivalRadius = 0.5f;  // tolerance for "arrived"

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

        // --- arrival detection ---
        float targetDistance = _agent.stoppingDistance + arrivalRadius;

        bool arrived =
            !_agent.pathPending &&
            _agent.remainingDistance <= targetDistance &&
            _agent.remainingDistance != Mathf.Infinity;

        if (arrived)
        {
            _idleTimer += Time.deltaTime;

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

        // --- Animator "Speed" that matches the player's scale ---
        if (_animator != null)
        {
            Vector3 vel = _agent.velocity;
            float worldSpeed = new Vector3(vel.x, 0f, vel.z).magnitude;

            // normalize to 0–1 range, like player (_movementVelocity.magnitude before multiplying by MoveSpeed)
            float normalizedSpeed = MoveSpeed > 0.01f ? worldSpeed / MoveSpeed : 0f;

            _animator.SetFloat("Speed", normalizedSpeed);
            _animator.SetBool("AirBorne", false); // NavMesh agents are grounded
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
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, wanderRadius);
    }
}
