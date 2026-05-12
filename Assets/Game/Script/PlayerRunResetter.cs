using System.Reflection;
using UnityEngine;

public class PlayerRunResetter : MonoBehaviour
{
    [Header("References")]
    public PlayerHealth playerHealth;
    public PlayerAnxiety playerAnxiety;

    [Header("Reset On Spawn")]
    [Tooltip("If ON, the player resets whenever it spawns in GameScene, even if RunReset.StartNewRun was not called.")]
    public bool resetWhenPlayerSpawns = true;

    [Tooltip("If ON, this calls RunReset.StartNewRun automatically when the player spawns and no new run was already started.")]
    public bool createRunOnSpawnIfMissing = true;

    [Header("Reset Health")]
    public bool resetHealth = true;
    public bool resetWeight = true;
    public bool resetAnxietyLockedHealth = true;

    [Header("Reset Anxiety")]
    public bool resetAnxiety = true;
    public bool resetOverflowDamage = true;
    public bool stopAnxietyVfx = true;

    [Header("Debug")]
    public bool logReset = true;

    private int lastAppliedRunNumber = -1;
    private bool didSpawnReset;

    private void Awake()
    {
        ResolveReferences();

        if (resetWhenPlayerSpawns)
        {
            if (createRunOnSpawnIfMissing && RunReset.RunNumber <= 0)
                RunReset.StartNewRun();

            ApplyFullReset("Player spawned");
            didSpawnReset = true;
        }
    }

    private void OnEnable()
    {
        RunReset.OnNewRunStarted += OnNewRunStarted;

        if (!didSpawnReset)
            ApplyRunResetIfNeeded();
    }

    private void OnDisable()
    {
        RunReset.OnNewRunStarted -= OnNewRunStarted;
    }

    private void OnNewRunStarted()
    {
        ApplyRunResetIfNeeded();
    }

    private void ResolveReferences()
    {
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        if (playerHealth == null)
            playerHealth = GetComponentInChildren<PlayerHealth>(true);

        if (playerAnxiety == null)
            playerAnxiety = GetComponent<PlayerAnxiety>();

        if (playerAnxiety == null)
            playerAnxiety = GetComponentInChildren<PlayerAnxiety>(true);
    }

    private void ApplyRunResetIfNeeded()
    {
        if (lastAppliedRunNumber == RunReset.RunNumber)
            return;

        ApplyFullReset("RunReset event");
    }

    private void ApplyFullReset(string reason)
    {
        lastAppliedRunNumber = RunReset.RunNumber;

        ResolveReferences();

        ResetPlayerHealth();
        ResetPlayerAnxiety();

        if (logReset)
        {
            Debug.Log(
                $"[PlayerRunResetter] Applied reset. Reason: {reason}. Run #{RunReset.RunNumber}.",
                this
            );
        }
    }

    private void ResetPlayerHealth()
    {
        if (playerHealth == null)
        {
            if (logReset)
                Debug.LogWarning("[PlayerRunResetter] Missing PlayerHealth reference.", this);

            return;
        }

        if (resetHealth)
            playerHealth.currentHealth = Mathf.Clamp(playerHealth.startingHealth, 0, playerHealth.maxHealth);

        if (resetWeight)
            playerHealth.currentWeightKg = playerHealth.startingWeightKg;

        if (resetAnxietyLockedHealth)
        {
            SetPrivateField(playerHealth, "anxietyLockedHealthLoss", 0);
            SetPrivateField(playerHealth, "isRegeneratingAnxietyLockedHealth", false);

            SetPrivateField(playerHealth, "nextAnxietyRegenTickTime", 0f);

            SetPrivateField(playerHealth, "anxietyDamageFlashStartTime", 0f);
            SetPrivateField(playerHealth, "anxietyDamageFlashUntil", 0f);

            SetPrivateField(playerHealth, "anxietyRegenFlashStartPercent", 0f);
            SetPrivateField(playerHealth, "anxietyRegenFlashEndPercent", 0f);
            SetPrivateField(playerHealth, "anxietyRegenFlashStartTime", 0f);
            SetPrivateField(playerHealth, "anxietyRegenFlashUntil", 0f);
        }

        SetPrivateField(playerHealth, "isDead", false);

        CallPrivateMethod(playerHealth, "ResetDamageColor");
        CallPrivateMethod(playerHealth, "UpdateAnimatorHealth");
    }

    private void ResetPlayerAnxiety()
    {
        if (playerAnxiety == null)
        {
            if (logReset)
                Debug.LogWarning("[PlayerRunResetter] Missing PlayerAnxiety reference.", this);

            return;
        }

        if (resetAnxiety)
        {
            playerAnxiety.currentAnxiety = Mathf.Clamp(
                playerAnxiety.startingAnxiety,
                0f,
                playerAnxiety.maxAnxiety
            );
        }

        if (resetOverflowDamage)
        {
            SetPrivateField(playerAnxiety, "accumulatedOverflowDamage", 0f);
            SetPrivateField(playerAnxiety, "queuedOverflowDamage", 0f);
            SetPrivateField(playerAnxiety, "overflowTickTimer", 0f);
            SetPrivateField(playerAnxiety, "isInAnxietyAnimation", false);
        }

        if (stopAnxietyVfx)
        {
            CallPrivateMethod(playerAnxiety, "CleanupContinuousAnxietyVFX");
            CallPrivateMethod(playerAnxiety, "ResetPlayerFlash");
        }

        CallPrivateMethod(playerAnxiety, "UpdateUI");
        CallPrivateMethod(playerAnxiety, "UpdateAnimatorState");
    }

    private void SetPrivateField(object target, string fieldName, object value)
    {
        if (target == null)
            return;

        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
        );

        if (field == null)
        {
            if (logReset)
                Debug.LogWarning(
                    $"[PlayerRunResetter] Could not find field '{fieldName}' on {target.GetType().Name}.",
                    this
                );

            return;
        }

        field.SetValue(target, value);
    }

    private void CallPrivateMethod(object target, string methodName)
    {
        if (target == null)
            return;

        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
        );

        if (method == null)
            return;

        method.Invoke(target, null);
    }
}