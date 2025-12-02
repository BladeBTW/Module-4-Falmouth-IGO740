using UnityEngine;

[RequireComponent(typeof(Collider))]
public class NPCWorkerTrigger : MonoBehaviour
{
    [Header("Requirements")]
    [Tooltip("Minimum health percentage required (0.75 = 75%).")]
    public float minHealthPercent = 0.75f;

    [Tooltip("Minimum weight in kg required (e.g. 2.2).")]
    public float minWeightKg = 2.2f;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null) return;

        float healthPercent = (float)playerHealth.currentHealth / Mathf.Max(1, playerHealth.maxHealth);
        float weight = playerHealth.currentWeightKg;

        bool healthyEnough = healthPercent >= minHealthPercent;
        bool heavyEnough = weight >= minWeightKg;

        if (SimpleGameOverUI.Instance == null)
        {
            Debug.LogError("NPCWorkerTrigger: No SimpleGameOverUI in scene.");
            return;
        }

        if (!healthyEnough && !heavyEnough)
        {
            // Both too low
            SimpleGameOverUI.Instance.ShowEnd(SimpleEndType.BothFailed);
        }
        else if (!healthyEnough)
        {
            // Not healthy enough
            SimpleGameOverUI.Instance.ShowEnd(SimpleEndType.NotHealthyEnough);
        }
        else if (!heavyEnough)
        {
            // Not heavy enough
            SimpleGameOverUI.Instance.ShowEnd(SimpleEndType.NotHeavyEnough);
        }
        else
        {
            // If you want: success case – for now you could also treat as "win" image or reuse one
            SimpleGameOverUI.Instance.ShowEnd(SimpleEndType.BothFailed); // or another sprite
        }
    }
}
