using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class FuriousChickenAI : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Animator animator;

    [Header("Detection")]
    public float aggroRange = 6f;
    public float loseRange = 9f;

    [Header("Spotted State")]
    public string spottedBoolParam = "Spotted";

    [Tooltip("How long the furious chicken stays in Spotted before chasing.")]
    public float spottedStateDuration = 0.8f;

    [Tooltip("If ON, spotted VFX/SFX/anxiety only happens once until the chicken loses the player.")]
    public bool playSpottedOnlyOncePerChase = true;

    [Header("Anxiety")]
    [Tooltip("PlayerAnxiety component on the player. Auto-found if empty.")]
    public PlayerAnxiety playerAnxiety;

    public bool increaseAnxietyWhenSpotted = true;

    [Tooltip("One-time anxiety burst when the furious chicken spots the player.")]
    public float spottedAnxietyAmount = 10f;

    public bool increaseAnxietyWhenChasing = true;

    [Tooltip("Anxiety added per second while the furious chicken is chasing.")]
    public float chasingAnxietyPerSecond = 6f;

    [Header("Spotted VFX / SFX On NPC")]
    public GameObject spottedVfxPrefab;

    [Tooltip("Optional spawn point. If empty, this chicken transform is used.")]
    public Transform spottedVfxSpawnPoint;

    public Vector3 spottedVfxLocalOffset = new Vector3(0f, 1.2f, 0f);
    public Vector3 spottedVfxLocalRotationEuler = Vector3.zero;
    public Vector3 spottedVfxLocalScale = Vector3.one;

    public bool parentSpottedVfxToSpawnPoint = true;
    public float spottedVfxLifetime = 2f;

    [Tooltip("Forces spawned spotted VFX onto this layer. Leave empty to ignore.")]
    public string forceSpottedVfxLayer = "VFX";

    public AudioClip spottedSfx;

    [Range(0f, 10f)]
    public float spottedSfxVolume = 1f;

    [Header("Player Chase VFX")]
    [Tooltip("VFX spawned on the player while this furious chicken is chasing.")]
    public GameObject chaseVfxPrefab;

    [Tooltip("Optional player-side spawn point. If empty, VFX attaches to player root.")]
    public Transform chaseVfxSpawnPoint;

    public Vector3 chaseVfxLocalOffset = new Vector3(0f, 0.8f, 0f);
    public Vector3 chaseVfxLocalRotationEuler = Vector3.zero;
    public Vector3 chaseVfxLocalScale = Vector3.one;

    [Tooltip("If true, existing chase VFX particles fade naturally when chase ends.")]
    public bool smoothStopChaseVfx = true;

    [Tooltip("Destroy delay after smooth stop.")]
    public float chaseVfxDestroyDelay = 2f;

    [Tooltip("Forces spawned chase VFX onto this layer. Leave empty to ignore.")]
    public string forceChaseVfxLayer = "VFX";

    [Header("Attack")]
    public float attackRange = 2.5f;
    public int attackDamage = 60;
    public float attackCooldown = 2.5f;

    [Tooltip("How long the AI stays locked in attack behaviour. This does NOT control hit timing.")]
    public float attackDuration = 1.2f;

    [Tooltip("Bool parameter used to enter the attack animation.")]
    public string attackBoolParam = "Attacking";

    [Header("Animation Event Damage")]
    public bool useAnimationEventDamage = true;
    public float attackDamageExtraRange = 0.4f;

    [Header("Continuous Touch Damage")]
    public bool damagePlayerOnTouch = true;
    public float touchDamageRadius = 2f;

    [Tooltip("Damage applied per touch tick, not per frame.")]
    public int touchDamagePerTick = 1;

    [Tooltip("How often touch damage is applied while touching the player.")]
    public float touchDamageTickInterval = 0.3f;

    [Header("Attack SFX")]
    public AudioClip attackSfx;

    [Range(0f, 10f)]
    public float attackSfxVolume = 1f;

    [Header("Touch / Growl SFX")]
    public AudioClip touchDamageSfx;

    [Range(0f, 10f)]
    public float touchDamageSfxVolume = 1f;

    public bool playTouchSfxOncePerContact = true;
    public bool loopTouchSfxWhileTouching = false;

    [Header("Movement")]
    public float moveSpeed = 4f;
    public float turnSpeed = 8f;

    [Header("Patrol")]
    public float patrolRadius = 10f;
    public float minWalkDistance = 2f;
    public float maxWalkDistance = 6f;
    public float minIdleTime = 1f;
    public float maxIdleTime = 4f;

    [Header("Animation")]
    public string speedFloatParam = "Speed";

    [Header("Debug")]
    public bool logAnxiety = false;

    private NavMeshAgent agent;

    private AudioSource attackAudioSource;
    private AudioSource touchAudioSource;
    private AudioSource spottedAudioSource;

    private PlayerHealth playerHealth;

    private Vector3 spawnPosition;

    private float idleTimer;
    private float nextAttackTime;
    private float nextTouchDamageTime;
    private float lostChaseTimer;

    private bool isIdle = true;
    private bool isChasing;
    private bool isSpotting;
    private bool isAttacking;
    private bool hasDamagedThisAttack;
    private bool touchSfxPlayedThisContact;
    private bool hasAppliedSpottedThisChase;

    private GameObject activeChaseVfx;
    private bool chaseVfxActive;

    private Coroutine attackRoutine;
    private Coroutine spottedRoutine;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.updateRotation = false;
            agent.autoBraking = true;
        }

        SetupAudioSources();
    }

    private void Start()
    {
        spawnPosition = transform.position;

        SnapToNavMesh();
        CachePlayerComponents();
        StartIdle();
    }

    private void Update()
    {
        if (!AgentReady())
        {
            UpdateAnimatorSpeed(0f);
            HideChaseVfx();
            return;
        }

        if (player == null)
        {
            StopAgentHard();
            UpdateAnimatorSpeed(0f);
            HideChaseVfx();
            return;
        }

        CachePlayerComponents();

        float distanceToPlayer =
            FlatDistance(transform.position, player.position);

        HandleDetection(distanceToPlayer);
        HandleContinuousTouchDamage(distanceToPlayer);

        if (isSpotting)
        {
            StopAgentHard();
            FacePlayer();
            UpdateAnimatorSpeed(0f);
            HideChaseVfx();
            return;
        }

        if (isAttacking)
        {
            StopAgentHard();
            FacePlayer();
            UpdateAnimatorSpeed(0f);
            return;
        }

        UpdateChaseVfx();

        if (isChasing)
        {
            ChaseUpdate(distanceToPlayer);
        }
        else
        {
            PatrolUpdate();
        }
    }

    private void HandleDetection(float distanceToPlayer)
    {
        if (!isChasing && !isSpotting && distanceToPlayer <= aggroRange)
        {
            StartSpottedState();
            return;
        }

        if (isChasing && distanceToPlayer >= loseRange)
        {
            lostChaseTimer += Time.deltaTime;

            if (lostChaseTimer >= 0.15f)
                LosePlayer();

            return;
        }

        if (isChasing)
            lostChaseTimer = 0f;
    }

    private void StartSpottedState()
    {
        if (isSpotting)
            return;

        isSpotting = true;
        isChasing = false;
        isIdle = false;
        lostChaseTimer = 0f;

        StopAgentHard();
        HideChaseVfx();
        FacePlayer();
        UpdateAnimatorSpeed(0f);

        SetSpottedAnimation(true);

        if (!playSpottedOnlyOncePerChase || !hasAppliedSpottedThisChase)
        {
            hasAppliedSpottedThisChase = true;

            PlaySpottedEffects();

            if (increaseAnxietyWhenSpotted)
                AddPlayerAnxiety(spottedAnxietyAmount, "spotted");
        }

        if (spottedRoutine != null)
            StopCoroutine(spottedRoutine);

        spottedRoutine = StartCoroutine(SpottedRoutine());
    }

    private IEnumerator SpottedRoutine()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, spottedStateDuration));

        SetSpottedAnimation(false);

        isSpotting = false;
        isChasing = true;
        isIdle = false;
        lostChaseTimer = 0f;

        ShowChaseVfx();

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = moveSpeed;

            if (player != null)
                agent.SetDestination(player.position);
        }

        spottedRoutine = null;
    }

    private void LosePlayer()
    {
        isChasing = false;
        isSpotting = false;
        isAttacking = false;
        lostChaseTimer = 0f;
        hasAppliedSpottedThisChase = false;

        HideChaseVfx();
        SetSpottedAnimation(false);
        SetAttackAnimation(false);

        StartIdle();
    }

    private void CachePlayerComponents()
    {
        if (player == null)
            return;

        if (playerHealth == null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();

            if (playerHealth == null)
                playerHealth = player.GetComponentInParent<PlayerHealth>();
        }

        if (playerAnxiety == null)
        {
            playerAnxiety = player.GetComponent<PlayerAnxiety>();

            if (playerAnxiety == null)
                playerAnxiety = player.GetComponentInParent<PlayerAnxiety>();
        }
    }

    private void SetupAudioSources()
    {
        attackAudioSource = gameObject.AddComponent<AudioSource>();
        attackAudioSource.playOnAwake = false;
        attackAudioSource.spatialBlend = 1f;
        attackAudioSource.loop = false;

        touchAudioSource = gameObject.AddComponent<AudioSource>();
        touchAudioSource.playOnAwake = false;
        touchAudioSource.spatialBlend = 1f;
        touchAudioSource.loop = false;

        spottedAudioSource = gameObject.AddComponent<AudioSource>();
        spottedAudioSource.playOnAwake = false;
        spottedAudioSource.spatialBlend = 1f;
        spottedAudioSource.loop = false;
    }

    private void AddPlayerAnxiety(float amount, string reason)
    {
        if (amount <= 0f)
            return;

        if (playerAnxiety == null)
            CachePlayerComponents();

        if (playerAnxiety == null)
        {
            if (logAnxiety)
                Debug.LogWarning("[FuriousChickenAI] Could not add anxiety because PlayerAnxiety was not found.", this);

            return;
        }

        // Important:
        // Use PlayerAnxiety.AddAnxiety(), not currentAnxiety += amount.
        // AddAnxiety updates the UI, animator state, and overflow damage.
        playerAnxiety.AddAnxiety(amount);

        if (logAnxiety)
        {
            Debug.Log(
                $"[FuriousChickenAI] Added {amount:0.00} anxiety from {reason}. Current anxiety: {playerAnxiety.currentAnxiety:0.00}",
                this
            );
        }
    }

    private void PlaySpottedEffects()
    {
        SpawnSpottedVfx();

        if (spottedSfx != null && spottedAudioSource != null)
            spottedAudioSource.PlayOneShot(spottedSfx, spottedSfxVolume);
    }

    private void SpawnSpottedVfx()
    {
        if (spottedVfxPrefab == null)
            return;

        Transform origin =
            spottedVfxSpawnPoint != null
                ? spottedVfxSpawnPoint
                : transform;

        Vector3 worldPosition =
            origin.position +
            origin.TransformDirection(spottedVfxLocalOffset);

        Quaternion worldRotation =
            origin.rotation * Quaternion.Euler(spottedVfxLocalRotationEuler);

        GameObject vfx = Instantiate(
            spottedVfxPrefab,
            worldPosition,
            worldRotation
        );

        vfx.transform.localScale = spottedVfxLocalScale;

        if (parentSpottedVfxToSpawnPoint)
            vfx.transform.SetParent(origin, true);

        ForceLayerIfNeeded(vfx, forceSpottedVfxLayer);
        PlayParticleSystems(vfx);

        if (spottedVfxLifetime > 0f)
            Destroy(vfx, spottedVfxLifetime);
    }

    private void UpdateChaseVfx()
    {
        if (isChasing)
            ShowChaseVfx();
        else
            HideChaseVfx();
    }

    private void ShowChaseVfx()
    {
        if (chaseVfxPrefab == null)
            return;

        if (player == null)
            return;

        if (chaseVfxActive && activeChaseVfx != null)
            return;

        Transform parent =
            chaseVfxSpawnPoint != null
                ? chaseVfxSpawnPoint
                : player;

        if (activeChaseVfx == null)
        {
            activeChaseVfx = Instantiate(
                chaseVfxPrefab,
                parent
            );

            activeChaseVfx.transform.localPosition = chaseVfxLocalOffset;
            activeChaseVfx.transform.localRotation =
                Quaternion.Euler(chaseVfxLocalRotationEuler);
            activeChaseVfx.transform.localScale = chaseVfxLocalScale;

            ForceLayerIfNeeded(activeChaseVfx, forceChaseVfxLayer);
        }

        activeChaseVfx.SetActive(true);
        chaseVfxActive = true;

        PlayParticleSystems(activeChaseVfx);
    }

    private void HideChaseVfx()
    {
        if (!chaseVfxActive)
            return;

        if (activeChaseVfx != null)
        {
            ParticleSystem[] particles =
                activeChaseVfx.GetComponentsInChildren<ParticleSystem>(true);

            foreach (ParticleSystem ps in particles)
            {
                if (ps == null)
                    continue;

                ps.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmitting
                );
            }

            if (!smoothStopChaseVfx)
            {
                activeChaseVfx.SetActive(false);
            }
            else
            {
                Destroy(activeChaseVfx, Mathf.Max(0f, chaseVfxDestroyDelay));
                activeChaseVfx = null;
            }
        }

        chaseVfxActive = false;
    }

    private void PlayParticleSystems(GameObject obj)
    {
        if (obj == null)
            return;

        ParticleSystem[] particles =
            obj.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem ps in particles)
        {
            if (ps != null)
                ps.Play(true);
        }
    }

    private void ForceLayerIfNeeded(GameObject obj, string layerName)
    {
        if (obj == null)
            return;

        if (string.IsNullOrWhiteSpace(layerName))
            return;

        int layer = LayerMask.NameToLayer(layerName);

        if (layer < 0)
            return;

        SetLayerRecursively(obj, layer);
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null)
            return;

        obj.layer = layer;

        foreach (Transform child in obj.transform)
        {
            if (child != null)
                SetLayerRecursively(child.gameObject, layer);
        }
    }

    private void ChaseUpdate(float distanceToPlayer)
    {
        isIdle = false;

        if (increaseAnxietyWhenChasing)
            AddPlayerAnxiety(chasingAnxietyPerSecond * Time.deltaTime, "chase");

        if (distanceToPlayer <= attackRange)
        {
            TryStartAttack();
            return;
        }

        agent.isStopped = false;
        agent.speed = moveSpeed;
        agent.SetDestination(player.position);

        RotateTowardVelocity();
        UpdateAnimatorSpeed(agent.velocity.magnitude);
    }

    private void TryStartAttack()
    {
        StopAgentHard();
        FacePlayer();
        UpdateAnimatorSpeed(0f);

        if (Time.time < nextAttackTime)
            return;

        if (attackRoutine != null)
            return;

        attackRoutine = StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        hasDamagedThisAttack = false;
        nextAttackTime = Time.time + attackCooldown;

        StopAgentHard();
        FacePlayer();
        UpdateAnimatorSpeed(0f);

        if (attackSfx != null && attackAudioSource != null)
            attackAudioSource.PlayOneShot(attackSfx, attackSfxVolume);

        SetAttackAnimation(true);

        yield return null;

        SetAttackAnimation(false);

        yield return new WaitForSeconds(attackDuration);

        isAttacking = false;
        hasDamagedThisAttack = false;
        attackRoutine = null;
    }

    // Called by the Animation Event on the furious chicken attack clip.
    // Event function name should be exactly:
    // AttackHitEvent
    public void AttackHitEvent()
    {
        Debug.Log("[FuriousChickenAI] AttackHitEvent fired.");

        if (!useAnimationEventDamage)
            return;

        if (!isAttacking)
        {
            Debug.Log("[FuriousChickenAI] Attack event fired, but chicken is not currently attacking.");
            return;
        }

        if (hasDamagedThisAttack)
            return;

        hasDamagedThisAttack = true;

        DamagePlayerIfInRange(
            attackDamage,
            attackRange + attackDamageExtraRange
        );
    }

    private void HandleContinuousTouchDamage(float distanceToPlayer)
    {
        if (!damagePlayerOnTouch || player == null)
        {
            StopTouchSfxIfNeeded();
            return;
        }

        if (isSpotting)
        {
            StopTouchSfxIfNeeded();
            return;
        }

        bool touchingPlayer = distanceToPlayer <= touchDamageRadius;

        if (!touchingPlayer)
        {
            touchSfxPlayedThisContact = false;
            StopTouchSfxIfNeeded();
            return;
        }

        HandleTouchSfx();

        if (Time.time < nextTouchDamageTime)
            return;

        nextTouchDamageTime = Time.time + touchDamageTickInterval;

        DamagePlayerIfInRange(
            touchDamagePerTick,
            touchDamageRadius
        );
    }

    private void DamagePlayerIfInRange(int damage, float range)
    {
        if (player == null)
            return;

        float distance = FlatDistance(transform.position, player.position);

        if (distance > range)
            return;

        if (playerHealth == null)
            CachePlayerComponents();

        if (playerHealth == null)
            return;

        playerHealth.TakeDamage(damage, gameObject);
    }

    private void HandleTouchSfx()
    {
        if (touchDamageSfx == null || touchAudioSource == null)
            return;

        touchAudioSource.volume = touchDamageSfxVolume;

        if (loopTouchSfxWhileTouching)
        {
            if (!touchAudioSource.isPlaying)
            {
                touchAudioSource.clip = touchDamageSfx;
                touchAudioSource.loop = true;
                touchAudioSource.Play();
            }

            return;
        }

        if (playTouchSfxOncePerContact)
        {
            if (!touchSfxPlayedThisContact)
            {
                touchSfxPlayedThisContact = true;
                touchAudioSource.loop = false;
                touchAudioSource.clip = touchDamageSfx;
                touchAudioSource.Play();
            }

            return;
        }

        if (!touchAudioSource.isPlaying)
        {
            touchAudioSource.loop = false;
            touchAudioSource.clip = touchDamageSfx;
            touchAudioSource.Play();
        }
    }

    private void StopTouchSfxIfNeeded()
    {
        if (touchAudioSource == null)
            return;

        if (touchAudioSource.isPlaying && loopTouchSfxWhileTouching)
            touchAudioSource.Stop();
    }

    private void PatrolUpdate()
    {
        if (isIdle)
        {
            idleTimer -= Time.deltaTime;

            StopAgentHard();
            UpdateAnimatorSpeed(0f);

            if (idleTimer <= 0f)
                PickRandomDestination();

            return;
        }

        if (!agent.pathPending &&
            agent.remainingDistance <= agent.stoppingDistance + 0.2f)
        {
            StartIdle();
            return;
        }

        RotateTowardVelocity();
        UpdateAnimatorSpeed(agent.velocity.magnitude);
    }

    private void StartIdle()
    {
        isIdle = true;
        idleTimer = Random.Range(minIdleTime, maxIdleTime);

        StopAgentHard();
        UpdateAnimatorSpeed(0f);
    }

    private void PickRandomDestination()
    {
        if (!AgentReady())
            return;

        for (int i = 0; i < 12; i++)
        {
            Vector2 random =
                Random.insideUnitCircle.normalized *
                Random.Range(minWalkDistance, maxWalkDistance);

            Vector3 candidate =
                transform.position + new Vector3(random.x, 0f, random.y);

            if (FlatDistance(candidate, spawnPosition) > patrolRadius)
                continue;

            if (NavMesh.SamplePosition(
                candidate,
                out NavMeshHit hit,
                3f,
                agent.areaMask))
            {
                isIdle = false;
                agent.isStopped = false;
                agent.speed = moveSpeed;
                agent.SetDestination(hit.position);
                return;
            }
        }

        StartIdle();
    }

    private void SetAttackAnimation(bool value)
    {
        if (animator == null)
            return;

        if (string.IsNullOrEmpty(attackBoolParam))
            return;

        animator.SetBool(attackBoolParam, value);
    }

    private void SetSpottedAnimation(bool value)
    {
        if (animator == null)
            return;

        if (string.IsNullOrEmpty(spottedBoolParam))
            return;

        animator.SetBool(spottedBoolParam, value);
    }

    private void UpdateAnimatorSpeed(float speed)
    {
        if (animator == null)
            return;

        if (string.IsNullOrEmpty(speedFloatParam))
            return;

        animator.SetFloat(speedFloatParam, speed);
    }

    private void FacePlayer()
    {
        if (player == null)
            return;

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction.normalized);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * turnSpeed
        );
    }

    private void RotateTowardVelocity()
    {
        if (agent == null)
            return;

        Vector3 velocity = agent.velocity;
        velocity.y = 0f;

        if (velocity.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(velocity.normalized);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * turnSpeed
        );
    }

    private void StopAgentHard()
    {
        if (!AgentReady())
            return;

        agent.isStopped = true;
        agent.velocity = Vector3.zero;
    }

    private bool AgentReady()
    {
        return agent != null && agent.enabled && agent.isOnNavMesh;
    }

    private void SnapToNavMesh()
    {
        if (agent == null)
            return;

        if (NavMesh.SamplePosition(
            transform.position,
            out NavMeshHit hit,
            3f,
            agent.areaMask))
        {
            transform.position = hit.position;
        }
    }

    private float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;

        return Vector3.Distance(a, b);
    }

    private void OnDisable()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        if (spottedRoutine != null)
        {
            StopCoroutine(spottedRoutine);
            spottedRoutine = null;
        }

        isAttacking = false;
        isSpotting = false;
        isChasing = false;
        hasDamagedThisAttack = false;
        hasAppliedSpottedThisChase = false;

        SetAttackAnimation(false);
        SetSpottedAnimation(false);

        if (attackAudioSource != null)
            attackAudioSource.Stop();

        if (touchAudioSource != null)
            touchAudioSource.Stop();

        if (spottedAudioSource != null)
            spottedAudioSource.Stop();

        if (activeChaseVfx != null)
            Destroy(activeChaseVfx);

        activeChaseVfx = null;
        chaseVfxActive = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRange);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, touchDamageRadius);
    }
}