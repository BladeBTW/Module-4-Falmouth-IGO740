using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 200;
    public int startingHealth = 100;
    public int currentHealth;

    [Header("Weight")]
    public float startingWeightKg = 1.9f;
    public float currentWeightKg = 1.9f;

    [Header("Animator")]
    public Animator animator;
    public string healthFloatParam = "Health";
    public string hurtBoolParam = "Hurt";
    public string deathTriggerName = "Die";

    [Header("Hurt Behaviour")]
    public float hurtDuration = 0.3f;
    [Range(0f, 1f)] public float hurtSpeedMultiplier = 0.1f;

    [Header("Hurt VFX / SFX")]
    public GameObject hurtVfxPrefab;
    public Vector3 hurtVfxRotationEuler = Vector3.zero;
    public float hurtVfxLifetime = 3f;
    public AudioClip hurtSfx;
    [Range(0f, 10f)] public float hurtSfxVolume = 1f;

    [Header("Heal VFX / SFX")]
    public GameObject healVfxPrefab;
    public Vector3 healVfxRotationEuler = Vector3.zero;
    public float healVfxLifetime = 3f;
    public AudioClip healSfx;
    [Range(0f, 10f)] public float healSfxVolume = 1f;

    [Header("VFX Spawn")]
    public Transform vfxSpawnPoint;
    public Vector3 vfxSpawnOffset = new Vector3(0f, 0.5f, 0f);

    [Header("Damage Debug")]
    public bool logDamageSource = true;

    [Header("Damage Flash")]
    public Renderer[] targetRenderers;
    public Color flashColor = Color.red;
    public float flashDuration = 0.15f;
    public float flashEmissionBoost = 3f;

    [Header("Death")]
    public float deathGameOverDelay = 2f;

    private bool _isDead;
    private Character _character;
    private float _baseMoveSpeed;
    private bool _hasBaseMoveSpeed;

    private Material[] _materials;
    private Color[] _originalBaseColors;
    private Color[] _originalEmissionColors;
    private bool _hasMaterials;

    private void Awake()
    {
        currentHealth = Mathf.Clamp(startingHealth, 0, maxHealth);
        currentWeightKg = startingWeightKg;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        _character = GetComponent<Character>();
        if (_character != null)
        {
            _baseMoveSpeed = _character.MoveSpeed;
            _hasBaseMoveSpeed = true;
        }

        SetupRenderersForFlash();
        UpdateAnimatorHealth();
    }

    public void TakeDamage(int amount)
    {
        TakeDamage(amount, null);
    }

    public void TakeDamage(int amount, GameObject source)
    {
        if (_isDead || amount <= 0)
            return;

        if (logDamageSource)
        {
            string sourceName = source != null ? source.name : "UNKNOWN";
            string sourcePos = source != null ? source.transform.position.ToString() : "no position";
            Debug.Log($"[DAMAGE] Player took {amount} damage from {sourceName} at {sourcePos}", source);
        }

        currentHealth = Mathf.Max(0, currentHealth - amount);
        UpdateAnimatorHealth();

        FlashDamageColor();
        PlayHurtEffects();

        if (currentHealth <= 0)
            Die();
        else
            TriggerHurt();
    }

    public void Heal(int amount)
    {
        if (_isDead || amount <= 0)
            return;

        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        UpdateAnimatorHealth();
        PlayHealEffects();
    }

    public void AddWeight(float amountKg)
    {
        currentWeightKg += amountKg;
    }

    private void TriggerHurt()
    {
        if (animator != null && !string.IsNullOrEmpty(hurtBoolParam))
            animator.SetBool(hurtBoolParam, true);

        if (_character != null && _hasBaseMoveSpeed)
            _character.MoveSpeed = _baseMoveSpeed * Mathf.Clamp01(hurtSpeedMultiplier);

        CancelInvoke(nameof(EndHurt));
        Invoke(nameof(EndHurt), hurtDuration);
    }

    private void EndHurt()
    {
        if (animator != null && !string.IsNullOrEmpty(hurtBoolParam))
            animator.SetBool(hurtBoolParam, false);

        if (!_isDead && _character != null && _hasBaseMoveSpeed)
            _character.MoveSpeed = _baseMoveSpeed;
    }

    private void PlayHurtEffects()
    {
        SpawnVFX(hurtVfxPrefab, hurtVfxRotationEuler, hurtVfxLifetime);

        if (hurtSfx != null && hurtSfxVolume > 0f)
            AudioSource.PlayClipAtPoint(hurtSfx, transform.position, hurtSfxVolume);
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
        Vector3 pos = origin.position + origin.TransformDirection(vfxSpawnOffset);
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
        if (_isDead)
            return;

        _isDead = true;

        if (DeathCounter.Instance != null)
            DeathCounter.Instance.RegisterDeath();

        CancelInvoke(nameof(EndHurt));

        if (_character != null)
            _character.MoveSpeed = 0f;

        if (animator != null && !string.IsNullOrEmpty(deathTriggerName))
            animator.SetTrigger(deathTriggerName);

        StartCoroutine(DeathGameOverSequence());
    }

    private IEnumerator DeathGameOverSequence()
    {
        float t = 0f;
        while (t < deathGameOverDelay)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (GameUIManager.Instance != null)
            GameUIManager.Instance.ShowGameOver();
    }

    private void UpdateAnimatorHealth()
    {
        if (animator != null && !string.IsNullOrEmpty(healthFloatParam))
            animator.SetFloat(healthFloatParam, currentHealth);
    }

    private void SetupRenderersForFlash()
    {
        Renderer[] renderers = targetRenderers != null && targetRenderers.Length > 0
            ? targetRenderers
            : GetComponentsInChildren<Renderer>();

        _materials = new Material[renderers.Length];
        _originalBaseColors = new Color[renderers.Length];
        _originalEmissionColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];

            if (r == null)
                continue;

            if (r is ParticleSystemRenderer)
                continue;

            Material mat = r.material;
            if (mat == null)
                continue;

            _materials[i] = mat;
            _originalBaseColors[i] = GetBaseColor(mat);
            _originalEmissionColors[i] = GetEmissionColor(mat);
            _hasMaterials = true;
        }
    }

    private void FlashDamageColor()
    {
        if (!_hasMaterials)
            return;

        CancelInvoke(nameof(ResetDamageColor));

        for (int i = 0; i < _materials.Length; i++)
        {
            Material mat = _materials[i];
            if (mat == null)
                continue;

            SetBaseColor(mat, flashColor);
            SetEmissionColor(mat, flashColor * flashEmissionBoost);
        }

        Invoke(nameof(ResetDamageColor), flashDuration);
    }

    private void ResetDamageColor()
    {
        if (!_hasMaterials)
            return;

        for (int i = 0; i < _materials.Length; i++)
        {
            Material mat = _materials[i];
            if (mat == null)
                continue;

            SetBaseColor(mat, _originalBaseColors[i]);
            SetEmissionColor(mat, _originalEmissionColors[i]);
        }
    }

    private static Color GetBaseColor(Material m)
    {
        if (m.HasProperty("_BaseColor")) return m.GetColor("_BaseColor");
        if (m.HasProperty("_Color")) return m.GetColor("_Color");
        return Color.white;
    }

    private static void SetBaseColor(Material m, Color c)
    {
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        else if (m.HasProperty("_Color")) m.SetColor("_Color", c);
    }

    private static Color GetEmissionColor(Material m)
    {
        if (m.HasProperty("_EmissionColor")) return m.GetColor("_EmissionColor");
        if (m.HasProperty("_EmissiveColor")) return m.GetColor("_EmissiveColor");
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