using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 200;
    public int startingHealth = 100;
    public int currentHealth;

    [Header("Anxiety Locked Health")]
    [SerializeField] private int anxietyLockedHealthLoss;

    public int AnxietyLockedHealthLoss => anxietyLockedHealthLoss;
    public bool IsRegeneratingAnxietyLockedHealth => isRegeneratingAnxietyLockedHealth;

    public bool IsAnxietyLockedHealthFlashing
    {
        get
        {
            return anxietyLockedHealthLoss > 0 &&
                   IsAnxietyDamageFlashActive &&
                   !isRegeneratingAnxietyLockedHealth;
        }
    }

    public bool IsAnxietyDamageFlashActive
    {
        get
        {
            return Time.time < anxietyDamageFlashUntil &&
                   !isRegeneratingAnxietyLockedHealth;
        }
    }

    public bool IsAnxietyRegenFlashActive
    {
        get
        {
            return Time.time < anxietyRegenFlashUntil;
        }
    }

    public float AnxietyRegenFlashStartPercent => anxietyRegenFlashStartPercent;
    public float AnxietyRegenFlashEndPercent => anxietyRegenFlashEndPercent;

    public Color AnxietyDamageFlashColor => anxietyFlashColor;
    public float AnxietyDamageFlashDuration => anxietyFlashDuration;
    public float AnxietyDamageFlashEmissionBoost => anxietyFlashEmissionBoost;

    public Color AnxietyLockedHealthBaseColor => anxietyLockedHealthBaseColor;
    public Color AnxietyLockedHealthRegenFlashColor => anxietyLockedHealthRegenFlashColor;

    [Tooltip("If ON, health lost through anxiety cannot be restored by normal healing until anxiety has calmed down.")]
    public bool lockHealthLostToAnxiety = true;

    [Header("Anxiety Health Unlocking")]
    [Tooltip("If ON, locked anxiety health slowly becomes healable again once anxiety is low enough.")]
    public bool regenerateAnxietyDamageAtLowAnxiety = true;

    [Tooltip("Locked anxiety health unlocks when current anxiety is at or below this value. Example: 0 = only fully calm, 25 = unlock when anxiety is 25 or lower.")]
    public float unlockWhenAnxietyAtOrBelow = 0.01f;

    [Tooltip("How much locked anxiety health becomes healable again each tick. This does NOT heal the player.")]
    public int anxietyRegenAmountPerTick = 5;

    [Tooltip("How often locked anxiety health becomes healable again while anxiety is low enough.")]
    public float anxietyRegenTickInterval = 1f;

    [Tooltip("How long the newly unlocked health chunk flashes in the UI.")]
    public float anxietyRegenFlashDuration = 0.25f;

    [Header("Anxiety Locked UI Colors")]
    [Tooltip("Default color for health that is locked by anxiety. Usually red.")]
    public Color anxietyLockedHealthBaseColor = Color.red;

    [Tooltip("Color the locked-health section flashes to while anxiety-locked health is becoming healable again. Usually purple.")]
    public Color anxietyLockedHealthRegenFlashColor = new Color(0.65f, 0f, 1f, 1f);

    [Header("Weight")]
    public float startingWeightKg = 1.9f;
    public float currentWeightKg = 1.9f;

    [Header("Animator")]
    public Animator animator;
    public string healthFloatParam = "Health";
    public string hurtBoolParam = "Hurt";
    public string halfHealthBoolParam = "HalfHealth";
    public string deathTriggerName = "Die";

    [Header("Half Health Animation")]
    [Range(0f, 1f)]
    public float halfHealthThreshold = 0.5f;

    [Header("Normal Hurt Behaviour")]
    public float hurtDuration = 0.3f;

    [Range(0f, 1f)]
    public float hurtSpeedMultiplier = 0.65f;

    [Header("Anxiety Damage Behaviour")]
    [Tooltip("Keep this OFF if anxiety damage should not trigger the upper-body Hurt animation.")]
    public bool anxietyDamageTriggersHurtAnimation = false;

    [Tooltip("1 = no movement slow. 0.5 = half speed.")]
    [Range(0f, 1f)]
    public float anxietyDamageSpeedMultiplier = 1f;

    [Header("Normal Damage Camera Shake")]
    public bool shakeCameraOnDamage = true;
    public float damageShakeDuration = 0.12f;
    public float damageShakeStrength = 0.08f;

    [Header("High Anxiety Damage Camera Shake")]
    public bool useHighAnxietyDamageShake = true;

    [Range(0f, 1f)]
    public float highAnxietyShakeThreshold = 0.5f;

    public float highAnxietyDamageShakeDuration = 0.08f;
    public float highAnxietyDamageShakeStrength = 0.025f;

    [Header("Normal Hurt VFX / SFX")]
    public GameObject hurtVfxPrefab;
    public Vector3 hurtVfxRotationEuler = Vector3.zero;
    public float hurtVfxLifetime = 3f;
    public AudioClip hurtSfx;

    [Range(0f, 10f)]
    public float hurtSfxVolume = 1f;

    [Header("Anxiety Hurt VFX / SFX")]
    public GameObject anxietyHurtVfxPrefab;
    public Vector3 anxietyHurtVfxRotationEuler = Vector3.zero;
    public float anxietyHurtVfxLifetime = 3f;
    public AudioClip anxietyHurtSfx;

    [Range(0f, 10f)]
    public float anxietyHurtSfxVolume = 1f;

    public bool useNormalHurtVfxIfAnxietyVfxMissing = false;
    public bool useNormalHurtSfxIfAnxietySfxMissing = true;

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

    [Header("Normal Damage Flash")]
    public Renderer[] targetRenderers;
    public Color flashColor = Color.red;
    public float flashDuration = 0.15f;
    public float flashEmissionBoost = 1f;

    [Header("Anxiety Damage Flash")]
    public Color anxietyFlashColor = new Color(0.65f, 0f, 1f, 1f);

    [Tooltip("Total time for one full anxiety flash: base -> purple -> base.")]
    public float anxietyFlashDuration = 0.15f;

    public float anxietyFlashEmissionBoost = 1f;

    [Header("Death")]
    public float deathGameOverDelay = 2f;

    private bool isDead;
    private bool isRegeneratingAnxietyLockedHealth;

    private Character character;
    private PlayerAnxiety playerAnxiety;

    private float baseMoveSpeed;
    private bool hasBaseMoveSpeed;

    private Material[] materials;
    private Color[] originalBaseColors;
    private Color[] originalEmissionColors;
    private bool hasMaterials;

    private Coroutine hurtRoutine;
    private Coroutine flashRoutine;

    private float nextAnxietyRegenTickTime;

    private float anxietyDamageFlashStartTime;
    private float anxietyDamageFlashUntil;

    private float anxietyRegenFlashStartPercent;
    private float anxietyRegenFlashEndPercent;
    private float anxietyRegenFlashStartTime;
    private float anxietyRegenFlashUntil;

    private void Awake()
    {
        currentHealth = Mathf.Clamp(startingHealth, 0, maxHealth);
        currentWeightKg = startingWeightKg;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        character = GetComponent<Character>();
        playerAnxiety = GetComponent<PlayerAnxiety>();

        if (character != null)
        {
            baseMoveSpeed = character.MoveSpeed;
            hasBaseMoveSpeed = true;
        }

        SetupRenderersForFlash();
        UpdateAnimatorHealth();
    }

    private void Update()
    {
        UnlockAnxietyLockedHealthIfCalm();
    }

    public int GetHealableMaxHealth()
    {
        if (!lockHealthLostToAnxiety)
            return maxHealth;

        return Mathf.Max(0, maxHealth - anxietyLockedHealthLoss);
    }

    public float GetHealthPercent()
    {
        if (maxHealth <= 0)
            return 0f;

        return (float)currentHealth / maxHealth;
    }

    public float GetAnxietyLockedHealthPercent()
    {
        if (maxHealth <= 0)
            return 0f;

        return (float)anxietyLockedHealthLoss / maxHealth;
    }

    public float GetHealthPlusLockedAnxietyPercent()
    {
        if (maxHealth <= 0)
            return 0f;

        return (float)(currentHealth + anxietyLockedHealthLoss) / maxHealth;
    }

    public float GetSyncedAnxietyFlashPhase()
    {
        float duration = Mathf.Max(0.01f, anxietyFlashDuration);

        if (!IsAnxietyDamageFlashActive)
            return 0f;

        float elapsed = Time.time - anxietyDamageFlashStartTime;
        float normalized = Mathf.Clamp01(elapsed / duration);

        return Mathf.Sin(normalized * Mathf.PI);
    }

    public float GetAnxietyRegenFlashPhase()
    {
        float duration = Mathf.Max(0.01f, anxietyRegenFlashDuration);

        if (!IsAnxietyRegenFlashActive)
            return 0f;

        float elapsed = Time.time - anxietyRegenFlashStartTime;
        float normalized = Mathf.Clamp01(elapsed / duration);

        return Mathf.Sin(normalized * Mathf.PI);
    }

    public float GetAnxietyRegenFlashFade()
    {
        float duration = Mathf.Max(0.01f, anxietyRegenFlashDuration);

        if (!IsAnxietyRegenFlashActive)
            return 0f;

        float elapsed = Time.time - anxietyRegenFlashStartTime;
        float normalized = Mathf.Clamp01(elapsed / duration);

        return 1f - normalized;
    }

    public float GetAnxietyLockedFlashProgress()
    {
        return GetSyncedAnxietyFlashPhase();
    }

    public Color GetBoostedAnxietyFlashColor()
    {
        Color boosted = anxietyFlashColor * Mathf.Max(1f, anxietyFlashEmissionBoost);
        boosted.a = anxietyFlashColor.a;
        return boosted;
    }

    public Color GetCurrentAnxietyLockedHealthUIColor()
    {
        if (IsAnxietyRegenFlashActive)
        {
            float t = GetAnxietyRegenFlashPhase();

            return Color.Lerp(
                anxietyLockedHealthBaseColor,
                anxietyLockedHealthRegenFlashColor,
                t
            );
        }

        if (IsAnxietyLockedHealthFlashing)
        {
            float t = GetSyncedAnxietyFlashPhase();

            return Color.Lerp(
                anxietyLockedHealthBaseColor,
                anxietyFlashColor,
                t
            );
        }

        return anxietyLockedHealthBaseColor;
    }

    public void TakeDamage(int amount)
    {
        TakeDamage(amount, null);
    }

    public void TakeDamage(int amount, GameObject source)
    {
        ApplyDamage(amount, source, false);
    }

    public void TakeAnxietyDamage(int amount)
    {
        TakeAnxietyDamage(amount, null);
    }

    public void TakeAnxietyDamage(int amount, GameObject source)
    {
        ApplyDamage(amount, source, true);
    }

    private void ApplyDamage(int amount, GameObject source, bool isAnxietyDamage)
    {
        if (isDead || amount <= 0)
            return;

        if (logDamageSource)
        {
            string sourceName = source != null ? source.name : "UNKNOWN";
            string sourcePos = source != null
                ? source.transform.position.ToString()
                : "no position";

            string damageType = isAnxietyDamage ? "ANXIETY" : "NORMAL";

            Debug.Log(
                $"[DAMAGE:{damageType}] Player took {amount} damage from {sourceName} at {sourcePos}",
                source
            );
        }

        int previousHealth = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - amount);
        int actualDamage = previousHealth - currentHealth;

        if (isAnxietyDamage && lockHealthLostToAnxiety && actualDamage > 0)
        {
            anxietyLockedHealthLoss = Mathf.Clamp(
                anxietyLockedHealthLoss + actualDamage,
                0,
                maxHealth
            );

            anxietyDamageFlashStartTime = Time.time;
            anxietyDamageFlashUntil = anxietyDamageFlashStartTime + anxietyFlashDuration;
            isRegeneratingAnxietyLockedHealth = false;
        }

        UpdateAnimatorHealth();

        if (isAnxietyDamage)
        {
            FlashDamageColor(
                anxietyFlashColor,
                anxietyFlashDuration,
                anxietyFlashEmissionBoost,
                true
            );

            PlayAnxietyHurtEffects();
        }
        else
        {
            FlashDamageColor(
                flashColor,
                flashDuration,
                flashEmissionBoost,
                false
            );

            PlayNormalHurtEffects();
        }

        PlayDamageCameraShake();

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        if (isAnxietyDamage)
        {
            if (anxietyDamageTriggersHurtAnimation)
                TriggerHurt(anxietyDamageSpeedMultiplier);

            return;
        }

        TriggerHurt(hurtSpeedMultiplier);
    }

    public void Heal(int amount)
    {
        if (isDead || amount <= 0)
            return;

        int healableMax = GetHealableMaxHealth();

        currentHealth = Mathf.Clamp(
            currentHealth + amount,
            0,
            healableMax
        );

        UpdateAnimatorHealth();
        PlayHealEffects();
    }

    public void AddWeight(float amountKg)
    {
        currentWeightKg += amountKg;
    }

    private void UnlockAnxietyLockedHealthIfCalm()
    {
        isRegeneratingAnxietyLockedHealth = false;

        if (isDead)
            return;

        if (!lockHealthLostToAnxiety)
            return;

        if (!regenerateAnxietyDamageAtLowAnxiety)
            return;

        if (anxietyLockedHealthLoss <= 0)
            return;

        if (playerAnxiety == null)
            playerAnxiety = GetComponent<PlayerAnxiety>();

        if (playerAnxiety == null)
            return;

        if (playerAnxiety.currentAnxiety > unlockWhenAnxietyAtOrBelow)
        {
            nextAnxietyRegenTickTime = 0f;
            return;
        }

        isRegeneratingAnxietyLockedHealth = true;

        anxietyDamageFlashStartTime = 0f;
        anxietyDamageFlashUntil = 0f;

        if (Time.time < nextAnxietyRegenTickTime)
            return;

        float safeInterval = Mathf.Max(0.01f, anxietyRegenTickInterval);
        nextAnxietyRegenTickTime = Time.time + safeInterval;

        int unlockAmount = Mathf.Max(1, anxietyRegenAmountPerTick);

        int previousLockedLoss = anxietyLockedHealthLoss;

        int actualUnlock = Mathf.Min(
            unlockAmount,
            anxietyLockedHealthLoss
        );

        if (actualUnlock <= 0)
            return;

        // Important:
        // This does NOT heal currentHealth.
        // It only removes the locked/unhealable health penalty.
        anxietyLockedHealthLoss -= actualUnlock;

        StartAnxietyUnlockFlash(previousLockedLoss, anxietyLockedHealthLoss);

        UpdateAnimatorHealth();
    }

    private void StartAnxietyUnlockFlash(int oldLockedLoss, int newLockedLoss)
    {
        if (maxHealth <= 0)
            return;

        if (newLockedLoss >= oldLockedLoss)
            return;

        float oldLockedStartPercent =
            Mathf.Clamp01((float)(currentHealth + newLockedLoss) / maxHealth);

        float oldLockedEndPercent =
            Mathf.Clamp01((float)(currentHealth + oldLockedLoss) / maxHealth);

        anxietyRegenFlashStartPercent = oldLockedStartPercent;
        anxietyRegenFlashEndPercent = oldLockedEndPercent;

        anxietyRegenFlashStartTime = Time.time;
        anxietyRegenFlashUntil =
            anxietyRegenFlashStartTime + anxietyRegenFlashDuration;
    }

    private void PlayDamageCameraShake()
    {
        if (CameraShake.Instance == null)
            return;

        if (playerAnxiety == null)
            playerAnxiety = GetComponent<PlayerAnxiety>();

        bool anxietyIsHigh = false;

        if (playerAnxiety != null)
        {
            anxietyIsHigh =
                playerAnxiety.GetAnxietyPercent() >= highAnxietyShakeThreshold;
        }

        if (anxietyIsHigh && useHighAnxietyDamageShake)
        {
            CameraShake.Instance.Shake(
                highAnxietyDamageShakeDuration,
                highAnxietyDamageShakeStrength
            );

            return;
        }

        if (shakeCameraOnDamage)
        {
            CameraShake.Instance.Shake(
                damageShakeDuration,
                damageShakeStrength
            );
        }
    }

    private void TriggerHurt(float speedMultiplier)
    {
        if (hurtRoutine != null)
            StopCoroutine(hurtRoutine);

        hurtRoutine = StartCoroutine(HurtRoutine(speedMultiplier));
    }

    private IEnumerator HurtRoutine(float speedMultiplier)
    {
        if (animator != null && !string.IsNullOrEmpty(hurtBoolParam))
            animator.SetBool(hurtBoolParam, true);

        if (character != null && hasBaseMoveSpeed)
        {
            character.MoveSpeed =
                baseMoveSpeed * Mathf.Clamp01(speedMultiplier);
        }

        yield return new WaitForSeconds(hurtDuration);

        if (animator != null && !string.IsNullOrEmpty(hurtBoolParam))
            animator.SetBool(hurtBoolParam, false);

        if (!isDead && character != null && hasBaseMoveSpeed)
            character.MoveSpeed = baseMoveSpeed;

        hurtRoutine = null;
    }

    private void PlayNormalHurtEffects()
    {
        SpawnVFX(hurtVfxPrefab, hurtVfxRotationEuler, hurtVfxLifetime);

        if (hurtSfx != null && hurtSfxVolume > 0f)
            AudioSource.PlayClipAtPoint(hurtSfx, transform.position, hurtSfxVolume);
    }

    private void PlayAnxietyHurtEffects()
    {
        if (anxietyHurtVfxPrefab != null)
        {
            SpawnVFX(anxietyHurtVfxPrefab, anxietyHurtVfxRotationEuler, anxietyHurtVfxLifetime);
        }
        else if (useNormalHurtVfxIfAnxietyVfxMissing)
        {
            SpawnVFX(hurtVfxPrefab, hurtVfxRotationEuler, hurtVfxLifetime);
        }

        if (anxietyHurtSfx != null && anxietyHurtSfxVolume > 0f)
        {
            AudioSource.PlayClipAtPoint(anxietyHurtSfx, transform.position, anxietyHurtSfxVolume);
        }
        else if (useNormalHurtSfxIfAnxietySfxMissing &&
                 hurtSfx != null &&
                 hurtSfxVolume > 0f)
        {
            AudioSource.PlayClipAtPoint(hurtSfx, transform.position, hurtSfxVolume);
        }
    }

    private void PlayHealEffects()
    {
        SpawnVFX(healVfxPrefab, healVfxRotationEuler, healVfxLifetime);

        if (healSfx != null && healSfxVolume > 0f)
            AudioSource.PlayClipAtPoint(healSfx, transform.position, healSfxVolume);
    }

    private void SpawnVFX(GameObject prefab, Vector3 rotationEuler, float lifetime)
    {
        if (prefab == null)
            return;

        Transform origin = vfxSpawnPoint != null ? vfxSpawnPoint : transform;

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

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        ResetDamageColor();

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
        if (animator == null)
            return;

        if (!string.IsNullOrEmpty(healthFloatParam))
            animator.SetFloat(healthFloatParam, currentHealth);

        if (!string.IsNullOrEmpty(halfHealthBoolParam))
        {
            bool isHalfHealth =
                currentHealth <= maxHealth * halfHealthThreshold;

            animator.SetBool(halfHealthBoolParam, isHalfHealth);
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

    private void FlashDamageColor(
        Color color,
        float duration,
        float emissionBoost,
        bool useSyncedAnxietyClock
    )
    {
        if (!hasMaterials || materials == null)
            return;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(
            FlashDamageColorRoutine(
                color,
                duration,
                emissionBoost,
                useSyncedAnxietyClock
            )
        );
    }

    private IEnumerator FlashDamageColorRoutine(
        Color color,
        float duration,
        float emissionBoost,
        bool useSyncedAnxietyClock
    )
    {
        float startTime = Time.time;
        float safeDuration = Mathf.Max(0.01f, duration);

        Color boostedColor = color * Mathf.Max(1f, emissionBoost);
        boostedColor.a = color.a;

        while (Time.time < startTime + safeDuration)
        {
            float phase;

            if (useSyncedAnxietyClock)
            {
                float elapsed = Time.time - anxietyDamageFlashStartTime;
                float normalized = Mathf.Clamp01(elapsed / safeDuration);

                phase = Mathf.Sin(normalized * Mathf.PI);
            }
            else
            {
                float elapsed = Time.time - startTime;
                float normalized = Mathf.Clamp01(elapsed / safeDuration);

                phase = Mathf.Sin(normalized * Mathf.PI);
            }

            for (int i = 0; i < materials.Length; i++)
            {
                Material mat = materials[i];

                if (mat == null)
                    continue;

                Color baseColor = Color.Lerp(originalBaseColors[i], color, phase);
                Color emissionColor = Color.Lerp(originalEmissionColors[i], boostedColor, phase);

                SetBaseColor(mat, baseColor);
                SetEmissionColor(mat, emissionColor);
            }

            yield return null;
        }

        ResetDamageColor();
        flashRoutine = null;
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