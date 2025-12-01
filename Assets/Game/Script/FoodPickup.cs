using UnityEngine;

public class FoodPickup : MonoBehaviour
{
    public int healthAmount = 10;
    public float weightAmount = 2f;

    private void OnTriggerEnter(Collider other)
    {
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null)
            return;

        bool applied = playerHealth.TryConsumeFood(healthAmount, weightAmount, transform.position);

        if (applied)
            Destroy(gameObject);
    }
}
