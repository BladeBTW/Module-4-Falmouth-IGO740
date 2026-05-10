using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FoodPickup : MonoBehaviour
{
    [Header("Health")]
    public int healthAmount = 40;
    public float weightAmountKg = 0.5f;

    [Header("Anxiety")]
    public float anxietyReductionAmount = 15f;

    [Header("VFX / SFX")]
    public GameObject collectVfxPrefab;
    public AudioClip collectSfx;

    [Range(0f, 10f)]
    public float collectSfxVolume = 3f;

    public Vector3 vfxOffset = Vector3.zero;
    public Vector3 vfxRotationEuler = Vector3.zero;
    public float vfxLifetime = 5f;

    [Header("Pickup")]
    public bool destroyOnPickup = true;

    private bool collected;
    private Collider pickupCollider;

    private void Awake()
    {
        pickupCollider = GetComponent<Collider>();

        if (pickupCollider != null)
            pickupCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected)
            return;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();

        if (playerHealth == null)
            playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null)
            return;

        collected = true;

        if (pickupCollider != null)
            pickupCollider.enabled = false;

        if (healthAmount > 0)
            playerHealth.Heal(healthAmount);

        if (weightAmountKg != 0f)
            playerHealth.AddWeight(weightAmountKg);

        PlayerAnxiety playerAnxiety = other.GetComponent<PlayerAnxiety>();

        if (playerAnxiety == null)
            playerAnxiety = other.GetComponentInParent<PlayerAnxiety>();

        if (playerAnxiety != null && anxietyReductionAmount > 0f)
            playerAnxiety.ReduceAnxiety(anxietyReductionAmount);

        SpawnVFX();
        PlaySFX();

        if (destroyOnPickup)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }

    private void SpawnVFX()
    {
        if (collectVfxPrefab == null)
            return;

        Vector3 spawnPos =
            transform.position + transform.TransformDirection(vfxOffset);

        Quaternion spawnRot =
            Quaternion.Euler(vfxRotationEuler);

        GameObject vfx = Instantiate(
            collectVfxPrefab,
            spawnPos,
            spawnRot
        );

        ParticleSystem ps = vfx.GetComponentInChildren<ParticleSystem>();

        if (ps != null)
            ps.Play(true);

        if (vfxLifetime > 0f)
            Destroy(vfx, vfxLifetime);
    }

    private void PlaySFX()
    {
        if (collectSfx == null)
            return;

        if (collectSfxVolume <= 0f)
            return;

        AudioSource.PlayClipAtPoint(
            collectSfx,
            transform.position,
            collectSfxVolume
        );
    }
}