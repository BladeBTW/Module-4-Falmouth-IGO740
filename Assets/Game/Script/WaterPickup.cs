using UnityEngine;

public class WaterPickup : MonoBehaviour
{
    public int healthAmount = 20;
    public float weightAmount = 1f;

    private void OnTriggerEnter(Collider other)
    {
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null)
            return;

        bool applied = playerHealth.TryConsumeWater(healthAmount, weightAmount, transform.position);

        if (applied)
            Destroy(gameObject);
    }
}
