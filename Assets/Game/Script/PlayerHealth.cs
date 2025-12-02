using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 2000;
    public int startingHealth = 1000;
    public int currentHealth;

    [Header("Weight")]
    public float startingWeightKg = 0f;
    public float currentWeightKg = 0f;   // worker will read this

    [Header("Animator")]
    [Tooltip("Animator on the player (chicken).")]
    public Animator animator;

    [Tooltip("Float parameter on the Animator that reflects current health (e.g. 'Health').")]
    public string healthFloatParam = "Health";

    [Tooltip("Bool parameter on the Animator that enters the Hurt state (e.g. 'Hurt').")]
    public string hurtBoolParam = "Hurt";

    [Tooltip("Trigger name for death state in the Animator (e.g. 'Die').")]
    public string deathTriggerName = "Die";

    [Header("Hurt Behaviour")]
    [Tooltip("How long the Hurt state stays active after taking damage (seconds).")]
    public float hurtDuration = 0.3f;

    [Tooltip("Multiplier applied to MoveSpeed while hurt. 0.1 = 90% slower, 1 = no slow.")]
    [Range(0f, 1f)]
    public float hurtSpeedMultiplier = 0.1f;

    [Header("Damage Flash")]
    [Tooltip("If empty, all child renderers will be used.")]
    public Renderer[] targetRenderers;
    public Color flashColor = Color.red;
    public float flashDuration = 0.15f;
    public float flashEmissionBoost = 3f;

    private bool _isDead = false;
    private bool _hurtActive = false;

    // Movement script (optional, to slow/stop movement)
    private Character _character;
    private float _baseMoveSpeed;
    private bool _hasBaseMoveSpeed = false;

    // Flash internals
    private Renderer[] _renderers;
    private Material[] _materials;
    private Color[] _originalBaseColors;
    private Color[] _originalEmissionColors;
    private bool _hasMaterials = false;

    private void Awake()
    {
        currentHealth = Mathf.Clamp(startingHealth, 0, maxHealth);
        currentWeightKg = startingWeightKg;

        _character = GetComponent<Character>();
        if (_character != null)
        {
            _baseMoveSpeed = _character.MoveSpeed;
            _hasBaseMoveSpeed = true;
        }

        SetupRenderersForFlash();

        UpdateAnimatorHealth();
    }

    // ------------ Public API ------------

    public void TakeDamage(int amount)
    {
        if (_isDead) return;
        if (amount <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        UpdateAnimatorHealth();

        // Flash on damage
        FlashDamageColor();

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
        if (_isDead) return;
        if (amount <= 0) return;

        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        UpdateAnimatorHealth();
    }

    public void AddWeight(float amountKg)
    {
        currentWeightKg += amountKg;
    }

    // ------------ Hurt logic ------------

    private void TriggerHurt()
    {
        _hurtActive = true;

        if (animator != null && !string.IsNullOrEmpty(hurtBoolParam))
        {
            animator.SetBool(hurtBoolParam, true);
        }

        ApplyHurtMovementSlow();

        CancelInvoke(nameof(EndHurt));
        Invoke(nameof(EndHurt), hurtDuration);
    }

    private void EndHurt()
    {
        _hurtActive = false;

        if (animator != null && !string.IsNullOrEmpty(hurtBoolParam))
        {
            animator.SetBool(hurtBoolParam, false);
        }

        if (!_isDead)
        {
            RestoreNormalMovementSpeed();
        }
    }

    private void ApplyHurtMovementSlow()
    {
        if (_character == null || !_hasBaseMoveSpeed)
            return;

        float clamped = Mathf.Clamp01(hurtSpeedMultiplier);
        _character.MoveSpeed = _baseMoveSpeed * clamped;
    }

    private void RestoreNormalMovementSpeed()
    {
        if (_character == null || !_hasBaseMoveSpeed)
            return;

        _character.MoveSpeed = _baseMoveSpeed;
    }

    // ------------ Death logic ------------

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;

        // Stop any hurt state
        CancelInvoke(nameof(EndHurt));
        _hurtActive = false;

        // Stop movement
        if (_character != null)
        {
            _character.MoveSpeed = 0f;
        }

        // Play death animation
        if (animator != null && !string.IsNullOrEmpty(deathTriggerName))
        {
            animator.SetTrigger(deathTriggerName);
        }

        // Show simple death image
        if (SimpleGameOverUI.Instance != null)
        {
            SimpleGameOverUI.Instance.ShowEnd(SimpleEndType.Death);
        }
        else
        {
            Time.timeScale = 0f;
        }
    }

    // ------------ Animator helpers ------------

    private void UpdateAnimatorHealth()
    {
        if (animator != null && !string.IsNullOrEmpty(healthFloatParam))
        {
            // Using raw health value; if you want 0–1, change to (float)currentHealth / maxHealth
            animator.SetFloat(healthFloatParam, currentHealth);
        }
    }

    // ------------ Flash setup & logic ------------

    private void SetupRenderersForFlash()
    {
        if (targetRenderers != null && targetRenderers.Length > 0)
        {
            _renderers = targetRenderers;
        }
        else
        {
            _renderers = GetComponentsInChildren<Renderer>();
        }

        if (_renderers == null || _renderers.Length == 0)
            return;

        int len = _renderers.Length;
        _materials = new Material[len];
        _originalBaseColors = new Color[len];
        _originalEmissionColors = new Color[len];

        for (int i = 0; i < len; i++)
        {
            var r = _renderers[i];
            if (r == null) continue;

            // material (not sharedMaterial) so we don't affect prefabs
            Material mat = r.material;
            if (mat == null) continue;

            _materials[i] = mat;
            _originalBaseColors[i] = GetBaseColor(mat);
            _originalEmissionColors[i] = GetEmissionColor(mat);
            _hasMaterials = true;
        }
    }

    private void FlashDamageColor()
    {
        if (!_hasMaterials || _materials == null)
            return;

        CancelInvoke(nameof(ResetDamageColor));

        for (int i = 0; i < _materials.Length; i++)
        {
            Material mat = _materials[i];
            if (mat == null) continue;

            SetBaseColor(mat, flashColor);

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

    // ------------ Shader helpers ------------

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
