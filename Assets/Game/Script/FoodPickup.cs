using UnityEngine;

public class FoodPickup : MonoBehaviour
{
    [Header("Food Values")]
    public int healthAmount = 10;
    public float weightAmount = 2f;

    [Header("Feedback")]
    public AudioClip pickupSfx;     // sound to play on pickup

    private void OnTriggerEnter(Collider other)
    {
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null)
            return;

        bool applied = playerHealth.TryConsumeFood(healthAmount, weightAmount, transform.position);

        if (applied)
        {
            // SFX (per-object)
            if (pickupSfx != null)
                AudioSource.PlayClipAtPoint(pickupSfx, transform.position);

            Destroy(gameObject);
        }
    }
}
