using System.Collections;
using UnityEngine;

public class ContactAnxietySource : MonoBehaviour
{
    [Header("Anxiety")]
    public float anxietyPerSecond = 1f;

    [Header("Detection")]
    public string playerTag = "Player";
    public float contactRadius = 1f;
    public Vector3 contactOffset = Vector3.zero;
    public LayerMask playerLayer = ~0;

    [Header("Touch VFX")]
    public GameObject touchVfxPrefab;

    [Tooltip("Optional player-side spawn point. If empty, VFX attaches to the player root.")]
    public Transform playerVfxSpawnPoint;

    public Vector3 vfxLocalOffset = new Vector3(0f, 0.5f, 0f);
    public Vector3 vfxLocalRotationEuler = Vector3.zero;
    public Vector3 vfxLocalScale = Vector3.one;

    [Tooltip("If true, one VFX stays alive while touching, then fades out smoothly.")]
    public bool keepVfxWhileTouching = true;

    [Tooltip("Used only if Keep Vfx While Touching is OFF.")]
    public float oneShotVfxLifetime = 1.5f;

    [Header("Smooth Fade")]
    [Tooltip("How long the VFX waits after contact stops before stopping emission.")]
    public float stopDelay = 0.05f;

    [Tooltip("Extra time after particles stop emitting before destroying the VFX object.")]
    public float fadeOutExtraTime = 1f;

    [Tooltip("If true, particles are cleared when the VFX first starts.")]
    public bool clearParticlesOnStart = false;

    [Tooltip("If true, the touch VFX will be destroyed after fade instead of being disabled.")]
    public bool destroyVfxAfterFade = true;

    [Header("Debug")]
    public bool logDebug = false;

    private PlayerAnxiety currentPlayerAnxiety;
    private Transform currentPlayerTransform;
    private GameObject activeTouchVfx;
    private ParticleSystem[] activeParticles;

    private bool wasTouchingLastFrame;
    private Coroutine stopVfxRoutine;

    private void Update()
    {
        FindTouchingPlayer();

        bool isTouching = currentPlayerAnxiety != null;

        if (isTouching)
        {
            // Important:
            // Use AddAnxiety directly instead of AddAnxietyOverTime,
            // because AddAnxietyOverTime may trigger PlayerAnxiety's own continuous VFX.
            currentPlayerAnxiety.AddAnxiety(anxietyPerSecond * Time.deltaTime);

            if (!wasTouchingLastFrame)
                SpawnOrResumeTouchVfx();
            else
                KeepTouchVfxAlive();
        }
        else
        {
            if (wasTouchingLastFrame)
                BeginSmoothStopTouchVfx();
        }

        wasTouchingLastFrame = isTouching;
    }

    private void FindTouchingPlayer()
    {
        currentPlayerAnxiety = null;
        currentPlayerTransform = null;

        Vector3 checkPosition =
            transform.position + transform.TransformDirection(contactOffset);

        Collider[] hits = Physics.OverlapSphere(
            checkPosition,
            contactRadius,
            playerLayer,
            QueryTriggerInteraction.Collide
        );

        foreach (Collider hit in hits)
        {
            if (hit == null)
                continue;

            if (!IsPlayerCollider(hit))
                continue;

            PlayerAnxiety anxiety = hit.GetComponent<PlayerAnxiety>();

            if (anxiety == null)
                anxiety = hit.GetComponentInParent<PlayerAnxiety>();

            if (anxiety == null && hit.transform.root != null)
                anxiety = hit.transform.root.GetComponent<PlayerAnxiety>();

            if (anxiety == null)
                continue;

            currentPlayerAnxiety = anxiety;
            currentPlayerTransform = anxiety.transform;
            return;
        }
    }

    private bool IsPlayerCollider(Collider hit)
    {
        if (hit == null)
            return false;

        if (string.IsNullOrWhiteSpace(playerTag))
            return true;

        bool hitHasTag = false;
        bool rootHasTag = false;

        try
        {
            hitHasTag = hit.CompareTag(playerTag);

            if (hit.transform.root != null)
                rootHasTag = hit.transform.root.CompareTag(playerTag);
        }
        catch
        {
            // If the tag does not exist in Unity Tags & Layers,
            // avoid hard-spamming errors from CompareTag.
            if (logDebug)
                Debug.LogWarning($"[ContactAnxietySource] Player tag '{playerTag}' is not defined.", this);

            return false;
        }

        return hitHasTag || rootHasTag;
    }

