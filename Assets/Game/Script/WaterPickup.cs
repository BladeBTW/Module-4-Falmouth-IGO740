using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WaterPickup : MonoBehaviour
{
    [Header("Water Pickup")]
    public int healthAmount = 20;      // how much health this water gives
    public float weightAmountKg = 1f;  // how much weight this water adds

    [Tooltip("Destroy this pickup after the player collects it.")]
    public bool destroyOnPickup = true;

    private void Reset()
    {
        // Make sure the collider acts as a trigger
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null)
            return;

        // Heal and add weight using the simplified PlayerHealth API
        if (healthAmount > 0)
            playerHealth.Heal(healthAmount);

        if (weightAmountKg != 0f)
            playerHealth.AddWeight(weightAmountKg);

        if (destroyOnPickup)
            Destroy(gameObject);
    }
}
