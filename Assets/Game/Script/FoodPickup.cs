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

        if (collectVfxPrefab != null)
        {
            GameObject vfx = Instantiate(
                collectVfxPrefab,
                transform.position,
                Quaternion.identity
            );

            Destroy(vfx, 5f);
        }

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
}
