using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerUIHud : MonoBehaviour
{
    [Header("References")]
    [Tooltip("PlayerHealth component of the player (chicken). If left empty, will try to find one in the scene.")]
    public PlayerHealth playerHealth;

    [Header("Health UI")]
    [Tooltip("Slider that represents the player's health.")]
    public Slider healthSlider;

    [Tooltip("If true, slider.value will be 0–1 (normalized). If false, uses raw health 0–maxHealth.")]
    public bool useNormalizedHealth = true;

    [Header("Weight UI")]
    [Tooltip("Text element that shows the player's current weight.")]
    public TMP_Text weightText;

    [Tooltip("Optional icon image that changes color with weight (e.g., a chicken or scale icon).")]
    public Image weightIcon;

    [Header("Weight Color Settings")]
    [Tooltip("Minimum weight considered healthy (inclusive).")]
    public float healthyMinWeightKg = 2.2f;

    [Tooltip("Maximum weight considered healthy (inclusive).")]
    public float healthyMaxWeightKg = 3.0f;

    [Tooltip("Color used when the weight is considered healthy.")]
    public Color healthyColor = Color.white;

    [Tooltip("Color used when the weight is NOT in the healthy range.")]
    public Color unhealthyColor = Color.red;

    [Tooltip("Format string for the weight text, e.g. \"0.00\" -> 2.34 kg, \"0.0\" -> 2.3 kg.")]
    public string weightFormat = "0.00";

    private void Start()
    {
        // Auto-find player if not assigned
        if (playerHealth == null)
        {
            playerHealth = FindObjectOfType<PlayerHealth>();
        }

        // Setup health slider limits
        if (healthSlider != null && playerHealth != null)
        {
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
    }

    private void Update()
    {
        if (playerHealth == null)
            return;

        UpdateHealthUI();
        UpdateWeightUI();
    }

    private void UpdateHealthUI()
    {
        if (healthSlider == null)
            return;

        if (useNormalizedHealth)
        {
            float normHealth = (float)playerHealth.currentHealth / Mathf.Max(1, playerHealth.maxHealth);
            healthSlider.value = normHealth;
        }
        else
        {
            healthSlider.value = playerHealth.currentHealth;
        }
    }

    private void UpdateWeightUI()
    {
        if (weightText == null && weightIcon == null)
            return;

        float weight = playerHealth.currentWeightKg;

        // Update text
        if (weightText != null)
        {
            weightText.text = weight.ToString(weightFormat) + " kg";
        }

        // Decide color based on healthy range
        bool isHealthy =
            weight >= healthyMinWeightKg &&
            weight <= healthyMaxWeightKg;

        Color targetColor = isHealthy ? healthyColor : unhealthyColor;

        // Apply to text
        if (weightText != null)
        {
            weightText.color = targetColor;
        }

        // Apply to icon
        if (weightIcon != null)
        {
            weightIcon.color = targetColor;
        }
    }
}
