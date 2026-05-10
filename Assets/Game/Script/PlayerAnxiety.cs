using UnityEngine;
using UnityEngine.UI;

public class PlayerAnxiety : MonoBehaviour
{
    [Header("Anxiety")]
    public float maxAnxiety = 100f;
    public float startingAnxiety = 0f;
    public float currentAnxiety;

    [Header("Natural Recovery")]
    public bool anxietyFallsOverTime = true;
    public float anxietyFallPerSecond = 4f;

    [Header("Overflow Anxiety Damage")]
    [Tooltip("If ON, anxiety gained beyond Max Anxiety becomes health damage.")]
    public bool overflowAnxietyDamagesHealth = true;

    [Tooltip("How much queued health damage each 1 overflow anxiety creates.")]
    public float overflowDamagePerAnxietyPoint = 1f;

    [Tooltip("If ON, fractional overflow damage is saved until it reaches 1 full damage.")]
    public bool accumulateFractionalOverflowDamage = true;

    [Header("Overflow Damage Safety")]
    [Tooltip("Important: ON stops anxiety overflow from damaging every frame.")]
    public bool useOverflowDamageTicks = true;

    [Tooltip("How often overflow anxiety damage is allowed to hit health / play VFX.")]
    public float overflowDamageTickInterval = 1f;

    [Tooltip("Maximum anxiety overflow damage allowed per tick. This prevents HP nuking.")]
    public int maxOverflowDamagePerTick = 1;

    [Tooltip("If ON, queued overflow damage is cleared once anxiety drops below max.")]
    public bool clearQueuedOverflowDamageWhenBelowMax = true;

    [Tooltip("If ON, logs overflow damage queue/ticks.")]
    public bool logOverflowDamage = false;

    [Header("Old High Anxiety Tick Damage")]
    [Tooltip("Usually OFF when using overflow damage. If ON, anxiety also damages over time while above Damage Threshold.")]
    public bool damageAtHighAnxiety = false;

    [Tooltip("Health damage starts when anxiety reaches this percentage.")]
    [Range(0f, 1f)]
    public float damageThreshold = 0.8f;

    public int anxietyDamagePerTick = 1;
    public float anxietyDamageTickInterval = 0.5f;

    [Header("Animator Anxiety State")]
    public Animator animator;
    public string anxietyBoolParam = "Anxiety";

    [Range(0f, 1f)]
    public float anxietyAnimationEnterThreshold = 0.8f;

    public bool useAnxietyExitThreshold = true;

    [Range(0f, 1f)]
    public float anxietyAnimationExitThreshold = 0.65f;

    [Header("UI")]
    public Slider anxietySlider;

    [Tooltip("Normal purple fill image.")]
    public Image normalFillImage;

    [Tooltip("High anxiety fill image.")]
    public Image highFillImage;

    [Header("UI Threshold")]
    [Range(0f, 1f)]
    public float highAnxietyThreshold = 0.8f;

    public Color normalAnxietyColor = new Color(0.55f, 0f, 1f, 1f);
    public Color highAnxietyColor = Color.red;

    [Header("High Anxiety Flash")]
    public bool flashHighAnxietyFill = true;

    [Tooltip("If ON, high anxiety bar uses PlayerHealth's synced anxiety damage flash phase.")]
    public bool syncFlashWithAnxietyDamageFlash = true;

    [Tooltip("If ON, the anxiety bar only flashes while anxiety damage flash is active. Recommended ON.")]
    public bool onlyFlashDuringAnxietyDamageFlash = true;

    [Tooltip("Only used when Sync Flash With Anxiety Damage Flash is OFF.")]
    public Color highAnxietyFlashColor = Color.white;

    [Tooltip("Only used when Sync Flash With Anxiety Damage Flash is OFF.")]
    public float flashSpeed = 6f;

    private PlayerHealth playerHealth;
    private float nextDamageTickTime;

    private RectTransform normalFillRect;
    private RectTransform highFillRect;

    private bool isOverHighThreshold;
    private bool anxietyAnimatorActive;

    private float queuedOverflowDamage;
    private float nextOverflowDamageTickTime;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        currentAnxiety = Mathf.Clamp(startingAnxiety, 0f, maxAnxiety);

        TryAutoFindFillImages();

        if (normalFillImage != null)
            normalFillRect = normalFillImage.rectTransform;

        if (highFillImage != null)
            highFillRect = highFillImage.rectTransform;

