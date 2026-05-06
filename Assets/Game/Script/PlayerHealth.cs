using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 200;
    public int startingHealth = 100;
    public int currentHealth;

    [Header("Weight")]
    public float startingWeightKg = 1.9f;
    public float currentWeightKg = 1.9f;

    [Header("Animator")]
    public Animator animator;
    public string healthFloatParam = "Health";
    public string hurtBoolParam = "Hurt";
    public string deathTriggerName = "Die";

    [Header("Hurt Behaviour")]
    public float hurtDuration = 0.3f;

    [Range(0f, 1f)]
    public float hurtSpeedMultiplier = 0.65f;

    [Header("Camera Shake")]
    public bool shakeCameraOnDamage = true;
    public float damageShakeDuration = 0.12f;
    public float damageShakeStrength = 0.08f;

    [Header("Hurt VFX / SFX")]
    public GameObject hurtVfxPrefab;
    public Vector3 hurtVfxRotationEuler = Vector3.zero;
    public float hurtVfxLifetime = 3f;
    public AudioClip hurtSfx;

    [Range(0f, 10f)]
    public float hurtSfxVolume = 1f;

    [Header("Heal VFX / SFX")]
    public GameObject healVfxPrefab;
    public Vector3 healVfxRotationEuler = Vector3.zero;
    public float healVfxLifetime = 3f;
    public AudioClip healSfx;

    [Range(0f, 10f)]
    public float healSfxVolume = 0.4f;

    [Header("VFX Spawn")]
    public Transform vfxSpawnPoint;
    public Vector3 vfxSpawnOffset = new Vector3(0f, 0.5f, 0f);

    [Header("Damage Debug")]
    public bool logDamageSource = true;

    [Header("Damage Flash")]
    public Renderer[] targetRenderers;
    public Color flashColor = Color.red;
    public float flashDuration = 0.15f;
    public float flashEmissionBoost = 3f;

    [Header("Death")]
    public float deathGameOverDelay = 2f;

    private bool isDead;
    private Character character;
    private float baseMoveSpeed;
    private bool hasBaseMoveSpeed;

    private Material[] materials;
    private Color[] originalBaseColors;
    private Color[] originalEmissionColors;
    private bool hasMaterials;

    private Coroutine hurtRoutine;

    private void Awake()
    {
        currentHealth = Mathf.Clamp(startingHealth, 0, maxHealth);
        currentWeightKg = startingWeightKg;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        character = GetComponent<Character>();

        if (character != null)
        {
            baseMoveSpeed = character.MoveSpeed;
            hasBaseMoveSpeed = true;
        }

        SetupRenderersForFlash();
        UpdateAnimatorHealth();
    }

    public void TakeDamage(int amount)
    {
        TakeDamage(amount, null);
    }

    public void TakeDamage(int amount, GameObject source)
    {
        if (isDead || amount <= 0)
            return;

        if (logDamageSource)
        {
            string sourceName = source != null ? source.name : "UNKNOWN";
            string sourcePos = source != null
                ? source.transform.position.ToString()
                : "no position";

            Debug.Log($"[DAMAGE] Player took {amount} damage from {sourceName} at {sourcePos}", source);
        }

        currentHealth = Mathf.Max(0, currentHealth - amount);

        UpdateAnimatorHealth();

        FlashDamageColor();
        PlayHurtEffects();

        if (shakeCameraOnDamage && CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(
                damageShakeDuration,
                damageShakeStrength
            );
        }

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            TriggerHurt();
        }
    }

    public void Heal(int amount)
    {
        if (isDead || amount <= 0)
            return;

        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);

        UpdateAnimatorHealth();

        PlayHealEffects();
    }

    public void AddWeight(float amountKg)
    {
        currentWeightKg += amountKg;
    }

    private void TriggerHurt()
    {
        if (hurtRoutine != null)
            StopCoroutine(hurtRoutine);

        hurtRoutine = StartCoroutine(HurtRoutine());
    }

    private IEnumerator HurtRoutine()
    {
        if (animator != null && !string.IsNullOrEmpty(hurtBoolParam))
            animator.SetBool(hurtBoolParam, true);

        if (character != null && hasBaseMoveSpeed)
        {
            character.MoveSpeed =
                baseMoveSpeed * Mathf.Clamp01(hurtSpeedMultiplier);
        }

        yield return new WaitForSeconds(hurtDuration);

        if (animator != null && !string.IsNullOrEmpty(hurtBoolParam))
            animator.SetBool(hurtBoolParam, false);

        if (!isDead && character != null && hasBaseMoveSpeed)
        {
            character.MoveSpeed = baseMoveSpeed;
        }

        hurtRoutine = null;
    }

    private void PlayHurtEffects()
    {
        SpawnVFX(
            hurtVfxPrefab,
            hurtVfxRotationEuler,
            hurtVfxLifetime
        );

        if (hurtSfx != null && hurtSfxVolume > 0f)
        {
            AudioSource.PlayClipAtPoint(
                hurtSfx,
                transform.position,
                hurtSfxVolume
            );
        }
    }

    private void PlayHealEffects()
    {
        SpawnVFX(
            healVfxPrefab,
            healVfxRotationEuler,
            healVfxLifetime
        );

        if (healSfx != null && healSfxVolume > 0f)
        {
            AudioSource.PlayClipAtPoint(
                healSfx,
                transform.position,
                healSfxVolume
            );
        }
    }

    private void SpawnVFX(
        GameObject prefab,
        Vector3 rotationEuler,
        float lifetime
    )
    {
        if (prefab == null)
            return;

        Transform origin =
            vfxSpawnPoint != null
            ? vfxSpawnPoint
            : transform;

        Vector3 pos =
            origin.position +
            origin.TransformDirection(vfxSpawnOffset);

        Quaternion rot = Quaternion.Euler(rotationEuler);

        GameObject vfx = Instantiate(prefab, pos, rot);

        ParticleSystem ps = vfx.GetComponentInChildren<ParticleSystem>();

        if (ps != null)
            ps.Play(true);

        if (lifetime > 0f)
            Destroy(vfx, lifetime);
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        if (hurtRoutine != null)
        {
            StopCoroutine(hurtRoutine);
            hurtRoutine = null;
        }

        if (animator != null && !string.IsNullOrEmpty(hurtBoolParam))
            animator.SetBool(hurtBoolParam, false);

        if (DeathCounter.Instance != null)
            DeathCounter.Instance.RegisterDeath();

        if (character != null)
            character.MoveSpeed = 0f;

        if (animator != null && !string.IsNullOrEmpty(deathTriggerName))
            animator.SetTrigger(deathTriggerName);

        StartCoroutine(DeathGameOverSequence());
    }

    private IEnumerator DeathGameOverSequence()
    {
        yield return new WaitForSecondsRealtime(deathGameOverDelay);

        if (GameUIManager.Instance != null)
            GameUIManager.Instance.ShowGameOver();
    }

    private void UpdateAnimatorHealth()
    {
        if (animator != null && !string.IsNullOrEmpty(healthFloatParam))
        {
            animator.SetFloat(
                healthFloatParam,
                currentHealth
            );
        }
    }

    private void SetupRenderersForFlash()
    {
        Renderer[] renderers =
            targetRenderers != null && targetRenderers.Length > 0
            ? targetRenderers
            : GetComponentsInChildren<Renderer>();

        materials = new Material[renderers.Length];
        originalBaseColors = new Color[renderers.Length];
        originalEmissionColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];

            if (r == null || r is ParticleSystemRenderer)
                continue;

            Material mat = r.material;

            if (mat == null)
                continue;

            materials[i] = mat;
            originalBaseColors[i] = GetBaseColor(mat);
            originalEmissionColors[i] = GetEmissionColor(mat);

            hasMaterials = true;
        }
    }

    private void FlashDamageColor()
    {
        if (!hasMaterials || materials == null)
            return;

        CancelInvoke(nameof(ResetDamageColor));

        foreach (Material mat in materials)
        {
            if (mat == null)
                continue;

            SetBaseColor(mat, flashColor);
            SetEmissionColor(
                mat,
                flashColor * flashEmissionBoost
            );
        }

        Invoke(nameof(ResetDamageColor), flashDuration);
    }

    private void ResetDamageColor()
    {
        if (!hasMaterials || materials == null)
            return;

        for (int i = 0; i < materials.Length; i++)
        {
            Material mat = materials[i];

            if (mat == null)
                continue;

            SetBaseColor(mat, originalBaseColors[i]);
            SetEmissionColor(mat, originalEmissionColors[i]);
        }
    }

    private static Color GetBaseColor(Material m)
    {
        if (m.HasProperty("_BaseColor"))
            return m.GetColor("_BaseColor");

        if (m.HasProperty("_Color"))
            return m.GetColor("_Color");

        return Color.white;
    }

    private static void SetBaseColor(Material m, Color c)
    {
        if (m.HasProperty("_BaseColor"))
            m.SetColor("_BaseColor", c);
        else if (m.HasProperty("_Color"))
            m.SetColor("_Color", c);
    }

    private static Color GetEmissionColor(Material m)
    {
        if (m.HasProperty("_EmissionColor"))
            return m.GetColor("_EmissionColor");

        if (m.HasProperty("_EmissiveColor"))
            return m.GetColor("_EmissiveColor");

        return Color.black;
    }

    private static void SetEmissionColor(Material m, Color c)
    {
        if (m.HasProperty("_EmissionColor"))
        {
            m.SetColor("_EmissionColor", c);
            m.EnableKeyword("_EMISSION");
        }
        else if (m.HasProperty("_EmissiveColor"))
        {
            m.SetColor("_EmissiveColor", c);
            m.EnableKeyword("_EMISSIVE_COLOR");
        }
    }
}