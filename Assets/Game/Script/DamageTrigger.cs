using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DamageTrigger : MonoBehaviour
{
    public enum TriggerEffectMode
    {
        Damage,
        SlowTerrain,
        DamageAndSlowTerrain
    }

    [Header("Mode")]
    public TriggerEffectMode effectMode = TriggerEffectMode.Damage;

    [Header("Damage")]
    public int damageAmount = 5;
    public bool damageOnlyOnce = false;

    [Header("Slow Terrain")]
    [Tooltip("1 = normal speed, 0.5 = half speed, 0 = fully stopped.")]
    [Range(0f, 1f)]
    public float playerSlowMoveSpeedMultiplier = 0.5f;

    [Tooltip("Separate slow multiplier for NPCs/enemies.")]
    [Range(0f, 1f)]
    public float npcSlowMoveSpeedMultiplier = 0.5f;

    [Tooltip("If true, target is slowed while inside this trigger. If false, slow lasts for Slow Duration.")]
    public bool slowWhileInside = true;

    [Tooltip("Only used if Slow While Inside is false.")]
    public float slowDuration = 1f;

    [Header("Targets")]
    public bool affectPlayer = true;
    public bool affectNPCs = true;

    public string playerTag = "Player";

    [Tooltip("NPCs with any of these tags can be slowed. Leave empty if you only want to rely on SlowableMovement being present.")]
    public string[] npcTags = new string[]
    {
        "Enemy",
        "NPC"
    };

    [Header("Debug")]
    public bool logDamage = true;
    public bool logSlow = true;

    private bool hasDamaged;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void Awake()
    {
        Collider col = GetComponent<Collider>();

        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        ApplyEffects(other, true);
    }

    private void OnTriggerStay(Collider other)
    {
        if (ShouldSlow() && slowWhileInside)
            TryStartSlow(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!slowWhileInside)
            return;

        SlowableMovement slowable = GetSlowable(other);

        if (slowable == null)
            return;

        slowable.RemoveSlow(this);

        if (logSlow)
        {
            Debug.Log(
                $"[DamageTrigger] {gameObject.name} removed slow from {other.name}.",
                this
            );
        }
    }

    private void ApplyEffects(Collider other, bool allowDamage)
    {
        if (!IsValidTarget(other))
            return;

        if (allowDamage && ShouldDamage())
            TryDamage(other);

        if (ShouldSlow())
            TryStartSlow(other);
    }

    private bool ShouldDamage()
    {
        return effectMode == TriggerEffectMode.Damage ||
               effectMode == TriggerEffectMode.DamageAndSlowTerrain;
    }

    private bool ShouldSlow()
    {
        return effectMode == TriggerEffectMode.SlowTerrain ||
               effectMode == TriggerEffectMode.DamageAndSlowTerrain;
    }

    private bool IsValidTarget(Collider other)
    {
        return IsPlayerTarget(other) || IsNPCTarget(other);
    }

    private bool IsPlayerTarget(Collider other)
    {
        if (!affectPlayer || other == null)
            return false;

        return other.CompareTag(playerTag) ||
               other.GetComponentInParent<Character>() != null;
    }

    private bool IsNPCTarget(Collider other)
    {
        if (!affectNPCs || other == null)
            return false;

        if (other.GetComponentInParent<SlowableMovement>() != null && !IsPlayerTarget(other))
            return true;

        for (int i = 0; i < npcTags.Length; i++)
        {
            if (!string.IsNullOrEmpty(npcTags[i]) && other.CompareTag(npcTags[i]))
                return true;
        }

        return false;
    }

    private float GetSlowMultiplierForTarget(Collider other)
    {
        if (IsPlayerTarget(other))
            return playerSlowMoveSpeedMultiplier;

        return npcSlowMoveSpeedMultiplier;
    }

    private void TryDamage(Collider other)
    {
        if (hasDamaged && damageOnlyOnce)
            return;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();

        if (playerHealth == null)
            playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null)
            return;

        if (logDamage)
        {
            Debug.Log(
                $"[DamageTrigger] {gameObject.name} damaged {other.name} for {damageAmount}.",
                this
            );
        }

        playerHealth.TakeDamage(damageAmount, gameObject);

        hasDamaged = true;
    }

    private void TryStartSlow(Collider other)
    {
        if (!IsValidTarget(other))
            return;

        SlowableMovement slowable = GetSlowable(other);

        if (slowable == null)
        {
            if (logSlow)
            {
                Debug.LogWarning(
                    $"[DamageTrigger] {other.name} entered slow terrain, but has no SlowableMovement component. Add SlowableMovement to the Player/NPC root.",
                    other
                );
            }

            return;
        }

        float multiplier = GetSlowMultiplierForTarget(other);
        slowable.AddSlow(this, multiplier);

        if (logSlow)
        {
            string targetType = IsPlayerTarget(other) ? "Player" : "NPC";

            Debug.Log(
                $"[DamageTrigger] {gameObject.name} slowed {slowable.name} as {targetType}. Multiplier: {multiplier}",
                this
            );
        }

        if (!slowWhileInside)
            StartCoroutine(RemoveSlowAfterDelay(slowable, slowDuration));
    }

    private System.Collections.IEnumerator RemoveSlowAfterDelay(
        SlowableMovement slowable,
        float delay
    )
    {
        yield return new WaitForSeconds(Mathf.Max(0.01f, delay));

        if (slowable != null)
            slowable.RemoveSlow(this);
    }

    private SlowableMovement GetSlowable(Collider other)
    {
        if (other == null)
            return null;

        SlowableMovement slowable = other.GetComponent<SlowableMovement>();

        if (slowable == null)
            slowable = other.GetComponentInParent<SlowableMovement>();

        return slowable;
    }
}