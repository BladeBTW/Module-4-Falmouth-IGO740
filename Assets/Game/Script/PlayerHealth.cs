using UnityEngine;
using UnityEngine.VFX;   // for VisualEffect Graph

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth = 100;

    [Header("Weight")]
    public float baseWeight = 0f;      // starting weight
    public float currentWeight = 0f;   // total current weight (base + gains)

    [Header("Water Settings")]
    public int maxWaterHeals = 5;
    public float maxWaterWeightGain = 10f;
    public VisualEffect waterVfxPrefab;
    public AudioClip waterHealSfx;
    public float waterHealVolume = 1f;     // can be <1 or >1

    [Header("Food Settings")]
    public int maxFoodHeals = 5;
    public float maxFoodWeightGain = 10f;
    public VisualEffect foodVfxPrefab;
    public AudioClip foodHealSfx;
    public float foodHealVolume = 1f;      // can be <1 or >1

    [Header("Damage Settings")]
    public AudioClip defaultDamageSfx;
    public float defaultDamageVolume = 1f; // can be <1 or >1

    [Header("Damage Flash")]
    [Tooltip("Leave empty to automatically include ALL renderers on the player.")]
    public Renderer[] targetRenderers = new Renderer[0];
    public Color flashColor = Color.red;
    public float flashDuration = 0.15f;
    public float flashEmissionBoost = 3f;  // how bright the emission flash is

    // Internal tracking
    private int _waterHealsUsed = 0;
    private float _waterWeightGained = 0f;

    private int _foodHealsUsed = 0;
    private float _foodWeightGained = 0f;

    private Renderer[] _renderers;
    private Material[] _materials;
    private Color[] _originalBaseColors;
    private Color[] _originalEmissionColors;
    private bool _hasMaterials = false;

    // Max weight for UI if needed
    public float MaxWeight => baseWeight + maxWaterWeightGain + maxFoodWeightGain;

    private void Awake()
    {
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        currentWeight = baseWeight;

        // Auto-detect all renderers if none assigned
        if (targetRenderers == null || targetRenderers.Length == 0)
            _renderers = GetComponentsInChildren<Renderer>();
        else
            _renderers = targetRenderers;

        if (_renderers != null && _renderers.Length > 0)
        {
            int len = _renderers.Length;
            _materials = new Material[len];
            _originalBaseColors = new Color[len];
            _originalEmissionColors = new Color[len];

            for (int i = 0; i < len; i++)
            {
                if (_renderers[i] == null) continue;

                // unique runtime material instance
                Material mat = _renderers[i].material;
                if (mat != null)
                {
                    _materials[i] = mat;
                    _originalBaseColors[i] = GetBaseColor(mat);
                    _originalEmissionColors[i] = GetEmissionColor(mat);
                    _hasMaterials = true;
                }
            }
        }

        if (!_hasMaterials)
        {
            Debug.LogWarning("PlayerHealth: No materials found for damage flash. " +
                             "Check that your player has Mesh/SkinnedMeshRenderers.", this);
        }
    }

    // ─────────── WATER ───────────
    public bool TryConsumeWater(int healthAmount, float weightAmount, Vector3 vfxPosition)
    {
        if (_waterHealsUsed >= maxWaterHeals)
            return false;

        int healthRoom = maxHealth - currentHealth;
        int healthToApply = Mathf.Clamp(healthAmount, 0, healthRoom);

        float waterWeightRoom = maxWaterWeightGain - _waterWeightGained;
        float weightToApply = Mathf.Clamp(weightAmount, 0f, waterWeightRoom);

        // Nothing to gain
        if (healthToApply <= 0 && weightToApply <= 0f)
            return false;

        if (healthToApply > 0) currentHealth += healthToApply;
        if (weightToApply > 0f)
        {
            currentWeight += weightToApply;
            _waterWeightGained += weightToApply;
        }

        _waterHealsUsed++;

        if (waterVfxPrefab != null)
        {
            var vfx = Instantiate(waterVfxPrefab, vfxPosition, Quaternion.identity);
            Destroy(vfx.gameObject, 3f);
        }

        if (waterHealSfx != null && waterHealVolume != 0f)
        {
            AudioSource.PlayClipAtPoint(waterHealSfx, vfxPosition, waterHealVolume);
        }

        return true;
    }

    // ─────────── FOOD ───────────
    public bool TryConsumeFood(int healthAmount, float weightAmount, Vector3 vfxPosition)
    {
        if (_foodHealsUsed >= maxFoodHeals)
            return false;

        int healthRoom = maxHealth - currentHealth;
        int healthToApply = Mathf.Clamp(healthAmount, 0, healthRoom);

        float foodWeightRoom = maxFoodWeightGain - _foodWeightGained;
        float weightToApply = Mathf.Clamp(weightAmount, 0f, foodWeightRoom);

        if (healthToApply <= 0 && weightToApply <= 0f)
            return false;

        if (healthToApply > 0) currentHealth += healthToApply;
        if (weightToApply > 0f)
        {
            currentWeight += weightToApply;
            _foodWeightGained += weightToApply;
        }

        _foodHealsUsed++;

        if (foodVfxPrefab != null)
        {
            var vfx = Instantiate(foodVfxPrefab, vfxPosition, Quaternion.identity);
            Destroy(vfx.gameObject, 3f);
        }

        if (foodHealSfx != null && foodHealVolume != 0f)
        {
            AudioSource.PlayClipAtPoint(foodHealSfx, vfxPosition, foodHealVolume);
        }

        return true;
    }

    // ─────────── DAMAGE ───────────
    public bool TryTakeDamage(int amount)
    {
        if (amount <= 0) return false;

        int old = currentHealth;
        currentHealth = Mathf.Max(currentHealth - amount, 0);

        if (currentHealth < old)
        {
            FlashDamageColor();
            return true;
        }

        return false;
    }

    // ─────────── FLASH ───────────
    private void FlashDamageColor()
    {
        if (!_hasMaterials || _materials == null)
            return;

        CancelInvoke(nameof(ResetDamageColor));

        for (int i = 0; i < _materials.Length; i++)
        {
            Material mat = _materials[i];
            if (mat == null) continue;

            // Base color flash
            SetBaseColor(mat, flashColor);

            // Emission flash (big, obvious glow)
            Color boosted = flashColor * flashEmissionBoost;
            SetEmissionColor(mat, boosted);
        }

        Invoke(nameof(ResetDamageColor), flashDuration);
    }

    private void ResetDamageColor()
    {
        if (!_hasMaterials || _materials == null ||
            _originalBaseColors == null || _originalEmissionColors == null)
            return;

        for (int i = 0; i < _materials.Length; i++)
        {
            Material mat = _materials[i];
            if (mat == null) continue;

            SetBaseColor(mat, _originalBaseColors[i]);
            SetEmissionColor(mat, _originalEmissionColors[i]);
        }
    }

    // ─────────── Shader helpers ───────────

    private static Color GetBaseColor(Material m)
    {
        if (m == null) return Color.white;

        if (m.HasProperty("_BaseColor"))
            return m.GetColor("_BaseColor");
        if (m.HasProperty("_Color"))
            return m.GetColor("_Color");

        return Color.white;
    }

    private static void SetBaseColor(Material m, Color c)
    {
        if (m == null) return;

        if (m.HasProperty("_BaseColor"))
            m.SetColor("_BaseColor", c);
        else if (m.HasProperty("_Color"))
            m.SetColor("_Color", c);
    }

    private static Color GetEmissionColor(Material m)
    {
        if (m == null) return Color.black;

        if (m.HasProperty("_EmissionColor"))
            return m.GetColor("_EmissionColor");
        if (m.HasProperty("_EmissiveColor"))
            return m.GetColor("_EmissiveColor");

        return Color.black;
    }

    private static void SetEmissionColor(Material m, Color c)
    {
        if (m == null) return;

        // Standard / URP Lit
        if (m.HasProperty("_EmissionColor"))
        {
            m.SetColor("_EmissionColor", c);
            m.EnableKeyword("_EMISSION");
        }
        // HDRP or custom
        else if (m.HasProperty("_EmissiveColor"))
        {
            m.SetColor("_EmissiveColor", c);
            m.EnableKeyword("_EMISSIVE_COLOR");
        }
    }
}
