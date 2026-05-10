using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SlowableMovement : MonoBehaviour
{
    [Header("Optional References")]
    public CharacterController characterController;
    public NavMeshAgent navMeshAgent;

    [Header("Player Movement Scripts")]
    [Tooltip("Drag your custom player movement script here if it has a public moveSpeed/speed field.")]
    public MonoBehaviour customMovementScript;

    [Tooltip("The field name on your movement script. Common examples: moveSpeed, movementSpeed, speed, walkSpeed.")]
    public string customSpeedFieldName = "moveSpeed";

    [Header("Base Speed")]
    public float baseMoveSpeed = 5f;

    [Tooltip("If true, reads the original speed from NavMeshAgent/custom script on Awake.")]
    public bool autoReadBaseSpeed = true;

    [Header("Debug")]
    public bool logSlowChanges = false;

    private readonly Dictionary<object, float> activeSlows = new Dictionary<object, float>();

    private float currentMultiplier = 1f;
    private System.Reflection.FieldInfo customSpeedField;
    private System.Reflection.PropertyInfo customSpeedProperty;

    private void Awake()
    {
        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        if (navMeshAgent == null)
            navMeshAgent = GetComponent<NavMeshAgent>();

        CacheCustomSpeedMember();

        if (autoReadBaseSpeed)
        {
            if (navMeshAgent != null)
            {
                baseMoveSpeed = navMeshAgent.speed;
            }
            else if (TryGetCustomSpeed(out float customSpeed))
            {
                baseMoveSpeed = customSpeed;
            }
        }

        ApplySpeed();
    }

    public void AddSlow(object source, float multiplier)
    {
        if (source == null)
            return;

        multiplier = Mathf.Clamp01(multiplier);

        activeSlows[source] = multiplier;

        RecalculateMultiplier();
    }

    public void RemoveSlow(object source)
    {
        if (source == null)
            return;

        if (activeSlows.Remove(source))
            RecalculateMultiplier();
    }

    public void ClearAllSlows()
    {
        activeSlows.Clear();
        RecalculateMultiplier();
    }

    private void RecalculateMultiplier()
    {
        float strongestSlow = 1f;

        foreach (float multiplier in activeSlows.Values)
            strongestSlow = Mathf.Min(strongestSlow, multiplier);

        currentMultiplier = strongestSlow;

        ApplySpeed();

        if (logSlowChanges)
        {
            Debug.Log(
                $"[SlowableMovement] {name} slow multiplier: {currentMultiplier}. Active slows: {activeSlows.Count}",
                this
            );
        }
    }

    private void ApplySpeed()
    {
        float finalSpeed = baseMoveSpeed * currentMultiplier;

        if (navMeshAgent != null)
            navMeshAgent.speed = finalSpeed;

        TrySetCustomSpeed(finalSpeed);
    }

    private void CacheCustomSpeedMember()
    {
        if (customMovementScript == null || string.IsNullOrEmpty(customSpeedFieldName))
            return;

        System.Type type = customMovementScript.GetType();

        customSpeedField = type.GetField(
            customSpeedFieldName,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic
        );

        customSpeedProperty = type.GetProperty(
            customSpeedFieldName,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic
        );
    }

    private bool TryGetCustomSpeed(out float speed)
    {
        speed = baseMoveSpeed;

        if (customMovementScript == null)
            return false;

        CacheCustomSpeedMember();

        if (customSpeedField != null && customSpeedField.FieldType == typeof(float))
        {
            speed = (float)customSpeedField.GetValue(customMovementScript);
            return true;
        }

        if (customSpeedProperty != null &&
            customSpeedProperty.PropertyType == typeof(float) &&
            customSpeedProperty.CanRead)
        {
            speed = (float)customSpeedProperty.GetValue(customMovementScript);
            return true;
        }

        return false;
    }

    private bool TrySetCustomSpeed(float speed)
    {
        if (customMovementScript == null)
            return false;

        CacheCustomSpeedMember();

        if (customSpeedField != null && customSpeedField.FieldType == typeof(float))
        {
            customSpeedField.SetValue(customMovementScript, speed);
            return true;
        }

        if (customSpeedProperty != null &&
            customSpeedProperty.PropertyType == typeof(float) &&
            customSpeedProperty.CanWrite)
        {
            customSpeedProperty.SetValue(customMovementScript, speed);
            return true;
        }

        return false;
    }

    private void OnDisable()
    {
        activeSlows.Clear();
        currentMultiplier = 1f;
        ApplySpeed();
    }
}