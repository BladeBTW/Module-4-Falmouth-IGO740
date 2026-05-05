using UnityEngine;
using UnityEngine.AI;

public class NPCWorkerTrigger : MonoBehaviour
{
    private NavMeshAgent agent;
    private bool isWorking = false;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        // Example idle/working behavior
        if (!isWorking && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            StartWorking();
        }
    }

    void StartWorking()
    {
        isWorking = true;
        agent.isStopped = true;

        // Simulate doing work (you can replace this)
        Invoke(nameof(StopWorking), 3f);
    }

    void StopWorking()
    {
        isWorking = false;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;

            // Send to random point again
            Vector3 randomDir = Random.insideUnitSphere * 10f;
            randomDir += transform.position;

            if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }
    }
}