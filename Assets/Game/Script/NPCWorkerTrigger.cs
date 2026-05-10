using UnityEngine;
using UnityEngine.AI;

public class NPCWorkerTrigger : MonoBehaviour
{
    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.E;
    public float interactRange = 2.2f;
    public bool requireInteractKey = true;

    [Header("Win / Lose Condition")]
    [Range(0f, 1f)]
    public float requiredHealthPercent = 0.5f;

    [Header("References")]
    public Transform player;
    public PlayerHealth playerHealth;

    [Header("Optional Worker AI")]
    public bool stopWorkerWhenPlayerNear = true;

    private NavMeshAgent agent;
    private bool hasTriggered;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (player == null)
        {
            GameObject foundPlayer = GameObject.FindGameObjectWithTag("Player");

            if (foundPlayer != null)
                player = foundPlayer.transform;
        }

        if (playerHealth == null && player != null)
            playerHealth = player.GetComponent<PlayerHealth>();
    }

    private void Update()
    {
        if (hasTriggered)
            return;

        if (player == null || playerHealth == null)
            return;

        float distance = Vector3.Distance(
            Flatten(transform.position),
            Flatten(player.position)
        );

        bool playerIsClose = distance <= interactRange;

        if (stopWorkerWhenPlayerNear && agent != null && agent.isOnNavMesh)
            agent.isStopped = playerIsClose;

        if (!playerIsClose)
            return;

        if (requireInteractKey && !Input.GetKeyDown(interactKey))
            return;

        ResolveWorkerInteraction();
    }

    private void ResolveWorkerInteraction()
    {
        hasTriggered = true;

        float healthPercent =
            playerHealth.maxHealth > 0
                ? (float)playerHealth.currentHealth / playerHealth.maxHealth
                : 0f;

        if (healthPercent > requiredHealthPercent)
        {
            Debug.Log("[NPCWorkerTrigger] Player reached worker above half health. Playing success cutscenes.");

            if (CutsceneManager.Instance != null)
            {
                CutsceneManager.Instance.PlaySuccessSequence();
            }
            else if (GameUIManager.Instance != null)
            {
                Debug.LogWarning("[NPCWorkerTrigger] No CutsceneManager found. Showing finished menu directly.");
                GameUIManager.Instance.ShowGameFinished();
            }
            else
            {
                Debug.LogError("[NPCWorkerTrigger] No CutsceneManager or GameUIManager found.");
            }
        }
        else
        {
            Debug.Log("[NPCWorkerTrigger] Player reached worker with half health or less. Game over.");

            if (GameUIManager.Instance != null)
                GameUIManager.Instance.ShowGameOver();
            else
                Debug.LogError("[NPCWorkerTrigger] No GameUIManager found.");
        }
    }

    private Vector3 Flatten(Vector3 value)
    {
        value.y = 0f;
        return value;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}