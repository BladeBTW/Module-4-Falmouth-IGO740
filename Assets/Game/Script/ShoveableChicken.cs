using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Rigidbody))]
public class ShoveableChicken : MonoBehaviour
{
    [Header("Shove")]
    public bool canBeShoved = true;
    public float shoveDuration = 0.18f;
    public float shoveSpeedMultiplier = 5f;
    public float recoveryTime = 0.01f;

    [Header("Ground Lock")]
    public bool lockYPositionDuringShove = true;

    [Header("Stability")]
    public bool lockRotationDuringShove = true;
    public bool faceShoveDirection = true;
    public float rotationSpeed = 12f;
    public bool keepUpright = true;

    [Header("Anxiety On Shove")]
    public float anxietyIncrease = 4f;

    [Header("Player VFX On Shove")]
    public GameObject playerShoveVfxPrefab;
    public Vector3 playerVfxLocalOffset = new Vector3(0f, 0.5f, 0f);
    public Vector3 playerVfxRotationEuler = Vector3.zero;
    public Vector3 playerVfxScale = Vector3.one;
    public float playerVfxLifetime = 1.5f;
    public bool parentVfxToPlayer = true;

    [Header("SFX On Shove")]
    public AudioClip shoveSfx;
    [Range(0f, 5f)] public float shoveSfxVolume = 1f;

    private Rigidbody rb;
    private NavMeshAgent agent;
    private Coroutine shoveRoutine;
    private RigidbodyConstraints originalConstraints;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();
        originalConstraints = rb.constraints;
    }

    public bool TryShove(Vector3 direction, float force, GameObject source)
    {
        return TryShove(direction, force, source, true);
    }

    public bool TryShove(Vector3 direction, float force, GameObject source, bool playFeedback)
    {
        if (!canBeShoved || rb == null)
            return false;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
            direction = transform.forward;

        direction.Normalize();

        if (playFeedback)
        {
            ApplyAnxiety(source);
            PlayPlayerShoveVFX(source);
            PlayShoveSFX();
        }

        if (shoveRoutine != null)
            StopCoroutine(shoveRoutine);

        shoveRoutine = StartCoroutine(ShoveRoutine(direction, force));
        return true;
    }

    private void ApplyAnxiety(GameObject source)
    {
        if (source == null)
            return;

        source.SendMessage(
            "AddAnxiety",
            anxietyIncrease,
            SendMessageOptions.DontRequireReceiver
        );
    }

    private void PlayPlayerShoveVFX(GameObject source)
    {
        if (playerShoveVfxPrefab == null || source == null)
            return;

        Transform playerTransform = source.transform;

        Vector3 position =
            playerTransform.position +
            playerTransform.TransformDirection(playerVfxLocalOffset);

        Quaternion rotation =
            playerTransform.rotation * Quaternion.Euler(playerVfxRotationEuler);

        Transform parent = parentVfxToPlayer ? playerTransform : null;

        GameObject vfx = Instantiate(playerShoveVfxPrefab, position, rotation, parent);
        vfx.transform.localScale = playerVfxScale;

        Destroy(vfx, playerVfxLifetime);
    }

    private void PlayShoveSFX()
    {
        if (shoveSfx == null)
            return;

        AudioSource.PlayClipAtPoint(shoveSfx, transform.position, shoveSfxVolume);
    }

    private IEnumerator ShoveRoutine(Vector3 direction, float force)
    {
        float lockedY = transform.position.y;

        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.enabled = false;
        }

        if (lockRotationDuringShove)
        {
            rb.constraints =
                originalConstraints |
                RigidbodyConstraints.FreezeRotationX |
                RigidbodyConstraints.FreezeRotationZ;
        }

        rb.angularVelocity = Vector3.zero;
        rb.velocity = Vector3.zero;

        Vector3 shoveVelocity =
            direction.normalized *
            force *
            shoveSpeedMultiplier;

        shoveVelocity.y = 0f;
        rb.velocity = shoveVelocity;

        float timer = 0f;

        while (timer < shoveDuration)
        {
            timer += Time.deltaTime;

            Vector3 currentVelocity = rb.velocity;
            currentVelocity.y = 0f;
            rb.velocity = currentVelocity;

            if (lockYPositionDuringShove)
            {
                Vector3 pos = transform.position;
                pos.y = lockedY;
                transform.position = pos;
            }

            if (keepUpright)
            {
                Vector3 euler = transform.eulerAngles;
                euler.x = 0f;
                euler.z = 0f;
                transform.eulerAngles = euler;
            }

            if (faceShoveDirection && direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation =
                    Quaternion.LookRotation(direction.normalized);

                transform.rotation =
                    Quaternion.Slerp(
                        transform.rotation,
                        targetRotation,
                        Time.deltaTime * rotationSpeed
                    );
            }

            yield return null;
        }

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (lockYPositionDuringShove)
        {
            Vector3 pos = transform.position;
            pos.y = lockedY;
            transform.position = pos;
        }

        yield return new WaitForSeconds(recoveryTime);

        rb.constraints = originalConstraints;

        if (agent != null)
        {
            agent.enabled = true;

            if (agent.isOnNavMesh)
                agent.isStopped = false;
        }

        shoveRoutine = null;
    }
}