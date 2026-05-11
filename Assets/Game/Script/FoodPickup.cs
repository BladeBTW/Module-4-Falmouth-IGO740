using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FoodPickup : MonoBehaviour
{
    private static int totalFoodConsumed;

    [Header("Health")]
    public int healthAmount = 40;
    public float weightAmountKg = 0.5f;

    [Header("Anxiety")]
    public float anxietyReductionAmount = 15f;

    [Header("Consumption Limit")]
    [Tooltip("If ON, eating too many food pickups damages the player.")]
    public bool useConsumptionLimit = true;

    [Tooltip("How many food pickups can be safely consumed before penalty starts.")]
    public int maxSafeConsumptions = 3;

    [Tooltip("Damage taken every time food is consumed after Max Safe Consumptions.")]
    public int overConsumeDamage = 10;

    [Header("Over-Consume VFX On Player")]
    public GameObject overConsumeVfxPrefab;

    [Tooltip("Optional direct spawn point. Usually leave this EMPTY on prefabs.")]
    public Transform overConsumeVfxSpawnPoint;

    [Tooltip("Child object name searched on the player when Over Consume Vfx Spawn Point is empty.")]
    public string playerSpawnPointName = "PlayerSpawnPoint";

    public Vector3 overConsumeVfxOffset = new Vector3(0f, 0.7f, 0f);
    public Vector3 overConsumeVfxRotationEuler = Vector3.zero;
    public Vector3 overConsumeVfxScale = Vector3.one;

    public bool parentOverConsumeVfxToPlayer = true;

    [Tooltip("Set to 0 or below if the VFX should stay forever.")]
    public float overConsumeVfxLifetime = 3f;

    [Tooltip("Optional layer name for spawned VFX. Leave empty to ignore.")]
    public string forceOverConsumeVfxLayer = "VFX";

    [Header("Over-Consume SFX")]
    public AudioClip overConsumeSfx;

    [Range(0f, 40f)]
    public float overConsumeSfxVolume = 1f;

    [Header("Normal VFX / SFX")]
    public GameObject collectVfxPrefab;
    public AudioClip collectSfx;

    [Range(0f, 40f)]
    public float collectSfxVolume = 3f;

    public Vector3 vfxOffset = Vector3.zero;
    public Vector3 vfxRotationEuler = Vector3.zero;

    [Tooltip("Set to 0 or below if the normal pickup VFX should stay forever.")]
    public float vfxLifetime = 5f;

    [Header("Pickup")]
    public bool destroyOnPickup = true;

    [Header("Debug")]
    public bool logConsumption = true;

    private bool collected;
    private Collider pickupCollider;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticCount()
    {
        totalFoodConsumed = 0;
    }

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

        totalFoodConsumed++;

        bool isOverConsumed =
            useConsumptionLimit &&
            totalFoodConsumed > maxSafeConsumptions;

        if (logConsumption)
        {
            Debug.Log(
                $"[FoodPickup] Food consumed: {totalFoodConsumed}. Over-consumed: {isOverConsumed}",
                this
            );
        }

        if (isOverConsumed)
        {
            // IMPORTANT:
            // Over-consumed food does NOT heal, does NOT reduce anxiety,
            // and does NOT play normal pickup/heal VFX.
            if (overConsumeDamage > 0)
                playerHealth.TakeDamage(overConsumeDamage, gameObject);

            SpawnOverConsumeVFX(playerHealth.transform);
            PlayOverConsumeSFX(playerHealth.transform.position);
        }
        else
        {
            // Normal safe food pickup behavior.
            if (healthAmount > 0)
                playerHealth.Heal(healthAmount);

            if (weightAmountKg != 0f)
                playerHealth.AddWeight(weightAmountKg);

            PlayerAnxiety playerAnxiety = other.GetComponent<PlayerAnxiety>();

            if (playerAnxiety == null)
                playerAnxiety = other.GetComponentInParent<PlayerAnxiety>();

            if (playerAnxiety != null && anxietyReductionAmount > 0f)
                playerAnxiety.ReduceAnxiety(anxietyReductionAmount);

            SpawnNormalVFX();
            PlayNormalSFX();
        }

        if (destroyOnPickup)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }

    private void SpawnNormalVFX()
    {
        if (collectVfxPrefab == null)
            return;

        Vector3 spawnPos =
            transform.position + transform.TransformDirection(vfxOffset);

        Quaternion spawnRot =
            transform.rotation * Quaternion.Euler(vfxRotationEuler);

        GameObject vfx = Instantiate(
            collectVfxPrefab,
            spawnPos,
            spawnRot
        );

        PlayParticleSystems(vfx);

        if (vfxLifetime > 0f)
            Destroy(vfx, vfxLifetime);
    }

    private void SpawnOverConsumeVFX(Transform playerTransform)
    {
        if (overConsumeVfxPrefab == null || playerTransform == null)
            return;

        Transform origin = overConsumeVfxSpawnPoint;

        if (origin == null && !string.IsNullOrEmpty(playerSpawnPointName))
        {
            Transform foundSpawnPoint = playerTransform.Find(playerSpawnPointName);

            if (foundSpawnPoint != null)
                origin = foundSpawnPoint;
        }

        if (origin == null)
            origin = playerTransform;

        Vector3 spawnPos =
            origin.position + origin.TransformDirection(overConsumeVfxOffset);

        Quaternion spawnRot =
            origin.rotation * Quaternion.Euler(overConsumeVfxRotationEuler);

        GameObject vfx = Instantiate(
            overConsumeVfxPrefab,
            spawnPos,
            spawnRot
        );

        vfx.transform.localScale = overConsumeVfxScale;

        if (parentOverConsumeVfxToPlayer)
            vfx.transform.SetParent(origin, true);

        ForceLayerIfNeeded(vfx, forceOverConsumeVfxLayer);
        PlayParticleSystems(vfx);

        if (overConsumeVfxLifetime > 0f)
            Destroy(vfx, overConsumeVfxLifetime);
    }

    private void PlayNormalSFX()
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

    private void PlayOverConsumeSFX(Vector3 position)
    {
        if (overConsumeSfx == null)
            return;

        if (overConsumeSfxVolume <= 0f)
            return;

        AudioSource.PlayClipAtPoint(
            overConsumeSfx,
            position,
            overConsumeSfxVolume
        );
    }

    private void PlayParticleSystems(GameObject obj)
    {
        if (obj == null)
            return;

        ParticleSystem[] particleSystems =
            obj.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem ps in particleSystems)
        {
            if (ps != null)
                ps.Play(true);
        }
    }

    private void ForceLayerIfNeeded(GameObject obj, string layerName)
    {
        if (obj == null)
            return;

        if (string.IsNullOrWhiteSpace(layerName))
            return;

        int layer = LayerMask.NameToLayer(layerName);

        if (layer < 0)
            return;

        SetLayerRecursively(obj, layer);
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null)
            return;

        obj.layer = layer;

        foreach (Transform child in obj.transform)
        {
            if (child != null)
                SetLayerRecursively(child.gameObject, layer);
        }
    }
}