using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DamageTrigger : MonoBehaviour
{
    [Header("Damage")]
    public int damageAmount = 25;

    [Header("Optional SFX")]
    public AudioClip hitSfx;
    [Tooltip("Audio volume (0–2). 1 = normal, 2 = double loud, 0.5 = quieter.")]
    public float hitVolume = 1f;

    [Tooltip("Destroy this object after dealing damage once (e.g. spike trap one-shot).")]
    public bool destroyAfterHit = false;

    private void Reset()
    {
        // Ensure collider behaves as a trigger
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null) return;

        // Apply damage
        playerHealth.TakeDamage(damageAmount);

        // Play SFX if assigned
        if (hitSfx != null && hitVolume > 0f)
            AudioSource.PlayClipAtPoint(hitSfx, transform.position, hitVolume);

        if (destroyAfterHit)
            Destroy(gameObject);
    }
}