    private void SpawnOrResumeTouchVfx()
    {
        if (touchVfxPrefab == null)
            return;

        Transform parent = playerVfxSpawnPoint != null
            ? playerVfxSpawnPoint
            : currentPlayerTransform;

        if (parent == null)
            return;

        if (stopVfxRoutine != null)
        {
            StopCoroutine(stopVfxRoutine);
            stopVfxRoutine = null;
        }

        if (keepVfxWhileTouching)
        {
            if (activeTouchVfx == null)
            {
                activeTouchVfx = Instantiate(touchVfxPrefab, parent);

                activeTouchVfx.transform.localPosition = vfxLocalOffset;
                activeTouchVfx.transform.localRotation =
                    Quaternion.Euler(vfxLocalRotationEuler);
                activeTouchVfx.transform.localScale = vfxLocalScale;

                activeParticles =
                    activeTouchVfx.GetComponentsInChildren<ParticleSystem>(true);

                if (logDebug)
                    Debug.Log("[ContactAnxietySource] Created touch VFX.", this);
            }

            activeTouchVfx.SetActive(true);
            PlayParticles(activeTouchVfx, clearParticlesOnStart);
        }
        else
        {
            GameObject vfx = Instantiate(touchVfxPrefab, parent);

            vfx.transform.localPosition = vfxLocalOffset;
            vfx.transform.localRotation =
                Quaternion.Euler(vfxLocalRotationEuler);
            vfx.transform.localScale = vfxLocalScale;

            PlayParticles(vfx, clearParticlesOnStart);

            if (oneShotVfxLifetime > 0f)
                Destroy(vfx, oneShotVfxLifetime);
        }
    }

    private void KeepTouchVfxAlive()
    {
        if (!keepVfxWhileTouching)
            return;

        if (activeTouchVfx == null)
            return;

        if (stopVfxRoutine != null)
        {
            StopCoroutine(stopVfxRoutine);
            stopVfxRoutine = null;
        }

        if (!activeTouchVfx.activeSelf)
            activeTouchVfx.SetActive(true);

        if (activeParticles == null || activeParticles.Length == 0)
            activeParticles = activeTouchVfx.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem ps in activeParticles)
        {
            if (ps == null)
                continue;

            if (!ps.isPlaying)
                ps.Play(true);
        }
    }

    private void BeginSmoothStopTouchVfx()
    {
        if (activeTouchVfx == null)
            return;

        if (stopVfxRoutine != null)
            StopCoroutine(stopVfxRoutine);

        stopVfxRoutine = StartCoroutine(SmoothStopTouchVfxRoutine());
    }

    private IEnumerator SmoothStopTouchVfxRoutine()
    {
        if (activeTouchVfx == null)
            yield break;

        if (stopDelay > 0f)
            yield return new WaitForSeconds(stopDelay);

        if (activeTouchVfx == null)
            yield break;

        if (activeParticles == null || activeParticles.Length == 0)
            activeParticles = activeTouchVfx.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem ps in activeParticles)
        {
            if (ps == null)
                continue;

            // This is the important bit:
            // stop making NEW particles, but let existing particles fade naturally.
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        float maxRemainingLifetime = 0.25f;

        foreach (ParticleSystem ps in activeParticles)
        {
            if (ps == null)
                continue;

            ParticleSystem.MainModule main = ps.main;
            maxRemainingLifetime = Mathf.Max(
                maxRemainingLifetime,
                main.startLifetime.constantMax
            );
        }

        yield return new WaitForSeconds(maxRemainingLifetime + fadeOutExtraTime);

        if (activeTouchVfx != null)
        {
            if (destroyVfxAfterFade)
            {
                Destroy(activeTouchVfx);
            }
            else
            {
                activeTouchVfx.SetActive(false);
            }
        }

        activeTouchVfx = null;
        activeParticles = null;
        stopVfxRoutine = null;
    }

    private void PlayParticles(GameObject target, bool clearFirst)
    {
        if (target == null)
            return;

        ParticleSystem[] particles =
            target.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem ps in particles)
        {
            if (ps == null)
                continue;

            if (clearFirst)
                ps.Clear(true);

            ps.Play(true);
        }
    }

    private void OnDisable()
    {
        if (stopVfxRoutine != null)
        {
            StopCoroutine(stopVfxRoutine);
            stopVfxRoutine = null;
        }

        if (activeTouchVfx != null)
            Destroy(activeTouchVfx);

        activeTouchVfx = null;
        activeParticles = null;
        currentPlayerAnxiety = null;
        currentPlayerTransform = null;
        wasTouchingLastFrame = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;

        Vector3 checkPosition =
            transform.position + transform.TransformDirection(contactOffset);

        Gizmos.DrawWireSphere(checkPosition, contactRadius);
    }
}