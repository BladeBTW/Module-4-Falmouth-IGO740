using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class NPCWorkerTrigger : MonoBehaviour
{
    [Header("Requirements")]
    [Tooltip("Minimum health percentage required (0.75 = 75%).")]
    [Range(0f, 1f)]
    public float minHealthPercent = 0.75f;

    [Tooltip("Minimum weight in kg required (e.g. 2.2).")]
    public float minWeightKg = 2.2f;

    [Header("Worker Animation")]
    [Tooltip("Animator on the worker NPC (for kick animation, etc.).")]
    public Animator workerAnimator;

    [Tooltip("Trigger name for the worker's kick animation.")]
    public string kickTriggerName = "Kick";

    [Tooltip("Delay (seconds) after kick before the player is killed.")]
    public float kickKillDelay = 0.5f;

    [Header("Debug")]
    public bool debugLogs = false;

    private void Reset()
    {
        // Ensure this collider acts as a trigger
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Only react to the player (look on this object or its parents)
        PlayerHealth playerHealth =
            other.GetComponent<PlayerHealth>() ??
            other.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null)
            return;

        if (debugLogs)
            Debug.Log("[NPCWorkerTrigger] Player entered worker trigger.", this);

        float healthPercent = (float)playerHealth.currentHealth /
                              Mathf.Max(1, playerHealth.maxHealth);
        float weightKg = playerHealth.currentWeightKg;

        bool healthyEnough = healthPercent >= minHealthPercent;
        bool heavyEnough = weightKg >= minWeightKg;

        if (debugLogs)
        {
            Debug.Log(
                $"[NPCWorkerTrigger] Health% = {healthPercent:P0}, " +
                $"Weight = {weightKg:0.00} kg, " +
                $"HealthyEnough = {healthyEnough}, HeavyEnough = {heavyEnough}",
                this
            );
        }

        if (healthyEnough && heavyEnough)
        {
            HandleSuccess();
        }
        else
        {
            HandleFail(playerHealth);
        }
    }

    // ───────── SUCCESS: healthy AND heavy enough ─────────
    private void HandleSuccess()
    {
        if (debugLogs)
            Debug.Log("[NPCWorkerTrigger] Requirements met -> SUCCESS sequence.", this);

        // Prefer cutscene flow if available (4Gather -> 5LuckyChicken)
        if (CutsceneManager.Instance != null)
        {
            CutsceneManager.Instance.PlaySuccessSequence();
        }
        else if (GameUIManager.Instance != null)
        {
            // Fallback: just show finished UI
            GameUIManager.Instance.ShowGameFinished();
        }
        else
        {
            Debug.LogWarning(
                "[NPCWorkerTrigger] No CutsceneManager or GameUIManager in scene. " +
                "Success will not show any UI.",
                this
            );
        }
    }

    // ───────── FAIL: not healthy or not heavy ─────────
    private void HandleFail(PlayerHealth playerHealth)
    {
        if (debugLogs)
            Debug.Log("[NPCWorkerTrigger] Requirements NOT met -> Kick + kill player.", this);

        // Trigger worker kick animation
        if (workerAnimator != null && !string.IsNullOrEmpty(kickTriggerName))
        {
            workerAnimator.SetTrigger(kickTriggerName);
        }

        // After delay, kill the player (which triggers their normal death flow)
        StartCoroutine(KillPlayerAfterKick(playerHealth));
    }

    private IEnumerator KillPlayerAfterKick(PlayerHealth playerHealth)
    {
        if (kickKillDelay > 0f)
            yield return new WaitForSeconds(kickKillDelay);

        if (playerHealth != null)
        {
            // Big damage to guarantee death
            playerHealth.TakeDamage(playerHealth.maxHealth * 10);
        }
    }
}
