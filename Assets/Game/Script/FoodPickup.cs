using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FoodPickup : MonoBehaviour
{
    [Header("Food Pickup")]
    public int healthAmount = 40;      // how much health this food gives
    public float weightAmountKg = 0.5f;

    [Tooltip("Destroy this pickup after the player collects it.")]
    public bool destroyOnPickup = true;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null)
            return;

        if (healthAmount > 0)
            playerHealth.Heal(healthAmount);

        if (weightAmountKg != 0f)
            playerHealth.AddWeight(weightAmountKg);

        if (destroyOnPickup)
            Destroy(gameObject);
    }
}
