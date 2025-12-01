using UnityEngine;
using UnityEngine.VFX;

public class DamageTrigger : MonoBehaviour
{
    [Header("Damage Settings")]
    public int damageAmount = 10;
    public bool destroyAfterHit = false;   // destroy this object after it damages the player
    public bool oneUseOnly = false;        // if true: can only ever damage once
    private bool used = false;

    [Header("Hit Feedback")]
    public VisualEffect hitVfxPrefab;      // optional VFX spawned at player position on hit

    [Tooltip("Optional per-object SFX. If left empty, PlayerHealth.defaultDamageSfx is used.")]
    public AudioClip hitSfxOverride;       // per-object override SFX

    [Tooltip("Volume for the override SFX. Can be <1 or >1.")]
    public float hitSfxVolumeOverride = 1f;

    private void OnTriggerEnter(Collider other)
    {
        if (used && oneUseOnly)
            return;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null)
            return;

        // Apply damage to player
        bool damaged = playerHealth.TryTakeDamage(damageAmount);
        if (!damaged)
            return; // no HP lost, no feedback

        used = true;

        Vector3 hitPosition = other.transform.position;

        // --- VFX ---
        if (hitVfxPrefab != null)
        {
            VisualEffect vfx = Instantiate(hitVfxPrefab, hitPosition, Quaternion.identity);
            Destroy(vfx.gameObject, 3f); // tweak lifetime if needed
        }

        // --- SFX ---
        if (hitSfxOverride != null && hitSfxVolumeOverride != 0f)
        {
            // Use per-object override clip & volume
            AudioSource.PlayClipAtPoint(hitSfxOverride, hitPosition, hitSfxVolumeOverride);
        }
        else if (playerHealth.defaultDamageSfx != null && playerHealth.defaultDamageVolume != 0f)
        {
            // Fallback to player's default damage SFX & volume
            AudioSource.PlayClipAtPoint(playerHealth.defaultDamageSfx, hitPosition, playerHealth.defaultDamageVolume);
        }

        // --- Cleanup ---
        if (destroyAfterHit)
        {
            Destroy(gameObject);
        }
    }
}
