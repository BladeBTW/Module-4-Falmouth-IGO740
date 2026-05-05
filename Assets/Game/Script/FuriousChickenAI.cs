using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AudioSource))]
public class FuriousChickenAI : MonoBehaviour
{
    [Header("Target")]
    public Transform targetOverride;
    public string playerTag = "Player";

    private Transform _player;
    private PlayerHealth _playerHealth;

    [Header("Aggro")]
    public float aggroRange = 8f;
    public float loseAggroRange = 12f;
    public float attackRange = 2f;

    [Header("Attack")]
    public int damagePerHit = 50;
    public float attackSpeed = 1f;

    [Header("Attack Timing")]
    public float attackAnimDuration = 0.8f;
    public float hitDelay = 0.3f;

    [Header("Animator")]
    public Animator animator;
    public string speedParamName = "Speed";
    public string attackingBoolName = "Attacking";

    [Header("Movement")]
    public float moveSpeed = 4f;

    [Header("NavMesh Safety")]
    public float navMeshSnapDistance = 10f;
    public bool disableIfNotOnNavMesh = true;

    [Header("Approach / Breathing SFX")]
    public AudioClip approachLoopSfx;
    [Range(0f, 10f)]
    public float approachVolume = 1f;

    [Header("Attack SFX")]
    public AudioClip attackSfx;
    [Range(0f, 10f)]
    public float attackVolume = 2f;

    private NavMeshAgent _agent;
    private AudioSource _audio;

    private bool _hasAggro = false;
    private bool _isAttacking = false;
    private bool _hasDealtDamageThisAttack = false;

    private float _attackEndTime = 0f;
    private float _damageTime = 0f;
    private float _nextAttackAllowedTime = 0f;

    private float AttackInterval => attackSpeed <= 0f ? float.MaxValue : 1f / attackSpeed;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _audio = GetComponent<AudioSource>();

        if (_agent != null)
        {
            _agent.speed = moveSpeed;
            _agent.stoppingDistance = attackRange * 0.8f;
        }

        if (_audio != null)
        {
            _audio.spatialBlend = 1f;
            _audio.loop = true;
            _audio.playOnAwake = false;
        }

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        ResolvePlayer();
        EnsureAgentOnNavMesh();
    }

    private void Update()
    {
        if (!AgentReady())
        {
            UpdateAnimatorSpeed(0f);
            SetNpcAttacking(false);
            StopApproachLoop();
            return;
        }

        if (_player == null || _playerHealth == null)
            ResolvePlayer();

        if (_player == null || _playerHealth == null)
        {
            SafeStopAgent();
            UpdateAnimatorSpeed(0f);
            SetNpcAttacking(false);
            StopApproachLoop();
            return;
        }

        Vector3 selfPos = transform.position;
        selfPos.y = 0f;

        Vector3 playerPos = _player.position;
        playerPos.y = 0f;

        float dist = Vector3.Distance(selfPos, playerPos);

        if (!_hasAggro && dist <= aggroRange)
        {
            _hasAggro = true;
            StartApproachLoop();
        }
        else if (_hasAggro && dist >= loseAggroRange)
        {
            _hasAggro = false;
            SafeResetPath();
            StopApproachLoop();
        }

        bool inAttackRange = dist <= attackRange;

        if (_hasAggro && !_isAttacking)
        {
            SafeSetStopped(false);
            SafeSetDestination(_player.position);
        }
        else
        {
            SafeSetStopped(true);
        }

        float speedNorm = 0f;

        if (_hasAggro && !_isAttacking && AgentReady())
            speedNorm = _agent.velocity.magnitude / Mathf.Max(0.01f, moveSpeed);

        UpdateAnimatorSpeed(speedNorm);

        if (_isAttacking)
        {
            if (!_hasDealtDamageThisAttack && Time.time >= _damageTime)
                DoDamage();

            if (Time.time >= _attackEndTime)
                EndAttack();

            return;
        }

        if (_hasAggro && inAttackRange && Time.time >= _nextAttackAllowedTime)
            StartAttack();
        else
            SetNpcAttacking(false);
    }

    private void ResolvePlayer()
    {
        if (targetOverride != null)
            _player = targetOverride;
        else
        {
            GameObject p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null)
                _player = p.transform;
        }

        if (_player != null)
            _playerHealth = _player.GetComponent<PlayerHealth>();
    }

    private bool EnsureAgentOnNavMesh()
    {
        if (_agent == null || !_agent.enabled)
            return false;

        if (_agent.isOnNavMesh)
            return true;

        NavMeshHit hit;

        if (NavMesh.SamplePosition(transform.position, out hit, navMeshSnapDistance, NavMesh.AllAreas))
        {
            _agent.Warp(hit.position);
            return _agent.isOnNavMesh;
        }

        Debug.LogWarning("[FuriousChickenAI] " + gameObject.name + " is not on NavMesh.", this);

        if (disableIfNotOnNavMesh)
            enabled = false;

        return false;
    }

    private bool AgentReady()
    {
        return _agent != null && _agent.enabled && _agent.isOnNavMesh;
    }

    private void SafeSetDestination(Vector3 destination)
    {
        if (!AgentReady())
            return;

        _agent.SetDestination(destination);
    }

    private void SafeSetStopped(bool stopped)
    {
        if (!AgentReady())
            return;

        _agent.isStopped = stopped;
    }

    private void SafeResetPath()
    {
        if (!AgentReady())
            return;

        _agent.ResetPath();
    }

    private void SafeStopAgent()
    {
        if (!AgentReady())
            return;

        _agent.isStopped = true;
        _agent.velocity = Vector3.zero;
    }

    private void StartAttack()
    {
        _isAttacking = true;
        _hasDealtDamageThisAttack = false;

        _attackEndTime = Time.time + attackAnimDuration;
        _damageTime = Time.time + hitDelay;
        _nextAttackAllowedTime = Time.time + AttackInterval;

        SafeStopAgent();

        StopApproachLoop();
        SetNpcAttacking(true);

        if (_player != null)
        {
            Vector3 look = _player.position;
            look.y = transform.position.y;
            transform.LookAt(look);
        }

        if (attackSfx != null && attackVolume > 0f)
            AudioSource.PlayClipAtPoint(attackSfx, transform.position, attackVolume);
    }

    private void EndAttack()
    {
        _isAttacking = false;
        SetNpcAttacking(false);

        if (_hasAggro)
            StartApproachLoop();
    }

    private void DoDamage()
    {
        if (_playerHealth == null)
            return;

        _playerHealth.TakeDamage(damagePerHit);
        _hasDealtDamageThisAttack = true;
    }

    private void StartApproachLoop()
    {
        if (_audio == null || approachLoopSfx == null)
            return;

        if (_audio.isPlaying)
            return;

        _audio.clip = approachLoopSfx;
        _audio.volume = approachVolume;
        _audio.loop = true;
        _audio.Play();
    }

    private void StopApproachLoop()
    {
        if (_audio != null && _audio.isPlaying)
            _audio.Stop();
    }

    private void UpdateAnimatorSpeed(float normalizedSpeed)
    {
        if (animator != null && !string.IsNullOrEmpty(speedParamName))
            animator.SetFloat(speedParamName, normalizedSpeed);
    }

    private void SetNpcAttacking(bool attacking)
    {
        if (animator != null && !string.IsNullOrEmpty(attackingBoolName))
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