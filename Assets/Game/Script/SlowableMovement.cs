using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SlowableMovement : MonoBehaviour
{
    [Header("Player / Character Movement")]
    [Tooltip("Enable if this object uses your Character.cs movement script.")]
    public bool affectCharacterMoveSpeed = true;

    [Tooltip("Optional. Auto-found if left empty.")]
    public Character character;

    [Header("NPC / NavMeshAgent Movement")]
    [Tooltip("Enable if this object uses NavMeshAgent movement.")]
    public bool affectNavMeshAgentSpeed = true;

    [Tooltip("Optional. Auto-found if left empty.")]
    public NavMeshAgent navMeshAgent;

    [Header("Debug")]
    public bool logSlowChanges = false;

    private readonly Dictionary<Object, float> activeSlowSources = new Dictionary<Object, float>();

    private float baseCharacterMoveSpeed;
    private float baseAgentSpeed;

    private bool hasCharacterBaseSpeed;
    private bool hasAgentBaseSpeed;

    private void Awake()
    {
        if (character == null)
            character = GetComponent<Character>();

        if (character == null)
            character = GetComponentInParent<Character>();

        if (navMeshAgent == null)
            navMeshAgent = GetComponent<NavMeshAgent>();

        if (navMeshAgent == null)
            navMeshAgent = GetComponentInParent<NavMeshAgent>();

        CacheBaseSpeeds();
        ApplyCurrentSlow();
    }

    private void OnEnable()
    {
        CacheBaseSpeeds();
        ApplyCurrentSlow();
    }

    private void CacheBaseSpeeds()
    {
        if (affectCharacterMoveSpeed && character != null && !hasCharacterBaseSpeed)
        {
            baseCharacterMoveSpeed = character.MoveSpeed;
            hasCharacterBaseSpeed = true;
        }

        if (affectNavMeshAgentSpeed && navMeshAgent != null && !hasAgentBaseSpeed)
        {
            baseAgentSpeed = navMeshAgent.speed;
            hasAgentBaseSpeed = true;
        }
    }

    public void AddSlow(Object source, float speedMultiplier)
    {
        if (source == null)
            return;

        CacheBaseSpeeds();

        speedMultiplier = Mathf.Clamp01(speedMultiplier);

        activeSlowSources[source] = speedMultiplier;

        ApplyCurrentSlow();
    }

    public void RemoveSlow(Object source)
    {
        if (source == null)
            return;

        if (activeSlowSources.Remove(source))
            ApplyCurrentSlow();
    }

    public void ClearAllSlows()
    {
        activeSlowSources.Clear();
        ApplyCurrentSlow();
    }

    private void ApplyCurrentSlow()
    {
        float strongestMultiplier = GetStrongestSlowMultiplier();

        if (affectCharacterMoveSpeed && character != null && hasCharacterBaseSpeed)
        {
            character.MoveSpeed = baseCharacterMoveSpeed * strongestMultiplier;
        }

        if (affectNavMeshAgentSpeed && navMeshAgent != null && hasAgentBaseSpeed)
        {
            navMeshAgent.speed = baseAgentSpeed * strongestMultiplier;
        }

        if (logSlowChanges)
        {
            Debug.Log(
                $"[SlowableMovement] {name} slow multiplier: {strongestMultiplier}. Active sources: {activeSlowSources.Count}",
                this
            );
        }
    }

    private float GetStrongestSlowMultiplier()
    {
        if (activeSlowSources.Count <= 0)
            return 1f;

        float strongest = 1f;

        foreach (float multiplier in activeSlowSources.Values)
        {
            strongest = Mathf.Min(strongest, multiplier);
        }

        return Mathf.Clamp01(strongest);
    }

    private void OnDisable()
    {
        RestoreBaseSpeeds();
    }

    private void OnDestroy()
    {
        RestoreBaseSpeeds();
    }

    private void RestoreBaseSpeeds()
    {
        if (affectCharacterMoveSpeed && character != null && hasCharacterBaseSpeed)
            character.MoveSpeed = baseCharacterMoveSpeed;

        if (affectNavMeshAgentSpeed && navMeshAgent != null && hasAgentBaseSpeed)
            navMeshAgent.speed = baseAgentSpeed;
    }
}