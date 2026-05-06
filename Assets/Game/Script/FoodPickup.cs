using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FoodPickup : MonoBehaviour
{
    [Header("Food Pickup")]
    public int healthAmount = 40;
    public float weightAmountKg = 0.5f;

    [Header("VFX / SFX")]
    [Tooltip("Prefab with ParticleSystem or VisualEffect on it.")]
    public GameObject collectVfxPrefab;

    public AudioClip collectSfx;

    [Tooltip("1 = normal, 2 = loud, 5 = very loud, 10 = extreme")]
    [Range(0f, 10f)]
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

        // Gameplay
        if (healthAmount > 0)
            playerHealth.Heal(healthAmount);

        if (weightAmountKg != 0f)
            playerHealth.AddWeight(weightAmountKg);

        // VFX
        SpawnVFX();

        // SFX
        if (collectSfx != null && collectSfxVolume > 0f)
        {
            AudioSource.PlayClipAtPoint(
                collectSfx,
                transform.position,
                collectSfxVolume
            );
        }

        if (destroyOnPickup)
            Destroy(gameObject);
    }

    private void SpawnVFX()
    {
        if (collectVfxPrefab == null)
            return;

        Vector3 spawnPos =
            transform.position +
            transform.TransformDirection(vfxOffset);

        Quaternion spawnRot =
            Quaternion.Euler(vfxRotationEuler);

        GameObject vfx = Instantiate(
            collectVfxPrefab,
            spawnPos,
            spawnRot
        );

        ParticleSystem ps =
            vfx.GetComponentInChildren<ParticleSystem>();

        if (ps != null)
            ps.Play(true);

        Destroy(vfx, vfxLifetime);
    }
}