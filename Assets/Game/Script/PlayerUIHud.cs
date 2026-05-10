using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerUIHud : MonoBehaviour
{
    [Header("References")]
    public PlayerHealth playerHealth;

    [Header("Health UI")]
    public Slider healthSlider;

    [Tooltip("Main green health fill.")]
    public Image healthFillImage;

    [Tooltip("Health lost due to anxiety. Base color = red, flashes purple on anxiety damage / unlock.")]
    public Image anxietyLockedFillImage;

    public bool useNormalizedHealth = true;

    [Header("Health Colors")]
    public Color healthFillColor = new Color(0.1f, 1f, 0.2f, 1f);

    [Tooltip("Fallback base color for health locked by anxiety if PlayerHealth does not provide one.")]
    public Color anxietyLockedBaseColor = Color.red;

    [Header("Anxiety Locked Flash")]
    [Tooltip("If ON, this UI uses PlayerHealth colors/timing for anxiety locked health flashing.")]
    public bool syncLockedHealthFlashWithPlayerHealth = true;

    [Tooltip("Fallback flash color if Sync Locked Health Flash With Player Health is OFF.")]
    public Color fallbackAnxietyFlashColor = new Color(0.65f, 0f, 1f, 1f);

    [Tooltip("Fallback flash speed if Sync Locked Health Flash With Player Health is OFF.")]
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

    private RectTransform healthFillRect;
    private RectTransform anxietyLockedFillRect;

    private void Awake()
    {
        if (playerHealth == null)
            playerHealth = FindObjectOfType<PlayerHealth>();

        AutoFindHealthUI();

        if (healthFillImage != null)
            healthFillRect = healthFillImage.rectTransform;

        if (anxietyLockedFillImage != null)
            anxietyLockedFillRect = anxietyLockedFillImage.rectTransform;

        SetupSlider();
        UpdateAllUI();
    }

    private void Update()
    {
        if (playerHealth == null)
            return;

        UpdateAllUI();
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
            healthSlider.maxValue = playerHealth.maxHealth;
        }
    }

    private void UpdateHealthUI()
    {
        float currentPercent =
            (float)playerHealth.currentHealth /
            Mathf.Max(1, playerHealth.maxHealth);

        float lockedPercent =
            (float)playerHealth.AnxietyLockedHealthLoss /
            Mathf.Max(1, playerHealth.maxHealth);

        currentPercent = Mathf.Clamp01(currentPercent);
        lockedPercent = Mathf.Clamp01(lockedPercent);

        float lockedEndPercent =
            Mathf.Clamp01(currentPercent + lockedPercent);

        UpdateHealthSlider(currentPercent);
        UpdateGreenHealthFill(currentPercent);
        UpdateLockedHealthFill(currentPercent, lockedEndPercent);
    }

    private void UpdateHealthSlider(float currentPercent)
    {
        if (healthSlider == null)
            return;

        if (useNormalizedHealth)
            healthSlider.value = currentPercent;
        else
            healthSlider.value = playerHealth.currentHealth;
    }

    private void UpdateGreenHealthFill(float currentPercent)
    {
        if (healthFillImage != null)
            healthFillImage.color = healthFillColor;

        if (healthFillRect == null)
            return;

        healthFillRect.anchorMin = new Vector2(0f, 0f);
        healthFillRect.anchorMax = new Vector2(currentPercent, 1f);
        healthFillRect.offsetMin = Vector2.zero;
        healthFillRect.offsetMax = Vector2.zero;
    }

    private void UpdateLockedHealthFill(float currentPercent, float lockedEndPercent)
    {
        if (anxietyLockedFillImage == null ||
            anxietyLockedFillRect == null)
            return;

        bool hasLockedHealth =
            playerHealth.AnxietyLockedHealthLoss > 0 &&
            lockedEndPercent > currentPercent;

        anxietyLockedFillImage.enabled = hasLockedHealth;

        if (!hasLockedHealth)
            return;

        anxietyLockedFillRect.anchorMin =
            new Vector2(currentPercent, 0f);

        anxietyLockedFillRect.anchorMax =
            new Vector2(lockedEndPercent, 1f);

        anxietyLockedFillRect.offsetMin = Vector2.zero;
        anxietyLockedFillRect.offsetMax = Vector2.zero;

        anxietyLockedFillImage.color =
            GetAnxietyLockedHealthColor();
    }

    private Color GetAnxietyLockedHealthColor()
    {
        if (playerHealth == null)
            return anxietyLockedBaseColor;

        if (syncLockedHealthFlashWithPlayerHealth)
        {
            return playerHealth.GetCurrentAnxietyLockedHealthUIColor();
        }

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
}