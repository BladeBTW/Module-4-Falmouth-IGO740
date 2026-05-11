using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAutoShove : MonoBehaviour
{
    [Header("Detection")]
    public bool autoShoveEnabled = true;

    [Tooltip("How often auto shove can trigger.")]
    public float shoveCooldown = 0.35f;

    [Tooltip("Layers that can be auto-shoved.")]
    public LayerMask shoveLayers = ~0;

    [Header("Shove Force")]
    public float shoveForce = 4f;
    public float upwardForce = 0.35f;
    public ForceMode forceMode = ForceMode.VelocityChange;

    [Header("Anxiety")]
    [Tooltip("This amount is added instantly per successful auto-shove. No Time.deltaTime.")]
    public float anxietyIncreasePerAutoShove = 50f;

    [Tooltip("If true, anxiety is added once per auto-shove event.")]
    public bool addAnxietyOnlyOncePerAutoShove = true;

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

    [Tooltip("How long the auto-shove VFX emits before fading naturally.")]
    public float shoveVfxLifetime = 0.35f;

    [Tooltip("Extra time after particles stop emitting before the VFX object is destroyed.")]
    public float shoveVfxExtraFadeTime = 0.35f;

    public AudioClip shoveSfx;

    [Range(0f, 10f)]
    public float shoveSfxVolume = 1f;

    public AudioSource shoveAudioSource;

    [Header("Debug")]
    public bool logShoves = true;

    private float _nextShoveTime;
    private PlayerAnxiety _playerAnxiety;
    private readonly HashSet<Transform> _shovedRootsThisEvent = new HashSet<Transform>();

    private void Awake()
    {
        _playerAnxiety = GetComponent<PlayerAnxiety>();

        if (_playerAnxiety == null)
            _playerAnxiety = GetComponentInChildren<PlayerAnxiety>();
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!autoShoveEnabled)
            return;

        if (Time.time < _nextShoveTime)
            return;

        if (hit == null || hit.collider == null)
            return;

        TryAutoShove(hit.collider, hit.moveDirection);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!autoShoveEnabled)
            return;

        if (Time.time < _nextShoveTime)
            return;

        if (collision == null || collision.collider == null)
            return;

        Vector3 direction = collision.relativeVelocity;

        if (direction.sqrMagnitude < 0.001f)
            direction = transform.forward;

        TryAutoShove(collision.collider, direction);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!autoShoveEnabled)
            return;

        if (Time.time < _nextShoveTime)
            return;

        if (other == null)
            return;

        TryAutoShove(other, transform.forward);
    }

    private void TryAutoShove(Collider hitCollider, Vector3 shoveDirection)
    {
        if (hitCollider == null)
            return;

        if (hitCollider.transform == transform || hitCollider.transform.IsChildOf(transform))
            return;

        if (!IsLayerAllowed(hitCollider.gameObject.layer))
            return;

        Transform targetRoot = GetTargetRoot(hitCollider);

        if (targetRoot == null)
            return;

        if (!IsValidTarget(targetRoot, hitCollider))
            return;

        _nextShoveTime = Time.time + shoveCooldown;
        _shovedRootsThisEvent.Clear();

        bool shoved = ShoveTarget(hitCollider, targetRoot, shoveDirection);

        if (!shoved)
            return;

        if (!_shovedRootsThisEvent.Contains(targetRoot))
            _shovedRootsThisEvent.Add(targetRoot);

        AddAutoShoveAnxiety();
        SpawnShoveVFX();
        PlayShoveSFX();

        if (logShoves)
        {
            Debug.Log(
                $"[PlayerAutoShove] Auto-shoved {targetRoot.name}. Anxiety +{anxietyIncreasePerAutoShove}",
                targetRoot
            );
        }
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

    private void AddAutoShoveAnxiety()
    {
        if (_playerAnxiety == null)
            return;

        if (anxietyIncreasePerAutoShove <= 0f)
            return;

        _playerAnxiety.AddAnxiety(anxietyIncreasePerAutoShove);
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

    private bool IsLayerAllowed(int layer)
    {
        return (shoveLayers.value & (1 << layer)) != 0;
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

    private void SpawnShoveVFX()
    {
        if (shoveVfxPrefab == null)
            return;

        Transform spawnPoint = shoveVfxSpawnPoint != null
            ? shoveVfxSpawnPoint
            : transform;

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
}