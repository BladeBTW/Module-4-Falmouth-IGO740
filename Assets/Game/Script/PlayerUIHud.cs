using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerUIHud : MonoBehaviour
{
    [Header("References")]
    public PlayerHealth playerHealth;
    public PlayerAnxiety playerAnxiety;

    [Header("Health UI")]
    public Slider healthSlider;

    [Tooltip("Main health fill.")]
    public Image healthFillImage;

    [Tooltip("Health lost due to anxiety. Base color = red, flashes purple on anxiety damage / unlock.")]
    public Image anxietyLockedFillImage;

    public bool useNormalizedHealth = true;

    [Header("Health Colors")]
    public Color healthFillColor = new Color(0.1f, 1f, 0.2f, 1f);

    [Tooltip("Color used for the normal health fill when anxiety is high.")]
    public Color highAnxietyHealthColor = Color.red;

    [Tooltip("Flash color used for the normal health fill when anxiety is high.")]
    public Color highAnxietyHealthFlashColor = new Color(0.65f, 0f, 1f, 1f);

    [Tooltip("If ON, the normal health fill changes color when anxiety is above threshold.")]
    public bool changeHealthColorAtHighAnxiety = true;

    [Tooltip("If ON, the normal health fill flashes when anxiety is above threshold.")]
    public bool flashHealthAtHighAnxiety = true;

    public float highAnxietyHealthFlashSpeed = 8f;

    [Tooltip("Fallback base color for health locked by anxiety if PlayerHealth does not provide one.")]
    public Color anxietyLockedBaseColor = Color.red;

    [Header("Anxiety Locked Flash")]
    public bool syncLockedHealthFlashWithPlayerHealth = true;
    public Color fallbackAnxietyFlashColor = new Color(0.65f, 0f, 1f, 1f);
    public float fallbackFlashSpeed = 8f;

    [Header("Weight UI")]
    public TMP_Text weightText;
    public Image weightIcon;

    [Header("Weight Color Settings")]
    public float healthyMinWeightKg = 2.2f;
    public float healthyMaxWeightKg = 3.0f;
    public Color healthyColor = Color.white;
    public Color unhealthyColor = Color.red;
    public string weightFormat = "0.00";

    [Header("Debug")]
    public bool autoFindReferences = true;
    public bool logHealthUpdates = false;
    public bool forceSliderFillRect = true;

    private RectTransform healthFillRect;
    private RectTransform anxietyLockedFillRect;

    private int lastLoggedHealth = -9999;
    private int lastLoggedMaxHealth = -9999;

    private void Awake()
    {
        ResolveReferences();
        SetupSlider();
        UpdateAllUI();
    }

    private void OnEnable()
    {
        ResolveReferences();
        SetupSlider();
        UpdateAllUI();
    }

    private void Update()
    {
        if (playerHealth == null || playerAnxiety == null)
            ResolveReferences();

        if (playerHealth == null)
            return;

        UpdateAllUI();
    }

    private void ResolveReferences()
    {
        if (playerHealth == null)
            playerHealth = FindObjectOfType<PlayerHealth>();

        if (playerAnxiety == null && playerHealth != null)
            playerAnxiety = playerHealth.GetComponent<PlayerAnxiety>();

        if (playerAnxiety == null)
            playerAnxiety = FindObjectOfType<PlayerAnxiety>();

        if (autoFindReferences)
            AutoFindHealthUI();

        if (healthFillImage != null)
            healthFillRect = healthFillImage.rectTransform;

        if (anxietyLockedFillImage != null)
            anxietyLockedFillRect = anxietyLockedFillImage.rectTransform;

        if (forceSliderFillRect && healthSlider != null && healthFillRect != null)
            healthSlider.fillRect = healthFillRect;
    }

    private void UpdateAllUI()
    {
        UpdateHealthUI();
        UpdateWeightUI();
    }

    private void SetupSlider()
    {
        if (healthSlider == null || playerHealth == null)
            return;

        if (useNormalizedHealth)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
        }
        else
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = Mathf.Max(1, playerHealth.maxHealth);
        }

        healthSlider.wholeNumbers = false;
        healthSlider.interactable = false;

        if (forceSliderFillRect && healthFillRect != null)
            healthSlider.fillRect = healthFillRect;
    }

    private void UpdateHealthUI()
    {
        if (playerHealth == null)
            return;

        int maxHealth = Mathf.Max(1, playerHealth.maxHealth);
        int currentHealth = Mathf.Clamp(playerHealth.currentHealth, 0, maxHealth);

        float currentPercent = (float)currentHealth / maxHealth;

        float lockedPercent =
            (float)playerHealth.AnxietyLockedHealthLoss / maxHealth;

        currentPercent = Mathf.Clamp01(currentPercent);
        lockedPercent = Mathf.Clamp01(lockedPercent);

        float lockedEndPercent =
            Mathf.Clamp01(currentPercent + lockedPercent);

        UpdateHealthSlider(currentHealth, currentPercent);
        UpdateGreenHealthFill(currentPercent);
        UpdateLockedHealthFill(currentPercent, lockedEndPercent);

        if (logHealthUpdates)
            LogHealthIfChanged(currentHealth, maxHealth, currentPercent);
    }

    private void UpdateHealthSlider(int currentHealth, float currentPercent)
    {
        if (healthSlider == null)
            return;

        if (useNormalizedHealth)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.value = currentPercent;
        }
        else
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = Mathf.Max(1, playerHealth.maxHealth);
            healthSlider.value = currentHealth;
        }
    }

    private void UpdateGreenHealthFill(float currentPercent)
    {
        if (healthFillImage != null)
        {
            healthFillImage.enabled = currentPercent > 0f;
            healthFillImage.color = GetCurrentHealthFillColor();

            if (healthFillImage.type == Image.Type.Filled)
            {
                healthFillImage.fillMethod = Image.FillMethod.Horizontal;
                healthFillImage.fillOrigin = 0;
                healthFillImage.fillAmount = currentPercent;
                return;
            }
        }

        if (healthFillRect == null)
            return;

        healthFillRect.anchorMin = new Vector2(0f, 0f);
        healthFillRect.anchorMax = new Vector2(currentPercent, 1f);
        healthFillRect.offsetMin = Vector2.zero;
        healthFillRect.offsetMax = Vector2.zero;
    }

    private Color GetCurrentHealthFillColor()
    {
        if (!changeHealthColorAtHighAnxiety)
            return healthFillColor;

        if (playerAnxiety == null)
            return healthFillColor;

        bool highAnxiety = playerAnxiety.IsHighAnxiety();

        if (!highAnxiety)
            return healthFillColor;

        if (!flashHealthAtHighAnxiety)
            return highAnxietyHealthColor;

        float pulse = Mathf.PingPong(Time.time * highAnxietyHealthFlashSpeed, 1f);

        return Color.Lerp(
            highAnxietyHealthColor,
            highAnxietyHealthFlashColor,
            pulse
        );
    }

    private void UpdateLockedHealthFill(float currentPercent, float lockedEndPercent)
    {
        if (anxietyLockedFillImage == null || anxietyLockedFillRect == null)
            return;

        bool hasLockedHealth =
            playerHealth.AnxietyLockedHealthLoss > 0 &&
            lockedEndPercent > currentPercent;

        anxietyLockedFillImage.enabled = hasLockedHealth;

        if (!hasLockedHealth)
            return;

        anxietyLockedFillImage.color = GetAnxietyLockedHealthColor();

        if (anxietyLockedFillImage.type == Image.Type.Filled)
        {
            anxietyLockedFillImage.fillMethod = Image.FillMethod.Horizontal;
            anxietyLockedFillImage.fillOrigin = 0;
            anxietyLockedFillImage.fillAmount = lockedEndPercent;
            return;
        }

        anxietyLockedFillRect.anchorMin = new Vector2(currentPercent, 0f);
        anxietyLockedFillRect.anchorMax = new Vector2(lockedEndPercent, 1f);
        anxietyLockedFillRect.offsetMin = Vector2.zero;
        anxietyLockedFillRect.offsetMax = Vector2.zero;
    }

    private Color GetAnxietyLockedHealthColor()
    {
        if (playerHealth == null)
            return anxietyLockedBaseColor;

        if (syncLockedHealthFlashWithPlayerHealth)
            return playerHealth.GetCurrentAnxietyLockedHealthUIColor();

        bool shouldFlash =
            playerHealth.IsAnxietyLockedHealthFlashing ||
            playerHealth.IsAnxietyRegenFlashActive;

        if (shouldFlash)
        {
            float localPhase =
                Mathf.PingPong(Time.time * fallbackFlashSpeed, 1f);

            return Color.Lerp(
                anxietyLockedBaseColor,
                fallbackAnxietyFlashColor,
                localPhase
            );
        }

        return anxietyLockedBaseColor;
    }

    private void UpdateWeightUI()
    {
        if (playerHealth == null)
            return;

        if (weightText == null && weightIcon == null)
            return;

        float weight = playerHealth.currentWeightKg;

        if (weightText != null)
            weightText.text = weight.ToString(weightFormat) + " kg";

        bool isHealthy =
            weight >= healthyMinWeightKg &&
            weight <= healthyMaxWeightKg;

        Color targetColor =
            isHealthy ? healthyColor : unhealthyColor;

        if (weightText != null)
            weightText.color = targetColor;

        if (weightIcon != null)
            weightIcon.color = targetColor;
    }

    private void AutoFindHealthUI()
    {
        if (healthSlider == null)
            healthSlider = GetComponentInChildren<Slider>(true);

        if (healthSlider == null)
            return;

        if (healthFillImage == null)
        {
            Transform fill =
                healthSlider.transform.Find("Fill Area/Fill");

            if (fill != null)
                healthFillImage = fill.GetComponent<Image>();
        }

        if (anxietyLockedFillImage == null)
        {
            Transform lockedFill =
                healthSlider.transform.Find("Fill Area/AnxietyLockedFill");

            if (lockedFill != null)
                anxietyLockedFillImage = lockedFill.GetComponent<Image>();
        }
    }

    private void LogHealthIfChanged(int currentHealth, int maxHealth, float currentPercent)
    {
        if (currentHealth == lastLoggedHealth && maxHealth == lastLoggedMaxHealth)
            return;

        lastLoggedHealth = currentHealth;
        lastLoggedMaxHealth = maxHealth;

        Debug.Log(
            $"[PlayerUIHud] Health UI updated: {currentHealth}/{maxHealth} = {currentPercent:0.00}. " +
            $"Slider: {(healthSlider != null ? healthSlider.name : "NULL")}, " +
            $"Fill: {(healthFillImage != null ? healthFillImage.name : "NULL")}, " +
            $"LockedFill: {(anxietyLockedFillImage != null ? anxietyLockedFillImage.name : "NULL")}, " +
            $"PlayerAnxiety: {(playerAnxiety != null ? playerAnxiety.name : "NULL")}",
            this
        );
    }
}