        UpdateUI();
        UpdateAnimatorAnxietyState();
    }

    private void Update()
    {
        if (anxietyFallsOverTime && currentAnxiety > 0f)
            ReduceAnxiety(anxietyFallPerSecond * Time.deltaTime);

        if (clearQueuedOverflowDamageWhenBelowMax && currentAnxiety < maxAnxiety)
            queuedOverflowDamage = 0f;

        HandleQueuedOverflowDamage();
        HandleHighAnxietyTickDamage();
        UpdateHighAnxietyFlash();
        UpdateAnimatorAnxietyState();
    }

    public void AddAnxiety(float amount)
    {
        if (amount <= 0f)
            return;

        float desiredAnxiety = currentAnxiety + amount;
        float overflow = Mathf.Max(0f, desiredAnxiety - maxAnxiety);

        currentAnxiety = Mathf.Clamp(
            desiredAnxiety,
            0f,
            maxAnxiety
        );

        if (overflow > 0f)
            QueueOverflowAnxietyDamage(overflow);

        UpdateUI();
        UpdateAnimatorAnxietyState();
    }

    public void AddAnxietyOverTime(float amountPerSecond)
    {
        if (amountPerSecond <= 0f)
            return;

        AddAnxiety(amountPerSecond * Time.deltaTime);
    }

    public void ReduceAnxiety(float amount)
    {
        if (amount <= 0f)
            return;

        currentAnxiety = Mathf.Clamp(
            currentAnxiety - amount,
            0f,
            maxAnxiety
        );

        UpdateUI();
        UpdateAnimatorAnxietyState();
    }

    public void SetAnxiety(float amount)
    {
        currentAnxiety = Mathf.Clamp(
            amount,
            0f,
            maxAnxiety
        );

        if (clearQueuedOverflowDamageWhenBelowMax && currentAnxiety < maxAnxiety)
            queuedOverflowDamage = 0f;

        UpdateUI();
        UpdateAnimatorAnxietyState();
    }

    public float GetAnxietyPercent()
    {
        if (maxAnxiety <= 0f)
            return 0f;

        return currentAnxiety / maxAnxiety;
    }

    public bool IsHighAnxiety()
    {
        return GetAnxietyPercent() >= damageThreshold;
    }

    public bool IsMaxAnxiety()
    {
        return currentAnxiety >= maxAnxiety;
    }

    public bool IsAnimatorAnxietyActive()
    {
        return anxietyAnimatorActive;
    }

    private void QueueOverflowAnxietyDamage(float overflowAmount)
    {
        if (!overflowAnxietyDamagesHealth)
            return;

        float rawDamage = overflowAmount * Mathf.Max(0f, overflowDamagePerAnxietyPoint);

        if (rawDamage <= 0f)
            return;

        if (!useOverflowDamageTicks)
        {
            ApplyOverflowDamageNow(rawDamage);
            return;
        }

        queuedOverflowDamage += rawDamage;

        if (logOverflowDamage)
        {
            Debug.Log(
                $"[PlayerAnxiety] Queued overflow damage: {rawDamage:0.00}. Total queued: {queuedOverflowDamage:0.00}",
                this
            );
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

        if (Time.time < nextOverflowDamageTickTime)
            return;

        nextOverflowDamageTickTime =
            Time.time + Mathf.Max(0.01f, overflowDamageTickInterval);

        int damageToApply;

        if (accumulateFractionalOverflowDamage)
            damageToApply = Mathf.FloorToInt(queuedOverflowDamage);
        else
            damageToApply = Mathf.CeilToInt(queuedOverflowDamage);

        if (damageToApply <= 0)
            return;

        if (maxOverflowDamagePerTick > 0)
            damageToApply = Mathf.Min(damageToApply, maxOverflowDamagePerTick);

        queuedOverflowDamage = Mathf.Max(0f, queuedOverflowDamage - damageToApply);

        ApplyAnxietyDamageToHealth(damageToApply);

        if (logOverflowDamage)
        {
            Debug.Log(
                $"[PlayerAnxiety] Applied overflow tick damage: {damageToApply}. Remaining queued: {queuedOverflowDamage:0.00}",
                this
            );
        }
    }

    private void ApplyOverflowDamageNow(float rawDamage)
    {
        int damageToApply;

        if (accumulateFractionalOverflowDamage)
            damageToApply = Mathf.FloorToInt(rawDamage);
        else
            damageToApply = Mathf.CeilToInt(rawDamage);

        if (damageToApply <= 0)
            return;

        ApplyAnxietyDamageToHealth(damageToApply);
    }

    private void ApplyAnxietyDamageToHealth(int damage)
    {
        if (damage <= 0)
            return;

        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        if (playerHealth == null)
            return;

        playerHealth.TakeAnxietyDamage(
            damage,
            gameObject
        );
    }

    private void HandleHighAnxietyTickDamage()
    {
        if (!damageAtHighAnxiety)
            return;

        if (!IsHighAnxiety())
            return;

        if (Time.time < nextDamageTickTime)
            return;

        nextDamageTickTime = Time.time + Mathf.Max(0.01f, anxietyDamageTickInterval);

        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        if (playerHealth == null)
            return;

        playerHealth.TakeAnxietyDamage(
            anxietyDamagePerTick,
            gameObject
        );
    }

    private void UpdateAnimatorAnxietyState()
    {
        float percent = GetAnxietyPercent();

        bool shouldBeAnxious;

        if (anxietyAnimatorActive && useAnxietyExitThreshold)
            shouldBeAnxious = percent >= anxietyAnimationExitThreshold;
        else
            shouldBeAnxious = percent >= anxietyAnimationEnterThreshold;

        anxietyAnimatorActive = shouldBeAnxious;

        if (animator == null)
            return;

        if (!string.IsNullOrEmpty(anxietyBoolParam))
            animator.SetBool(anxietyBoolParam, shouldBeAnxious);
    }

    private void UpdateUI()
    {
        float percent = GetAnxietyPercent();

        if (anxietySlider != null)
        {
            anxietySlider.maxValue = maxAnxiety;
            anxietySlider.value = currentAnxiety;
        }

        UpdateSplitFill(percent);
    }

    private void UpdateSplitFill(float percent)
    {
        if (normalFillImage != null)
            normalFillImage.color = normalAnxietyColor;

        float normalPercent = Mathf.Min(
            percent,
            highAnxietyThreshold
        );

        float highPercent = Mathf.Max(
            0f,
            percent - highAnxietyThreshold
        );

        isOverHighThreshold = highPercent > 0.001f;

        if (normalFillRect != null)
        {
            normalFillRect.anchorMin = new Vector2(0f, 0f);
            normalFillRect.anchorMax = new Vector2(normalPercent, 1f);
            normalFillRect.offsetMin = Vector2.zero;
            normalFillRect.offsetMax = Vector2.zero;
        }

        if (highFillRect != null)
        {
            highFillRect.anchorMin =
                new Vector2(highAnxietyThreshold, 0f);

            highFillRect.anchorMax =
                new Vector2(highAnxietyThreshold + highPercent, 1f);

            highFillRect.offsetMin = Vector2.zero;
            highFillRect.offsetMax = Vector2.zero;
        }

        if (highFillImage != null)
        {
            highFillImage.enabled = isOverHighThreshold;

            if (!isOverHighThreshold)
                highFillImage.color = highAnxietyColor;
        }
    }

    private void UpdateHighAnxietyFlash()
    {
        if (highFillImage == null)
            return;

        if (!isOverHighThreshold)
            return;

        if (!flashHighAnxietyFill)
        {
            highFillImage.color = highAnxietyColor;
            return;
        }

        if (syncFlashWithAnxietyDamageFlash)
            UpdateHighAnxietyFlashSynced();
        else
            UpdateHighAnxietyFlashLocal();
    }

    private void UpdateHighAnxietyFlashSynced()
    {
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        if (playerHealth == null)
        {
            UpdateHighAnxietyFlashLocal();
            return;
        }

        if (onlyFlashDuringAnxietyDamageFlash &&
            !playerHealth.IsAnxietyDamageFlashActive)
        {
            highFillImage.color = highAnxietyColor;
            return;
        }

        float phase = playerHealth.GetSyncedAnxietyFlashPhase();

        Color boostedFlashColor =
            playerHealth.GetBoostedAnxietyFlashColor();

        highFillImage.color = Color.Lerp(
            highAnxietyColor,
            boostedFlashColor,
            phase
        );
    }

    private void UpdateHighAnxietyFlashLocal()
    {
        float flash = Mathf.PingPong(
            Time.time * flashSpeed,
            1f
        );

        highFillImage.color = Color.Lerp(
            highAnxietyColor,
            highAnxietyFlashColor,
            flash
        );
    }

    private void TryAutoFindFillImages()
    {
        if (anxietySlider == null)
            return;

        if (normalFillImage == null)
        {
            Transform fill =
                anxietySlider.transform.Find("Fill Area/Fill");

            if (fill != null)
                normalFillImage = fill.GetComponent<Image>();
        }

        if (highFillImage == null)
        {
            Transform highFill =
                anxietySlider.transform.Find("Fill Area/HighFill");

            if (highFill != null)
                highFillImage = highFill.GetComponent<Image>();
        }
    }
}