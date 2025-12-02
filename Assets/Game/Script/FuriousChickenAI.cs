using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class FuriousChickenAI : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("If assigned, this will be the target. Otherwise finds object with playerTag.")]
    public Transform targetOverride;
    public string playerTag = "Player";

    private Transform _player;
    private PlayerHealth _playerHealth;

    [Header("Aggro")]
    [Tooltip("Start chasing when the player is within this distance.")]
    public float aggroRange = 8f;

    [Tooltip("Stop chasing when the player is farther than this.")]
    public float loseAggroRange = 12f;

    [Tooltip("Within this range, the chicken can attack the player.")]
    public float attackRange = 2f;

    [Header("Attack")]
    [Tooltip("Damage dealt per successful hit.")]
    public int damagePerHit = 50;

    [Tooltip("Attacks per second (how often a NEW attack can start). 1 = once/sec, 2 = twice/sec.")]
    public float attackSpeed = 1f; // attacks per second

    [Header("Attack Timing")]
    [Tooltip("How long one attack animation lasts (seconds). The NPC stays in Attacking = true for this duration.")]
    public float attackAnimDuration = 0.8f;

    [Tooltip("Delay (seconds) from start of attack until damage is applied (line up with hit frame).")]
    public float hitDelay = 0.3f;

    [Header("Animator")]
    [Tooltip("Animator that drives THIS NPC's movement/attack animations. If left empty, will auto-find in children.")]
    public Animator animator;
    [Tooltip("Name of the Animator float parameter controlling locomotion (e.g. 'Speed').")]
    public string speedParamName = "Speed";
    [Tooltip("Name of the Animator bool parameter that switches to attack state (e.g. 'Attacking').")]
    public string attackingBoolName = "Attacking";

    [Header("Movement")]
    [Tooltip("NavMeshAgent movement speed.")]
    public float moveSpeed = 4f;

    [Header("Debug")]
    public bool debug = false;

    private NavMeshAgent _agent;
    private bool _hasAggro = false;

    // Attack state
    private bool _isAttacking = false;
    private bool _hasDealtDamageThisAttack = false;
    private float _attackEndTime = 0f;
    private float _damageTime = 0f;
    private float _nextAttackAllowedTime = 0f;

    // For restoring agent behaviour after attack
    private bool _prevUpdatePosition = true;
    private bool _prevUpdateRotation = true;

    private float AttackInterval => attackSpeed <= 0f ? float.MaxValue : 1f / attackSpeed;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = moveSpeed;
        _agent.updateRotation = true;
        _agent.stoppingDistance = attackRange * 0.8f;

        // Cache original settings
        _prevUpdatePosition = _agent.updatePosition;
        _prevUpdateRotation  = _agent.updateRotation;

        // Auto-find NPC animator if not assigned
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
            if (animator == null)
                Debug.LogError("[FuriousChickenAI] No Animator found on NPC.", this);
        }

        // Resolve player target
        if (targetOverride != null)
        {
            _player = targetOverride;
        }
        else
        {
            GameObject p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null)
                _player = p.transform;
        }

        if (_player != null)
        {
            _playerHealth = _player.GetComponent<PlayerHealth>();
        }

        if (_player == null)
        {
            Debug.LogError(
                $"[FuriousChickenAI] Could not find player. " +
                $"Either assign targetOverride or tag your player '{playerTag}'.",
                this
            );
        }
        else if (_playerHealth == null)
        {
            Debug.LogError(
                "[FuriousChickenAI] Player found but no PlayerHealth on it.",
                _player
            );
        }
    }

    private void Update()
    {
        if (_player == null || _playerHealth == null)
        {
            UpdateAnimatorSpeed(0f);
            SetNpcAttacking(false);
            return;
        }

        // Flat 2D distance (ignore height, so slopes don't break aggro)
        Vector3 selfPos = transform.position;
        Vector3 playerPos = _player.position;
        selfPos.y = 0f;
        playerPos.y = 0f;
        float dist = Vector3.Distance(selfPos, playerPos);

        // Aggro logic
        if (!_hasAggro && dist <= aggroRange)
        {
            _hasAggro = true;
        }
        else if (_hasAggro && dist >= loseAggroRange)
        {
            _hasAggro = false;
            _agent.ResetPath();
        }

        bool inAttackRange = dist <= attackRange;

        // Movement: only move if not currently in an attack
        if (_hasAggro && !_isAttacking)
        {
            _agent.isStopped = false;
            _agent.SetDestination(_player.position);
        }
        else if (!_hasAggro)
        {
            _agent.isStopped = true;
        }
        else if (_isAttacking)
        {
            // Extra safety: keep everything fully stopped while attacking
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
        }

        // Update locomotion speed param (no walking during attack)
        float worldSpeed = new Vector3(_agent.velocity.x, 0f, _agent.velocity.z).magnitude;
        float normSpeed = moveSpeed > 0.01f ? Mathf.Clamp01(worldSpeed / moveSpeed) : 0f;
        if (!_hasAggro || _isAttacking)
            normSpeed = 0f;
        UpdateAnimatorSpeed(normSpeed);

        // --- Attack state machine ---

        // If we are in the middle of an attack, let it finish
        if (_isAttacking)
        {
            // Deal damage once, when hitDelay has passed
            if (!_hasDealtDamageThisAttack && Time.time >= _damageTime)
            {
                DoDamage();
            }

            // End of attack animation duration
            if (Time.time >= _attackEndTime)
            {
                EndAttack();
            }

            return; // Don't start new attacks while one is playing
        }

        // Not currently attacking: can we start a new one?
        if (_hasAggro && inAttackRange && Time.time >= _nextAttackAllowedTime)
        {
            StartAttack();
        }
        else
        {
            // make sure attacking bool is false when idle/chasing
            SetNpcAttacking(false);
        }
    }

    private void StartAttack()
    {
        _isAttacking = true;
        _hasDealtDamageThisAttack = false;

        _attackEndTime = Time.time + attackAnimDuration;
        _damageTime = Time.time + hitDelay;

        // prevent a new attack from starting too soon
        _nextAttackAllowedTime = Time.time + AttackInterval;

        // STOP ALL MOVEMENT DURING ATTACK (hard lock)
        _agent.isStopped = true;
        _agent.ResetPath();
        _agent.velocity = Vector3.zero;

        // Disable automatic NavMesh position/rotation updates while attacking
        _prevUpdatePosition = _agent.updatePosition;
        _prevUpdateRotation  = _agent.updateRotation;
        _agent.updatePosition = false;
        _agent.updateRotation = false;

        SetNpcAttacking(true);

        // Face the player once at the start
        if (_player != null)
        {
            Vector3 lookAt = _player.position;
            lookAt.y = transform.position.y;
            transform.LookAt(lookAt);
        }

        if (debug)
            Debug.Log($"{name} started attack. Ends at {_attackEndTime}, hit at {_damageTime}");
    }

    private void EndAttack()
    {
        _isAttacking = false;
        SetNpcAttacking(false);

        // Re-enable NavMeshAgent updates and movement
        _agent.updatePosition = _prevUpdatePosition;
        _agent.updateRotation = _prevUpdateRotation;
        _agent.isStopped = false;
        _agent.velocity = Vector3.zero;

        if (debug)
            Debug.Log($"{name} finished attack.");
    }

    private void DoDamage()
    {
        if (_playerHealth == null)
            return;

        _playerHealth.TakeDamage(damagePerHit);

        if (debug)
            Debug.Log($"{name} dealt {damagePerHit} damage to player.");

        _hasDealtDamageThisAttack = true;
    }

    private void UpdateAnimatorSpeed(float normalizedSpeed)
    {
        if (animator && !string.IsNullOrEmpty(speedParamName))
            animator.SetFloat(speedParamName, normalizedSpeed);
    }

    private void SetNpcAttacking(bool attacking)
    {
        if (animator && !string.IsNullOrEmpty(attackingBoolName))
            animator.SetBool(attackingBoolName, attacking);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, loseAggroRange);
    }
}
