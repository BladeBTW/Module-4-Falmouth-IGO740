using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerAnxiety : MonoBehaviour
{
    [Header("Anxiety")]
    public float maxAnxiety = 100f;
    public float startingAnxiety = 0f;

    // Kept public because older scripts in your project access:
    // playerAnxiety.currentAnxiety
    public float currentAnxiety;

    public float CurrentAnxiety => currentAnxiety;
    public float NormalizedAnxiety => maxAnxiety <= 0f ? 0f : currentAnxiety / maxAnxiety;

    [Header("Natural Recovery")]
    public bool anxietyFallsOverTime = true;

    [Tooltip("How much anxiety is removed per second.")]
    public float anxietyFallPerSecond = 5f;

    [Header("Overflow Anxiety Damage")]
    [Tooltip("If true, anxiety gain beyond Max Anxiety damages the player.")]
    public bool overflowAnxietyDamagesHealth = true;

    [Tooltip("How much overflow anxiety equals 1 damage.")]
    public float overflowDamagePerAnxietyPoint = 1f;

    [Tooltip("If true, fractional overflow damage is saved until it reaches 1.")]
    public bool accumulateFractionalOverflowDamage = true;

    [Header("Overflow Damage Safety")]
    public bool useOverflowDamageTicks = false;
    public float overflowDamageTickInterval = 1f;
    public int maxOverflowDamagePerTick = 1;
    public bool clearQueuedOverflowDamageWhenBelowMax = true;
    public bool logOverflowDamage = false;

    [Header("Old High Anxiety Tick Damage")]
    public bool damageAtHighAnxiety = false;

    [Range(0f, 1f)]
    public float damageThreshold = 0.5f;

    public int anxietyDamagePerTick = 5;
    public float anxietyDamageTickInterval = 1f;

    [Header("Animator Anxiety State")]
    public Animator animator;
    public string anxietyBoolParam = "Anxiety";

    [Range(0f, 1f)]
    public float anxietyAnimationEnterThreshold = 0.5f;

    public bool useAnxietyExitThreshold = true;

    [Range(0f, 1f)]
    public float anxietyAnimationExitThreshold = 0.35f;

    [Header("UI")]
    public Slider anxietySlider;
    public Image normalFillImage;
    public Image highFillImage;

    [Header("UI Threshold")]
    [Range(0f, 1f)]
    public float highAnxietyThreshold = 0.5f;

    public Color normalAnxietyColor = new Color(0.6f, 0f, 1f, 1f);
    public Color highAnxietyColor = Color.red;

    [Header("High Anxiety Flash")]
    public bool flashHighAnxietyFill = true;

    [Tooltip("Kept for compatibility. This script now flashes directly when anxiety is high.")]
    public bool syncFlashWithAnxietyDamageFlash = true;

    [Tooltip("If true, the anxiety bar only flashes during old damage flash events. Usually keep OFF.")]
    public bool onlyFlashDuringAnxietyDamageFlash = false;

    public Color highAnxietyFlashColor = new Color(1f, 0.35f, 0.35f, 1f);
    public float flashSpeed = 8f;

    [Header("Player High Anxiety Visual Flash")]
    public bool flashPlayerAtHighAnxiety = true;

    [Tooltip("Renderers that flash when anxiety is high. If empty, they are auto-found in children.")]
    public Renderer[] playerFlashRenderers;

    public Color playerHighAnxietyFlashColor = new Color(0.65f, 0f, 1f, 1f);
    public float playerFlashEmissionBoost = 1.5f;

    [Tooltip("Usually _EmissionColor for URP/Lit. If that does nothing, try _BaseColor or _Color.")]
    public string playerFlashColorProperty = "_EmissionColor";

    [Header("Anxiety Gain VFX")]
    [Tooltip("Particle VFX spawned when anxiety increases enough.")]
    public GameObject anxietyVfxPrefab;

    [Tooltip("Optional spawn point. If empty, this object is used.")]
    public Transform anxietyVfxSpawnPoint;

    public Vector3 anxietyVfxOffset = new Vector3(0f, 0.7f, 0f);
    public Vector3 anxietyVfxRotationEuler = Vector3.zero;
    public Vector3 anxietyVfxScale = Vector3.one;

    [Tooltip("Only play one-shot anxiety VFX if instant anxiety gain is at least this amount.")]
    public float minAnxietyGainForVfx = 1f;

    [Tooltip("How long instant anxiety VFX emits before fading naturally.")]
    public float anxietyVfxEmitTime = 0.2f;

    [Tooltip("Extra time after instant particles stop emitting before destroying the VFX object.")]
    public float anxietyVfxExtraFadeTime = 1f;

    [Tooltip("If true, the spawned VFX follows the player/spawn point.")]
    public bool parentAnxietyVfxToSpawnPoint = true;

    [Tooltip("Optional layer name for spawned VFX. Leave empty to ignore.")]
    public string forceAnxietyVfxLayer = "VFX";

    [Header("Continuous Anxiety VFX")]
    [Tooltip("Use one persistent VFX for AddAnxietyOverTime sources, such as touching chickens.")]
    public bool useContinuousAnxietyVfxForOverTimeSources = false;

    [Tooltip("How long the continuous VFX stays alive after contact anxiety stops.")]
    public float continuousAnxietyVfxGraceTime = 0.25f;

    [Tooltip("How long to let particles fade after stopping emission.")]
    public float continuousAnxietyVfxFadeTime = 1f;

    [Tooltip("If true, contact anxiety also plays the anxiety gain SFX when the continuous VFX starts.")]
    public bool playSfxWhenContinuousVfxStarts = false;

    [Header("Anxiety Gain SFX")]
    public AudioClip anxietyGainSfx;

    [Range(0f, 10f)]
    public float anxietyGainSfxVolume = 1f;

    public AudioSource anxietyAudioSource;

    [Header("Debug")]
    public bool logAnxietyChanges = false;

    private PlayerHealth playerHealth;
    private Coroutine highAnxietyDamageRoutine;

    private bool isInAnxietyAnimation;
    private float accumulatedOverflowDamage;
    private float queuedOverflowDamage;
    private float overflowTickTimer;

    private GameObject activeContinuousAnxietyVfx;
    private ParticleSystem[] activeContinuousAnxietyParticles;
    private Coroutine continuousAnxietyStopRoutine;

    private RectTransform normalFillRect;
    private RectTransform highFillRect;

    private MaterialPropertyBlock playerFlashBlock;
    private Color[] originalRendererBaseColors;
    private Color[] originalRendererEmissionColors;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();

        if (playerHealth == null)
            playerHealth = GetComponentInChildren<PlayerHealth>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        currentAnxiety = Mathf.Clamp(startingAnxiety, 0f, maxAnxiety);

        SetupUI();
        SetupPlayerFlashRenderers();
        UpdateUI();
        UpdatePlayerHighAnxietyFlash();
        UpdateAnimatorState();
    }

    private void OnEnable()
    {
        SetupUI();
        SetupPlayerFlashRenderers();
        UpdateUI();
        UpdatePlayerHighAnxietyFlash();
        UpdateAnimatorState();
    }

    private void OnDisable()
    {
        CleanupContinuousAnxietyVFX();
        ResetPlayerFlash();
    }

    private void Update()
    {
        HandleNaturalRecovery();
        HandleOldHighAnxietyTickDamage();
        HandleQueuedOverflowDamage();

        UpdateUI();
        UpdatePlayerHighAnxietyFlash();
        UpdateAnimatorState();
    }

    // ------------------------------------------------------------
    // COMPATIBILITY METHODS FOR YOUR EXISTING SCRIPTS
    // ------------------------------------------------------------

    public void AddAnxietyOverTime(float amountPerSecond)
    {
        AddAnxietyOverTime(amountPerSecond, 1f);
    }

    public void AddAnxietyOverTime(float amountPerSecond, float multiplier)
    {
        float amount = amountPerSecond * multiplier * Time.deltaTime;

        AddAnxietyInternal(amount, false);

        if (amount > 0f)
            RefreshContinuousAnxietyVFX();
    }

    public bool IsAnimatorAnxietyActive()
    {
        return isInAnxietyAnimation;
    }

    public float GetAnxietyPercent()
    {
        return NormalizedAnxiety;
    }

    public float GetAnxiety01()
    {
        return NormalizedAnxiety;
    }

    public bool IsHighAnxiety()
    {
        return NormalizedAnxiety >= highAnxietyThreshold;
    }

    // ------------------------------------------------------------
    // MAIN ANXIETY API
    // ------------------------------------------------------------

    public void AddAnxiety(float amount)
    {
        AddAnxietyInternal(amount, true);
    }

    private void AddAnxietyInternal(float amount, bool allowOneShotVfx)
    {
        if (amount <= 0f)
            return;

        float previousAnxiety = currentAnxiety;
        float intendedAnxiety = currentAnxiety + amount;
        float overflowAmount = Mathf.Max(0f, intendedAnxiety - maxAnxiety);

        currentAnxiety = Mathf.Clamp(intendedAnxiety, 0f, maxAnxiety);

        float actualGain = Mathf.Max(0f, currentAnxiety - previousAnxiety);

        if (logAnxietyChanges)
        {
            Debug.Log(
                $"[PlayerAnxiety] AddAnxiety {amount}. Actual gain: {actualGain}. Current: {currentAnxiety}/{maxAnxiety}. Overflow: {overflowAmount}",
                this
            );
        }

        if (allowOneShotVfx && actualGain >= minAnxietyGainForVfx)
        {
            SpawnAnxietyVFX();
            PlayAnxietyGainSFX();
        }

        if (overflowAmount > 0f)
            ApplyOverflowAnxietyDamage(overflowAmount);

        UpdateUI();
        UpdatePlayerHighAnxietyFlash();
        UpdateAnimatorState();
    }

    public void ReduceAnxiety(float amount)
    {
        if (amount <= 0f)
            return;

        currentAnxiety = Mathf.Clamp(currentAnxiety - amount, 0f, maxAnxiety);

        if (clearQueuedOverflowDamageWhenBelowMax && currentAnxiety < maxAnxiety)
        {
            queuedOverflowDamage = 0f;
            accumulatedOverflowDamage = 0f;
        }

        if (logAnxietyChanges)
            Debug.Log($"[PlayerAnxiety] ReduceAnxiety {amount}. Current: {currentAnxiety}/{maxAnxiety}", this);

        UpdateUI();
        UpdatePlayerHighAnxietyFlash();
        UpdateAnimatorState();
    }

    public void SetAnxiety(float value)
    {
        currentAnxiety = Mathf.Clamp(value, 0f, maxAnxiety);

        if (clearQueuedOverflowDamageWhenBelowMax && currentAnxiety < maxAnxiety)
        {
            queuedOverflowDamage = 0f;
            accumulatedOverflowDamage = 0f;
        }

        UpdateUI();
        UpdatePlayerHighAnxietyFlash();
        UpdateAnimatorState();
    }

    public void ClearAnxiety()
    {
        currentAnxiety = 0f;
        queuedOverflowDamage = 0f;
        accumulatedOverflowDamage = 0f;

        CleanupContinuousAnxietyVFX();

        UpdateUI();
        UpdatePlayerHighAnxietyFlash();
        UpdateAnimatorState();
    }

    private void HandleNaturalRecovery()
    {
        if (!anxietyFallsOverTime)
            return;

        if (currentAnxiety <= 0f)
            return;

        if (anxietyFallPerSecond <= 0f)
            return;

        currentAnxiety = Mathf.Clamp(
            currentAnxiety - anxietyFallPerSecond * Time.deltaTime,
            0f,
            maxAnxiety
        );

        if (clearQueuedOverflowDamageWhenBelowMax && currentAnxiety < maxAnxiety)
        {
            queuedOverflowDamage = 0f;
            accumulatedOverflowDamage = 0f;
        }
    }

    private void ApplyOverflowAnxietyDamage(float overflowAmount)
    {
        if (!overflowAnxietyDamagesHealth)
            return;

        if (playerHealth == null)
            return;

        if (overflowDamagePerAnxietyPoint <= 0f)
            return;

        float rawDamage = overflowAmount * overflowDamagePerAnxietyPoint;

        if (useOverflowDamageTicks)
        {
            queuedOverflowDamage += rawDamage;

            if (logOverflowDamage)
                Debug.Log($"[PlayerAnxiety] Queued overflow anxiety damage: {queuedOverflowDamage}", this);

            return;
        }

        if (accumulateFractionalOverflowDamage)
        {
            accumulatedOverflowDamage += rawDamage;

            int wholeDamage = Mathf.FloorToInt(accumulatedOverflowDamage);

            if (wholeDamage <= 0)
                return;

            accumulatedOverflowDamage -= wholeDamage;
            DamagePlayerWithAnxietyDamage(wholeDamage);
        }
        else
        {
            int damage = Mathf.RoundToInt(rawDamage);

            if (damage <= 0)
                return;

            DamagePlayerWithAnxietyDamage(damage);
        }
    }

    private void HandleQueuedOverflowDamage()
    {
        if (!useOverflowDamageTicks)
            return;

        if (!overflowAnxietyDamagesHealth)
            return;

        if (queuedOverflowDamage <= 0f)
            return;

        if (playerHealth == null)
            return;

        overflowTickTimer += Time.deltaTime;

        if (overflowTickTimer < overflowDamageTickInterval)
            return;

        overflowTickTimer = 0f;

        int damage = Mathf.FloorToInt(queuedOverflowDamage);

        if (damage <= 0)
            return;

        if (maxOverflowDamagePerTick > 0)
            damage = Mathf.Min(damage, maxOverflowDamagePerTick);

        queuedOverflowDamage -= damage;

        DamagePlayerWithAnxietyDamage(damage);

        if (logOverflowDamage)
            Debug.Log($"[PlayerAnxiety] Applied queued overflow anxiety damage tick: {damage}", this);
    }

    private void HandleOldHighAnxietyTickDamage()
    {
        if (!damageAtHighAnxiety)
        {
            if (highAnxietyDamageRoutine != null)
            {
                StopCoroutine(highAnxietyDamageRoutine);
                highAnxietyDamageRoutine = null;
            }

            return;
        }

        bool shouldDamage =
            NormalizedAnxiety >= damageThreshold &&
            anxietyDamagePerTick > 0 &&
            anxietyDamageTickInterval > 0f;

        if (shouldDamage)
        {
            if (highAnxietyDamageRoutine == null)
                highAnxietyDamageRoutine = StartCoroutine(HighAnxietyDamageRoutine());
        }
        else
        {
            if (highAnxietyDamageRoutine != null)
            {
                StopCoroutine(highAnxietyDamageRoutine);
                highAnxietyDamageRoutine = null;
            }
        }
    }

    private IEnumerator HighAnxietyDamageRoutine()
    {
        while (damageAtHighAnxiety && NormalizedAnxiety >= damageThreshold)
        {
            DamagePlayerWithAnxietyDamage(anxietyDamagePerTick);
            yield return new WaitForSeconds(anxietyDamageTickInterval);
        }

        highAnxietyDamageRoutine = null;
    }

    private void DamagePlayerWithAnxietyDamage(int damage)
    {
        if (damage <= 0)
            return;

        if (playerHealth == null)
            return;

        // Important:
        // This makes the HP loss register as AnxietyLockedHealthLoss
        // inside PlayerHealth.cs.
        playerHealth.TakeAnxietyDamage(damage, gameObject);
    }

    // ------------------------------------------------------------
    // ANXIETY UI
    // ------------------------------------------------------------

    private void SetupUI()
    {
        if (anxietySlider != null)
        {
            anxietySlider.minValue = 0f;
            anxietySlider.maxValue = maxAnxiety;
            anxietySlider.wholeNumbers = false;
            anxietySlider.interactable = false;

            // We manually control the fill segments.
            // This avoids Unity Slider fighting our split purple/red fill setup.
            anxietySlider.fillRect = null;
        }

        if (normalFillImage != null)
        {
            normalFillRect = normalFillImage.rectTransform;
            normalFillImage.color = normalAnxietyColor;
        }

        if (highFillImage != null)
        {
            highFillRect = highFillImage.rectTransform;
            highFillImage.color = highAnxietyColor;
        }
    }

    private void UpdateUI()
    {
        float anxietyPercent = Mathf.Clamp01(NormalizedAnxiety);
        float threshold = Mathf.Clamp01(highAnxietyThreshold);

        bool hasAnyAnxiety = anxietyPercent > 0f;
        bool highAnxiety = anxietyPercent >= threshold;

        if (anxietySlider != null)
        {
            anxietySlider.minValue = 0f;
            anxietySlider.maxValue = maxAnxiety;
            anxietySlider.value = currentAnxiety;

            // Keep this null so the Slider does not override our custom fill rects.
            anxietySlider.fillRect = null;
        }

        // Normal purple section:
        // 0% to current anxiety when under threshold.
        // 0% to threshold when above threshold.
        if (normalFillImage != null)
        {
            float normalStart = 0f;
            float normalEnd = anxietyPercent;

            if (threshold > 0f)
                normalEnd = Mathf.Min(anxietyPercent, threshold);

            normalFillImage.enabled = hasAnyAnxiety && normalEnd > normalStart;
            normalFillImage.color = normalAnxietyColor;

            SetUIImageFillSegment(
                normalFillImage,
                normalFillRect,
                normalStart,
                normalEnd
            );
        }

        // High red section:
        // threshold to current anxiety.
        if (highFillImage != null)
        {
            float highStart = threshold;
            float highEnd = anxietyPercent;

            bool showHighFill =
                highAnxiety &&
                hasAnyAnxiety &&
                highEnd > highStart;

            highFillImage.enabled = showHighFill;

            if (showHighFill)
            {
                if (flashHighAnxietyFill && !onlyFlashDuringAnxietyDamageFlash)
                {
                    float pulse = Mathf.PingPong(Time.time * flashSpeed, 1f);

                    highFillImage.color = Color.Lerp(
                        highAnxietyColor,
                        highAnxietyFlashColor,
                        pulse
                    );
                }
                else
                {
                    highFillImage.color = highAnxietyColor;
                }

                SetUIImageFillSegment(
                    highFillImage,
                    highFillRect,
                    highStart,
                    highEnd
                );
            }
        }
    }

    private void SetUIImageFillSegment(
        Image image,
        RectTransform rect,
        float startPercent,
        float endPercent
    )
    {
        if (image == null)
            return;

        startPercent = Mathf.Clamp01(startPercent);
        endPercent = Mathf.Clamp01(endPercent);

        if (endPercent < startPercent)
            endPercent = startPercent;

        if (rect == null)
            rect = image.rectTransform;

        if (rect == null)
            return;

        rect.anchorMin = new Vector2(startPercent, 0f);
        rect.anchorMax = new Vector2(endPercent, 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // ------------------------------------------------------------
    // PLAYER HIGH ANXIETY FLASH
    // ------------------------------------------------------------

    private void SetupPlayerFlashRenderers()
    {
        if (playerFlashBlock == null)
            playerFlashBlock = new MaterialPropertyBlock();

        if (playerFlashRenderers == null || playerFlashRenderers.Length == 0)
            playerFlashRenderers = GetComponentsInChildren<Renderer>(true);

        if (playerFlashRenderers == null)
            return;

        originalRendererBaseColors = new Color[playerFlashRenderers.Length];
        originalRendererEmissionColors = new Color[playerFlashRenderers.Length];

        for (int i = 0; i < playerFlashRenderers.Length; i++)
        {
            Renderer rend = playerFlashRenderers[i];

            originalRendererBaseColors[i] = Color.white;
            originalRendererEmissionColors[i] = Color.black;

            if (rend == null || rend.sharedMaterial == null)
                continue;

            if (rend.sharedMaterial.HasProperty("_BaseColor"))
                originalRendererBaseColors[i] = rend.sharedMaterial.GetColor("_BaseColor");
            else if (rend.sharedMaterial.HasProperty("_Color"))
                originalRendererBaseColors[i] = rend.sharedMaterial.GetColor("_Color");

            if (rend.sharedMaterial.HasProperty(playerFlashColorProperty))
                originalRendererEmissionColors[i] = rend.sharedMaterial.GetColor(playerFlashColorProperty);
            else if (rend.sharedMaterial.HasProperty("_EmissionColor"))
                originalRendererEmissionColors[i] = rend.sharedMaterial.GetColor("_EmissionColor");
        }
    }

    private void UpdatePlayerHighAnxietyFlash()
    {
        if (!flashPlayerAtHighAnxiety)
        {
            ResetPlayerFlash();
            return;
        }

        bool highAnxiety = NormalizedAnxiety >= highAnxietyThreshold;

        if (!highAnxiety)
        {
            ResetPlayerFlash();
            return;
        }

        if (playerFlashRenderers == null || playerFlashRenderers.Length == 0)
            return;

        float pulse = Mathf.PingPong(Time.time * flashSpeed, 1f);

        for (int i = 0; i < playerFlashRenderers.Length; i++)
        {
            Renderer rend = playerFlashRenderers[i];

            if (rend == null || rend.sharedMaterial == null)
                continue;

            rend.GetPropertyBlock(playerFlashBlock);

            Color baseColor = i < originalRendererBaseColors.Length
                ? originalRendererBaseColors[i]
                : Color.white;

            Color baseEmission = i < originalRendererEmissionColors.Length
                ? originalRendererEmissionColors[i]
                : Color.black;

            Color flashedBaseColor = Color.Lerp(
                baseColor,
                playerHighAnxietyFlashColor,
                pulse
            );

            Color flashedEmission = Color.Lerp(
                baseEmission,
                playerHighAnxietyFlashColor * playerFlashEmissionBoost,
                pulse
            );

            if (rend.sharedMaterial.HasProperty(playerFlashColorProperty))
                playerFlashBlock.SetColor(playerFlashColorProperty, flashedEmission);

            if (rend.sharedMaterial.HasProperty("_EmissionColor"))
                playerFlashBlock.SetColor("_EmissionColor", flashedEmission);

            if (rend.sharedMaterial.HasProperty("_BaseColor"))
                playerFlashBlock.SetColor("_BaseColor", flashedBaseColor);

            if (rend.sharedMaterial.HasProperty("_Color"))
                playerFlashBlock.SetColor("_Color", flashedBaseColor);

            rend.SetPropertyBlock(playerFlashBlock);
        }
    }

    private void ResetPlayerFlash()
    {
        if (playerFlashRenderers == null || originalRendererBaseColors == null)
            return;

        if (playerFlashBlock == null)
            playerFlashBlock = new MaterialPropertyBlock();

        for (int i = 0; i < playerFlashRenderers.Length; i++)
        {
            Renderer rend = playerFlashRenderers[i];

            if (rend == null || rend.sharedMaterial == null)
                continue;

            rend.GetPropertyBlock(playerFlashBlock);

            Color baseColor = i < originalRendererBaseColors.Length
                ? originalRendererBaseColors[i]
                : Color.white;

            Color emissionColor = i < originalRendererEmissionColors.Length
                ? originalRendererEmissionColors[i]
                : Color.black;

            if (rend.sharedMaterial.HasProperty(playerFlashColorProperty))
                playerFlashBlock.SetColor(playerFlashColorProperty, emissionColor);

            if (rend.sharedMaterial.HasProperty("_EmissionColor"))
                playerFlashBlock.SetColor("_EmissionColor", emissionColor);

            if (rend.sharedMaterial.HasProperty("_BaseColor"))
                playerFlashBlock.SetColor("_BaseColor", baseColor);

            if (rend.sharedMaterial.HasProperty("_Color"))
                playerFlashBlock.SetColor("_Color", baseColor);

            rend.SetPropertyBlock(playerFlashBlock);
        }
    }

    // ------------------------------------------------------------
    // ANIMATOR
    // ------------------------------------------------------------

    private void UpdateAnimatorState()
    {
        if (animator == null)
            return;

        if (string.IsNullOrEmpty(anxietyBoolParam))
            return;

        float normalized = NormalizedAnxiety;

        if (!isInAnxietyAnimation)
        {
            if (normalized >= anxietyAnimationEnterThreshold)
                isInAnxietyAnimation = true;
        }
        else
        {
            float exitThreshold = useAnxietyExitThreshold
                ? anxietyAnimationExitThreshold
                : anxietyAnimationEnterThreshold;

            if (normalized <= exitThreshold)
                isInAnxietyAnimation = false;
        }

        if (HasAnimatorParameter(anxietyBoolParam, AnimatorControllerParameterType.Bool))
            animator.SetBool(anxietyBoolParam, isInAnxietyAnimation);
    }

    private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType type)
    {
        if (animator == null)
            return false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == type)
                return true;
        }

        return false;
    }

    // ------------------------------------------------------------
    // ONE-SHOT ANXIETY VFX
    // ------------------------------------------------------------

    private void SpawnAnxietyVFX()
    {
        if (anxietyVfxPrefab == null)
            return;

        GameObject vfx = CreateAnxietyVFXInstance();

        ParticleSystem[] particleSystems =
            vfx.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem ps in particleSystems)
        {
            if (ps == null)
                continue;

            ps.Clear(true);
            ps.Play(true);
        }

        StartCoroutine(SmoothStopAndDestroyAnxietyVFX(vfx, particleSystems));
    }

    private IEnumerator SmoothStopAndDestroyAnxietyVFX(
        GameObject vfx,
        ParticleSystem[] particleSystems
    )
    {
        if (vfx == null)
            yield break;

        float emitTime = Mathf.Max(0.01f, anxietyVfxEmitTime);

        yield return new WaitForSeconds(emitTime);

        if (vfx == null)
            yield break;

        foreach (ParticleSystem ps in particleSystems)
        {
            if (ps == null)
                continue;

            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        float maxRemainingLifetime = 0.25f;

        foreach (ParticleSystem ps in particleSystems)
        {
            if (ps == null)
                continue;

            ParticleSystem.MainModule main = ps.main;

            maxRemainingLifetime = Mathf.Max(
                maxRemainingLifetime,
                main.startLifetime.constantMax
            );
        }

        yield return new WaitForSeconds(maxRemainingLifetime + anxietyVfxExtraFadeTime);

        if (vfx != null)
            Destroy(vfx);
    }

    // ------------------------------------------------------------
    // CONTINUOUS ANXIETY VFX
    // ------------------------------------------------------------

    private void RefreshContinuousAnxietyVFX()
    {
        if (!useContinuousAnxietyVfxForOverTimeSources)
            return;

        if (anxietyVfxPrefab == null)
            return;

        if (continuousAnxietyStopRoutine != null)
        {
            StopCoroutine(continuousAnxietyStopRoutine);
            continuousAnxietyStopRoutine = null;
        }

        bool createdNew = false;

        if (activeContinuousAnxietyVfx == null)
        {
            activeContinuousAnxietyVfx = CreateAnxietyVFXInstance();

            activeContinuousAnxietyParticles =
                activeContinuousAnxietyVfx.GetComponentsInChildren<ParticleSystem>(true);

            createdNew = true;
        }

        if (activeContinuousAnxietyParticles != null)
        {
            foreach (ParticleSystem ps in activeContinuousAnxietyParticles)
            {
                if (ps == null)
                    continue;

                if (!ps.isPlaying)
                    ps.Play(true);
            }
        }

        if (createdNew && playSfxWhenContinuousVfxStarts)
            PlayAnxietyGainSFX();

        continuousAnxietyStopRoutine =
            StartCoroutine(StopContinuousAnxietyVFXAfterDelay());
    }

    private IEnumerator StopContinuousAnxietyVFXAfterDelay()
    {
        yield return new WaitForSeconds(continuousAnxietyVfxGraceTime);

        if (activeContinuousAnxietyParticles != null)
        {
            foreach (ParticleSystem ps in activeContinuousAnxietyParticles)
            {
                if (ps == null)
                    continue;

                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        yield return new WaitForSeconds(continuousAnxietyVfxFadeTime);

        if (activeContinuousAnxietyVfx != null)
            Destroy(activeContinuousAnxietyVfx);

        activeContinuousAnxietyVfx = null;
        activeContinuousAnxietyParticles = null;
        continuousAnxietyStopRoutine = null;
    }

    private void CleanupContinuousAnxietyVFX()
    {
        if (continuousAnxietyStopRoutine != null)
        {
            StopCoroutine(continuousAnxietyStopRoutine);
            continuousAnxietyStopRoutine = null;
        }

        if (activeContinuousAnxietyVfx != null)
            Destroy(activeContinuousAnxietyVfx);

        activeContinuousAnxietyVfx = null;
        activeContinuousAnxietyParticles = null;
    }

    private GameObject CreateAnxietyVFXInstance()
    {
        Transform spawnPoint = anxietyVfxSpawnPoint != null
            ? anxietyVfxSpawnPoint
            : transform;

        Vector3 spawnPos =
            spawnPoint.position + spawnPoint.TransformDirection(anxietyVfxOffset);

        Quaternion spawnRot =
            spawnPoint.rotation * Quaternion.Euler(anxietyVfxRotationEuler);

        GameObject vfx = Instantiate(anxietyVfxPrefab, spawnPos, spawnRot);

        vfx.transform.localScale = anxietyVfxScale;

        if (parentAnxietyVfxToSpawnPoint)
            vfx.transform.SetParent(spawnPoint, true);

        ForceLayerIfNeeded(vfx, forceAnxietyVfxLayer);

        return vfx;
    }

    private void PlayAnxietyGainSFX()
    {
        if (anxietyGainSfx == null)
            return;

        if (anxietyGainSfxVolume <= 0f)
            return;

        if (anxietyAudioSource != null)
        {
            anxietyAudioSource.PlayOneShot(anxietyGainSfx, anxietyGainSfxVolume);
        }
        else
        {
            AudioSource.PlayClipAtPoint(
                anxietyGainSfx,
                transform.position,
                anxietyGainSfxVolume
            );
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