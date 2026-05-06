using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WaterPickup : MonoBehaviour
{
    [Header("Water Pickup")]
    public int healthAmount = 20;
    public float weightAmountKg = 1f;

    [Header("VFX / SFX")]
    public GameObject collectVfxPrefab;
    public AudioClip collectSfx;

    [Tooltip("Now goes up to 40, so max is 4x louder than before.")]
    [Range(0f, 40f)]
    public float collectSfxVolume = 3f;

    [Header("VFX Placement")]
    public Vector3 vfxOffset = Vector3.zero;
    public Vector3 vfxRotationEuler = Vector3.zero;
    public float vfxLifetime = 5f;

    public bool destroyOnPickup = true;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
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

        SpawnVFX();

        if (collectSfx != null && collectSfxVolume > 0f)
            AudioSource.PlayClipAtPoint(collectSfx, transform.position, collectSfxVolume);

        if (destroyOnPickup)
            Destroy(gameObject);
    }

    private void SpawnVFX()
    {
        if (collectVfxPrefab == null)
            return;

        Vector3 spawnPos = transform.position + transform.TransformDirection(vfxOffset);
        Quaternion spawnRot = Quaternion.Euler(vfxRotationEuler);

        GameObject vfx = Instantiate(collectVfxPrefab, spawnPos, spawnRot);

        ParticleSystem ps = vfx.GetComponentInChildren<ParticleSystem>();
        if (ps != null)
            ps.Play(true);

        if (vfxLifetime > 0f)
            Destroy(vfx, vfxLifetime);
    }
}