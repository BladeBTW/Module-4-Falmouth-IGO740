using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerShove : MonoBehaviour
{
    [Header("Input")]
    public KeyCode shoveKey = KeyCode.Space;
    public bool allowMouseClickShove = false;
    public int mouseButton = 0;

    [Header("Shove Area")]
    public Transform shoveOrigin;
    public float shoveRadius = 0.8f;
    public float shoveRange = 1.4f;
    public LayerMask shoveLayers = ~0;

    [Header("Shove Force")]
    public float shoveForce = 4f;
    public float upwardForce = 0.5f;
    public ForceMode forceMode = ForceMode.VelocityChange;

    [Header("Cooldown")]
    public float shoveCooldown = 0.35f;

    [Header("Anxiety")]
    [Tooltip("This amount is added instantly per successful shove. No Time.deltaTime.")]
    public float anxietyIncreasePerShove = 50f;

    [Tooltip("If true, anxiety is added once per shove button press, even if multiple chickens are hit.")]
    public bool addAnxietyOnlyOncePerShove = true;

    [Tooltip("If true, anxiety is only added if at least one target was actually shoved.")]
    public bool onlyAddAnxietyWhenTargetHit = true;

    [Header("Target Filters")]
    public bool affectNPCs = true;
    public bool affectEnemies = true;
    public string npcTag = "NPC";
    public string enemyTag = "Enemy";

    [Header("VFX / SFX")]
    public GameObject shoveVfxPrefab;
    public Transform shoveVfxSpawnPoint;
    public Vector3 shoveVfxOffset = Vector3.zero;
    public Vector3 shoveVfxRotationEuler = Vector3.zero;

    [Tooltip("How long the shove VFX emits before fading naturally.")]
    public float shoveVfxLifetime = 0.35f;

    [Tooltip("Extra time after particles stop emitting before the VFX object is destroyed.")]
    public float shoveVfxExtraFadeTime = 0.35f;

    public AudioClip shoveSfx;

    [Range(0f, 10f)]
    public float shoveSfxVolume = 1f;

    public AudioSource shoveAudioSource;

    [Header("Debug")]
    public bool logShoves = true;
    public bool drawGizmos = true;

    private float _nextShoveTime;
    private PlayerAnxiety _playerAnxiety;
    private readonly HashSet<Transform> _hitRootsThisShove = new HashSet<Transform>();

    private void Awake()
    {
        _playerAnxiety = GetComponent<PlayerAnxiety>();

        if (_playerAnxiety == null)
            _playerAnxiety = GetComponentInChildren<PlayerAnxiety>();

        if (shoveOrigin == null)
            shoveOrigin = transform;
    }

    private void Update()
    {
        bool pressedKeyboard = Input.GetKeyDown(shoveKey);
        bool pressedMouse = allowMouseClickShove && Input.GetMouseButtonDown(mouseButton);

        if (pressedKeyboard || pressedMouse)
            TryShove();
    }

    public void TryShove()
    {
        if (Time.time < _nextShoveTime)
            return;

        _nextShoveTime = Time.time + shoveCooldown;

        bool hitSomething = PerformShove();

        if (!onlyAddAnxietyWhenTargetHit || hitSomething)
            AddShoveAnxiety();

        if (hitSomething)
        {
            SpawnShoveVFX();
            PlayShoveSFX();
        }
    }

    private bool PerformShove()
    {
        _hitRootsThisShove.Clear();

        Vector3 origin = shoveOrigin != null ? shoveOrigin.position : transform.position;
        Vector3 forward = shoveOrigin != null ? shoveOrigin.forward : transform.forward;
        Vector3 center = origin + forward.normalized * shoveRange;

        Collider[] hits = Physics.OverlapSphere(
            center,
            shoveRadius,
            shoveLayers,
            QueryTriggerInteraction.Ignore
        );

        bool hitSomething = false;

        foreach (Collider hit in hits)
        {
            if (hit == null)
                continue;

            if (hit.transform == transform || hit.transform.IsChildOf(transform))
                continue;

            Transform root = GetTargetRoot(hit);

            if (root == null)
                continue;

            if (_hitRootsThisShove.Contains(root))
                continue;

            if (!IsValidTarget(root, hit))
                continue;

            _hitRootsThisShove.Add(root);

            bool shoved = ShoveTarget(hit, root, forward);

            if (!shoved)
                continue;

            hitSomething = true;

            if (!addAnxietyOnlyOncePerShove)
                AddShoveAnxiety();

            if (logShoves)
                Debug.Log($"[PlayerShove] Shoved {root.name}.", root);
        }

        return hitSomething;
    }

    private bool ShoveTarget(Collider hitCollider, Transform targetRoot, Vector3 shoveDirection)
    {
        ShoveableTarget shoveable = targetRoot.GetComponent<ShoveableTarget>();

        if (shoveable == null)
            shoveable = hitCollider.GetComponentInParent<ShoveableTarget>();

        Rigidbody rb = hitCollider.attachedRigidbody;

        if (rb == null && targetRoot != null)
            rb = targetRoot.GetComponent<Rigidbody>();

        if (rb != null)
        {
            Vector3 direction = shoveDirection;

            if (direction.sqrMagnitude < 0.001f)
                direction = targetRoot.position - transform.position;

            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
                direction = transform.forward;

            direction.Normalize();

            Vector3 force = direction * shoveForce;
            force.y += upwardForce;

            rb.AddForce(force, forceMode);
        }

        if (shoveable != null)
            shoveable.OnShoved(transform, shoveForce);

        return rb != null || shoveable != null;
    }

    private Transform GetTargetRoot(Collider hitCollider)
    {
        if (hitCollider == null)
            return null;

        if (hitCollider.attachedRigidbody != null)
            return hitCollider.attachedRigidbody.transform;

        ShoveableTarget shoveable = hitCollider.GetComponentInParent<ShoveableTarget>();

        if (shoveable != null)
            return shoveable.transform;

        return hitCollider.transform.root;
    }

    private bool IsValidTarget(Transform targetRoot, Collider hitCollider)
    {
        if (targetRoot == null || hitCollider == null)
            return false;

        bool isNpc =
            affectNPCs &&
            (
                targetRoot.CompareTag(npcTag) ||
                hitCollider.CompareTag(npcTag)
            );

        bool isEnemy =
            affectEnemies &&
            (
                targetRoot.CompareTag(enemyTag) ||
                hitCollider.CompareTag(enemyTag)
            );

        return isNpc || isEnemy;
    }

    private void AddShoveAnxiety()
    {
        if (_playerAnxiety == null)
            return;

        if (anxietyIncreasePerShove <= 0f)
            return;

        _playerAnxiety.AddAnxiety(anxietyIncreasePerShove);

        if (logShoves)
            Debug.Log($"[PlayerShove] Added anxiety: {anxietyIncreasePerShove}", this);
    }

    private void SpawnShoveVFX()
    {
        if (shoveVfxPrefab == null)
            return;

        Transform spawnPoint = shoveVfxSpawnPoint != null
            ? shoveVfxSpawnPoint
            : shoveOrigin;

        if (spawnPoint == null)
            spawnPoint = transform;

        Vector3 spawnPos =
            spawnPoint.position + spawnPoint.TransformDirection(shoveVfxOffset);

        Quaternion spawnRot =
            spawnPoint.rotation * Quaternion.Euler(shoveVfxRotationEuler);

        GameObject vfx = Instantiate(shoveVfxPrefab, spawnPos, spawnRot);

        ParticleSystem[] particleSystems =
            vfx.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem ps in particleSystems)
        {
            if (ps == null)
                continue;

            ps.Clear(true);
            ps.Play(true);
        }

        StartCoroutine(SmoothStopAndDestroyShoveVFX(vfx, particleSystems));
    }

    private IEnumerator SmoothStopAndDestroyShoveVFX(GameObject vfx, ParticleSystem[] particleSystems)
    {
        if (vfx == null)
            yield break;

        float activeEmitTime = Mathf.Max(0.01f, shoveVfxLifetime);

        yield return new WaitForSeconds(activeEmitTime);

        if (vfx == null)
            yield break;

        foreach (ParticleSystem ps in particleSystems)
        {
            if (ps == null)
                continue;

            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        float maxRemainingLifetime = 0.25f;

        foreach (ParticleSystem ps in particleSystems)
        {
            if (ps == null)
                continue;

            ParticleSystem.MainModule main = ps.main;
            maxRemainingLifetime = Mathf.Max(maxRemainingLifetime, main.startLifetime.constantMax);
        }

        yield return new WaitForSeconds(maxRemainingLifetime + shoveVfxExtraFadeTime);

        if (vfx != null)
            Destroy(vfx);
    }

    private void PlayShoveSFX()
    {
        if (shoveSfx == null)
            return;

        if (shoveSfxVolume <= 0f)
            return;

        if (shoveAudioSource != null)
        {
            shoveAudioSource.PlayOneShot(shoveSfx, shoveSfxVolume);
        }
        else
        {
            AudioSource.PlayClipAtPoint(shoveSfx, transform.position, shoveSfxVolume);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
            return;

        Transform originTransform = shoveOrigin != null ? shoveOrigin : transform;

        Vector3 origin = originTransform.position;
        Vector3 center = origin + originTransform.forward.normalized * shoveRange;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, shoveRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(origin, center);
    }
}