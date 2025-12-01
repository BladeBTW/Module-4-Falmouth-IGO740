using UnityEngine;
using UnityEngine.VFX;   // for VisualEffect

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth = 100;

    [Header("Weight")]
    public float baseWeight = 0f;          // starting weight (optional)
    public float currentWeight = 0f;       // total current weight (base + gains)

    [Header("Water Settings")]
    public int maxWaterHeals = 5;          // max times water can heal per run
    public float maxWaterWeightGain = 10f; // max total weight water can add per run
    public VisualEffect waterVfxPrefab;    // VFX Splash or similar

    [Header("Food Settings")]
    public int maxFoodHeals = 5;           // max times food can heal per run
    public float maxFoodWeightGain = 10f;  // max total weight food can add per run
    public VisualEffect foodVfxPrefab;     // second VFX for food

    // Internal tracking
    private int _waterHealsUsed = 0;
    private float _waterWeightGained = 0f;

    private int _foodHealsUsed = 0;
    private float _foodWeightGained = 0f;

    private void Awake()
    {
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        currentWeight = baseWeight;
    }

    // ------------ PUBLIC API ------------

    /// <summary>
    /// Consume WATER: heal + weight gain, limited by water caps.
    /// Returns true if something was actually applied.
    /// </summary>
    public bool TryConsumeWater(int healthAmount, float weightAmount, Vector3 vfxPosition)
    {
        // Per-run heal count limit
        if (_waterHealsUsed >= maxWaterHeals)
            return false;

        // How much health can we still gain at all?
        int healthRoom = maxHealth - currentHealth;
        int healthToApply = Mathf.Clamp(healthAmount, 0, healthRoom);

        // How much water-weight can we still gain this run?
        float waterWeightRoom = maxWaterWeightGain - _waterWeightGained;
        float weightToApply = Mathf.Clamp(weightAmount, 0f, waterWeightRoom);

        // If nothing can be applied, do nothing
        if (healthToApply <= 0 && weightToApply <= 0f)
            return false;

        // Apply health
        if (healthToApply > 0)
        {
            currentHealth += healthToApply;
        }

        // Apply weight
        if (weightToApply > 0f)
        {
            _waterWeightGained += weightToApply;
            currentWeight += weightToApply;
        }

        _waterHealsUsed++;

        // Play VFX
        if (waterVfxPrefab != null)
        {
            VisualEffect vfx = Instantiate(waterVfxPrefab, vfxPosition, Quaternion.identity);
            Destroy(vfx.gameObject, 3f);   // adjust lifetime as needed
        }

        return true;
    }

    /// <summary>
    /// Consume FOOD: heal + weight gain, limited by food caps.
    /// Returns true if something was actually applied.
    /// </summary>
    public bool TryConsumeFood(int healthAmount, float weightAmount, Vector3 vfxPosition)
    {
        // Per-run heal count limit
        if (_foodHealsUsed >= maxFoodHeals)
            return false;

        // How much health can we still gain at all?
        int healthRoom = maxHealth - currentHealth;
        int healthToApply = Mathf.Clamp(healthAmount, 0, healthRoom);

        // How much food-weight can we still gain this run?
        float foodWeightRoom = maxFoodWeightGain - _foodWeightGained;
        float weightToApply = Mathf.Clamp(weightAmount, 0f, foodWeightRoom);

        // If nothing can be applied, do nothing
        if (healthToApply <= 0 && weightToApply <= 0f)
            return false;

        // Apply health
        if (healthToApply > 0)
        {
            currentHealth += healthToApply;
        }

        // Apply weight
        if (weightToApply > 0f)
        {
            _foodWeightGained += weightToApply;
            currentWeight += weightToApply;
        }

        _foodHealsUsed++;

        // Play VFX
        if (foodVfxPrefab != null)
        {
            VisualEffect vfx = Instantiate(foodVfxPrefab, vfxPosition, Quaternion.identity);
            Destroy(vfx.gameObject, 3f);   // adjust lifetime as needed
        }

        return true;
    }
    public bool TryTakeDamage(int amount)
{
    if (amount <= 0)
        return false;

    int oldHealth = currentHealth;
    currentHealth = Mathf.Max(currentHealth - amount, 0);

    // did damage actually change anything?
    return currentHealth < oldHealth;
}

}
