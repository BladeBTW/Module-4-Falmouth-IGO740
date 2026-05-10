using UnityEngine;

public class CryingChickenAura : MonoBehaviour
{
    [Header("Area")]
    public float radius = 3f;
    public LayerMask playerLayer;
    public string playerTag = "Player";

    [Header("Anxiety")]
    public bool increasePlayerAnxiety = true;
    public float anxietyIncreasePerSecond = 8f;

    [Header("Player Anxiety Indicator")]
    public bool togglePlayerAnxietyVfx = true;

    [Tooltip("If ON, the indicator only appears while anxiety is actively increasing from this aura.")]
    public bool onlyShowIndicatorWhenAddingAnxiety = true;

    [Header("Area VFX")]
    public GameObject areaVfxPrefab;
    public Transform vfxSpawnPoint;
    public Vector3 vfxOffset = Vector3.zero;
    public Vector3 vfxRotationEuler = Vector3.zero;
    public bool parentVfxToChicken = true;
    public bool autoScaleVfxToRadius = true;
    public float vfxScaleMultiplier = 1f;

    [Header("Sound")]
    public AudioClip auraSound;
    [Range(0f, 5f)] public float volume = 1f;
    public bool loopSound = true;
    public float minDistance = 1f;
    public float maxDistance = 8f;

    private GameObject spawnedVfx;
    private AudioSource audioSource;

    private PlayerAnxiety currentPlayerAnxiety;
    private PlayerAnxietyVFX currentPlayerVfx;

    private bool playerInsideAura;

    private void Awake()
    {
        SpawnVfx();
        SetupAudio();
    }

    private void OnEnable()
    {
        if (spawnedVfx != null)
            spawnedVfx.SetActive(true);

        if (audioSource != null && auraSound != null && loopSound)
            audioSource.Play();
    }

    private void OnDisable()
    {
        if (spawnedVfx != null)
            spawnedVfx.SetActive(false);

        if (audioSource != null)
            audioSource.Stop();

        ClearCurrentPlayer();
    }

    private void Update()
    {
        UpdateAudio();
        UpdateAreaVfxScale();

        FindPlayerInAura();

        bool shouldAddAnxiety =
            playerInsideAura &&
            increasePlayerAnxiety &&
            currentPlayerAnxiety != null;

        if (shouldAddAnxiety)
        {
            currentPlayerAnxiety.AddAnxietyOverTime(
                anxietyIncreasePerSecond
            );
        }

        UpdatePlayerIndicator(shouldAddAnxiety);
    }

    private void FindPlayerInAura()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            radius,
            playerLayer,
            QueryTriggerInteraction.Collide
        );

        PlayerAnxiety foundAnxiety = null;
        PlayerAnxietyVFX foundVfx = null;

        foreach (Collider hit in hits)
        {
            bool isPlayer =
                hit.CompareTag(playerTag) ||
                hit.transform.root.CompareTag(playerTag);

            if (!isPlayer)
                continue;

            foundAnxiety = hit.GetComponentInParent<PlayerAnxiety>();
            foundVfx = hit.GetComponentInParent<PlayerAnxietyVFX>();
            break;
        }

        bool foundPlayer = foundAnxiety != null || foundVfx != null;

        if (!foundPlayer)
        {
            playerInsideAura = false;
            ClearCurrentPlayer();
            return;
        }

        playerInsideAura = true;

        if (foundAnxiety != currentPlayerAnxiety ||
            foundVfx != currentPlayerVfx)
        {
            ClearCurrentPlayer();

            currentPlayerAnxiety = foundAnxiety;
            currentPlayerVfx = foundVfx;
        }
    }

    private void UpdatePlayerIndicator(bool anxietyIsBeingAdded)
    {
        if (!togglePlayerAnxietyVfx)
            return;

        if (currentPlayerVfx == null)
            return;

        bool shouldShowIndicator;

        if (onlyShowIndicatorWhenAddingAnxiety)
        {
            shouldShowIndicator = anxietyIsBeingAdded;
        }
        else
        {
            shouldShowIndicator = playerInsideAura;
        }

        currentPlayerVfx.SetAnxietyVFXActive(
            this,
            shouldShowIndicator
        );
    }

    private void ClearCurrentPlayer()
    {
        if (currentPlayerVfx != null)
        {
            currentPlayerVfx.SetAnxietyVFXActive(
                this,
                false
            );
        }

        currentPlayerAnxiety = null;
        currentPlayerVfx = null;
        playerInsideAura = false;
    }

    private void SpawnVfx()
    {
        if (areaVfxPrefab == null)
            return;

        Transform origin = vfxSpawnPoint != null
            ? vfxSpawnPoint
            : transform;

        Vector3 pos =
            origin.position +
            origin.TransformDirection(vfxOffset);

        Quaternion rot =
            Quaternion.Euler(vfxRotationEuler);

        Transform parent = parentVfxToChicken
            ? transform
            : null;

        spawnedVfx = Instantiate(
            areaVfxPrefab,
            pos,
            rot,
            parent
        );

        if (autoScaleVfxToRadius)
        {
            float diameter = radius * 2f * vfxScaleMultiplier;

            spawnedVfx.transform.localScale =
                new Vector3(diameter, diameter, diameter);
        }

        ParticleSystem ps =
            spawnedVfx.GetComponentInChildren<ParticleSystem>();

        if (ps != null)
            ps.Play(true);
    }

    private void SetupAudio()
    {
        if (auraSound == null)
            return;

        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.clip = auraSound;
        audioSource.loop = loopSound;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;

        UpdateAudio();

        if (loopSound)
            audioSource.Play();
    }

    private void UpdateAudio()
    {
        if (audioSource == null)
            return;

        audioSource.volume = volume;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
    }

    private void UpdateAreaVfxScale()
    {
        if (spawnedVfx == null)
            return;

        if (!autoScaleVfxToRadius)
            return;

        float diameter = radius * 2f * vfxScaleMultiplier;

        spawnedVfx.transform.localScale =
            new Vector3(diameter, diameter, diameter);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}