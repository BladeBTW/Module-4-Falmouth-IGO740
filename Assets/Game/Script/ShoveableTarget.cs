using System.Collections;
using UnityEngine;

public class ShoveableTarget : MonoBehaviour
{
    [Header("Reaction")]
    public bool brieflyDisableNavAgent = true;
    public float navDisableDuration = 0.25f;

    [Header("Animator")]
    public Animator animator;
    public string shovedTriggerName = "Shoved";
    public bool useShovedTrigger = true;

    [Header("Debug")]
    public bool logShoved = false;

    private UnityEngine.AI.NavMeshAgent _agent;
    private Coroutine _reenableRoutine;

    private void Awake()
    {
        _agent = GetComponent<UnityEngine.AI.NavMeshAgent>();

        if (_agent == null)
            _agent = GetComponentInChildren<UnityEngine.AI.NavMeshAgent>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    public void OnShoved(Transform shover, float force)
    {
        if (logShoved)
            Debug.Log($"[ShoveableTarget] {name} shoved by {shover.name}. Force: {force}", this);

        if (useShovedTrigger && animator != null && !string.IsNullOrEmpty(shovedTriggerName))
            animator.SetTrigger(shovedTriggerName);

        if (brieflyDisableNavAgent && _agent != null && _agent.enabled)
        {
            if (_reenableRoutine != null)
                StopCoroutine(_reenableRoutine);

            _reenableRoutine = StartCoroutine(TemporarilyDisableAgent());
        }
    }

    private IEnumerator TemporarilyDisableAgent()
    {
        _agent.enabled = false;

        yield return new WaitForSeconds(navDisableDuration);

        if (_agent != null)
            _agent.enabled = true;

        _reenableRoutine = null;
    }
}