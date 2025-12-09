using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 2000;
    public int startingHealth = 1000;
    public int currentHealth;

    [Header("Weight")]
    [Tooltip("Starting weight of the chicken in kg.")]
    public float startingWeightKg = 0f;

    [Tooltip("Current weight of the chicken in kg (read by worker).")]
    public float currentWeightKg = 0f;

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

    [Header("Hurt VFX / SFX")]
    [Tooltip("Prefab to spawn when the player gets hurt (ParticleSystem or VFX Graph prefab).")]
    public GameObject hurtVfxPrefab;

    [Tooltip("How long to keep the hurt VFX alive (seconds). Set 0 or negative to never auto-destroy.")]
    public float hurtVfxLifetime = 3f;

    [Tooltip("Sound to play when the player gets hurt.")]
    public AudioClip hurtSfx;

    [Tooltip("1 = normal, 2 = loud, 5 = very loud, 10 = extreme.")]
    [Range(0f, 10f)]
    public float hurtSfxVolume = 1f;

    [Header("Damage Flash")]
    [Tooltip("If empty, all child renderers will be used.")]
    public Renderer[] targetRenderers;

    [Tooltip("Color to flash when damaged.")]
    public Color flashColor = Color.red;

    [Tooltip("How long the flash lasts.")]
    public float flashDuration = 0.15f;

    [Tooltip("How strong to boost emission when flashing.")]
    public float flashEmissionBoost = 3f;

    [Header("Death")]
    [Tooltip("Delay (in seconds, real time) before showing Game Over after death animation starts.")]
    public float deathGameOverDelay = 2f;

    private bool _isDead = false;
    private bool _hurtActive = false;

    // Movement script (to slow/stop movement)
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

        Debug.Log($"[PlayerHealth] Awake. StartingHealth={currentHealth}, MaxHealth={maxHealth}, Weight={currentWeightKg}kg");
    }

    // ───────── PUBLIC API ─────────

    public void TakeDamage(int amount)
    {
        if (_isDead) return;
        if (amount <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        UpdateAnimatorHealth();

        Debug.Log($"[PlayerHealth] Took damage: {amount}, new health = {currentHealth}");

        // Visual flash
        FlashDamageColor();

        if (currentHealth <= 0)
        {
            Debug.Log("[PlayerHealth] Health reached 0, calling Die().");
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

        Debug.Log($"[PlayerHealth] Healed by {amount}, new health = {currentHealth}");
    }

    public void AddWeight(float amountKg)
    {
        currentWeightKg += amountKg;
        Debug.Log($"[PlayerHealth] Weight increased by {amountKg}kg, now = {currentWeightKg}kg");
    }

    // ───────── HURT LOGIC ─────────

    private void TriggerHurt()
    {
        _hurtActive = true;
        Debug.Log("[PlayerHealth] TriggerHurt()");

        if (animator != null && !string.IsNullOrEmpty(hurtBoolParam))
        {
            animator.SetBool(hurtBoolParam, true);
        }
        else
        {
            Debug.LogWarning("[PlayerHealth] No animator or hurtBoolParam not set.");
        }

        ApplyHurtMovementSlow();
        PlayHurtEffects();

        CancelInvoke(nameof(EndHurt));
        Invoke(nameof(EndHurt), hurtDuration);
    }

    private void EndHurt()
    {
        _hurtActive = false;
        Debug.Log("[PlayerHealth] EndHurt()");

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
        Debug.Log($"[PlayerHealth] Movement slowed for hurt. New MoveSpeed={_character.MoveSpeed}");
    }

    private void RestoreNormalMovementSpeed()
    {
        if (_character == null || !_hasBaseMoveSpeed)
            return;

        _character.MoveSpeed = _baseMoveSpeed;
        Debug.Log($"[PlayerHealth] Movement restored. MoveSpeed={_character.MoveSpeed}");
    }

    private void PlayHurtEffects()
    {
        // VFX at player position
        if (hurtVfxPrefab != null)
        {
            GameObject vfx = Instantiate(hurtVfxPrefab, transform.position, Quaternion.identity);
            if (hurtVfxLifetime > 0f)
            {
                Destroy(vfx, hurtVfxLifetime);
            }
        }

        // SFX at player position
        if (hurtSfx != null && hurtSfxVolume > 0f)
        {
            AudioSource.PlayClipAtPoint(hurtSfx, transform.position, hurtSfxVolume);
        }
    }

    // ───────── DEATH LOGIC ─────────

    private void Die()
    {
        if (_isDead)
        {
            Debug.Log("[PlayerHealth] Die() called but already dead.");
            return;
        }

        _isDead = true;
        Debug.Log("[PlayerHealth] Die() started.");

        // Count this as a death for the playthrough
        if (DeathCounter.Instance != null)
        {
            DeathCounter.Instance.RegisterDeath();
            Debug.Log($"[PlayerHealth] DeathCounter incremented. Total = {DeathCounter.Instance.TotalDeaths}");
        }
        else
        {
            Debug.LogWarning("[PlayerHealth] No DeathCounter in scene.");
        }

        // Stop any hurt state
        CancelInvoke(nameof(EndHurt));
        _hurtActive = false;

        // Stop movement
        if (_character != null)
        {
            _character.MoveSpeed = 0f;
            Debug.Log("[PlayerHealth] Character movement stopped.");
        }

        // Trigger death animation
        if (animator != null && !string.IsNullOrEmpty(deathTriggerName))
        {
            Debug.Log($"[PlayerHealth] Triggering death animation '{deathTriggerName}'.");
            animator.SetTrigger(deathTriggerName);
        }
        else
        {
            Debug.LogWarning("[PlayerHealth] No animator or deathTriggerName not set.");
        }

        // Start sequence to show Game Over after a delay
        Debug.Log("[PlayerHealth] Starting DeathGameOverSequence coroutine.");
        StartCoroutine(DeathGameOverSequence());
    }

    private IEnumerator DeathGameOverSequence()
    {
        float delay = Mathf.Max(0f, deathGameOverDelay);
        Debug.Log($"[PlayerHealth] DeathGameOverSequence started. Waiting {delay} seconds (unscaled).");

        float t = 0f;
        while (t < delay)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        Debug.Log("[PlayerHealth] Death delay elapsed, trying to show Game Over.");

        if (GameUIManager.Instance != null)
        {
            Debug.Log("[PlayerHealth] GameUIManager.Instance found, calling ShowGameOver().");
            GameUIManager.Instance.ShowGameOver();
        }
        else
        {
            Debug.LogError("[PlayerHealth] No GameUIManager.Instance found! Cannot show Game Over.");
            // No freeze fallback here; just log the error.
        }
    }

    // ───────── ANIMATOR HELPERS ─────────

    private void UpdateAnimatorHealth()
    {
        if (animator != null && !string.IsNullOrEmpty(healthFloatParam))
        {
            animator.SetFloat(healthFloatParam, currentHealth);
        }
    }

    // ───────── FLASH SETUP & LOGIC ─────────

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

            // Use instance material (not sharedMaterial) so we don't affect prefabs
            Material mat = r.material;
            if (mat == null) continue;

            _materials[i] = mat;
            _originalBaseColors[i] = GetBaseColor(mat);
            _originalEmissionColors[i] = GetEmissionColor(mat);
            _hasMaterials = true;
        }

        Debug.Log($"[PlayerHealth] SetupRenderersForFlash. Materials found: {_materials.Length}, hasMaterials={_hasMaterials}");
    }

    private void FlashDamageColor()
    {
        if (!_hasMaterials || _materials == null)
        {
            Debug.LogWarning("[PlayerHealth] FlashDamageColor called but no materials cached.");
            return;
        }

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

    // ───────── SHADER HELPERS ─────────

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
