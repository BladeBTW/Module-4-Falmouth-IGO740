using UnityEngine;
using UnityEngine.AI;

public class NavMeshAgentDebug : MonoBehaviour
{
    private NavMeshAgent agent;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (agent == null)
            return;

        Debug.Log(
            $"{name} | enabled={agent.enabled} | onNavMesh={agent.isOnNavMesh} | " +
            $"hasPath={agent.hasPath} | pending={agent.pathPending} | " +
            $"stopped={agent.isStopped} | velocity={agent.velocity.magnitude:F2} | " +
            $"remaining={SafeRemainingDistance()}"
        );
    }

    private string SafeRemainingDistance()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return "N/A";

        return agent.remainingDistance.ToString("F2");
    }
}