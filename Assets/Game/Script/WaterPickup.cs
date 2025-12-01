using UnityEngine;

public class WaterPickup : MonoBehaviour
{
    [Header("Water Values")]
    public int healthAmount = 20;
    public float weightAmount = 1f;

    [Header("Feedback")]
    public AudioClip pickupSfx;     // sound to play on pickup

    private void OnTriggerEnter(Collider other)
    {
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null)
            return;

        bool applied = playerHealth.TryConsumeWater(healthAmount, weightAmount, transform.position);

        if (applied)
        {
            // SFX (per-object)
            if (pickupSfx != null)
                AudioSource.PlayClipAtPoint(pickupSfx, transform.position);

            Destroy(gameObject);
        }
    }
}
