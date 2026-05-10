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

    [Tooltip("If true, VFX stays active while the player is touching.")]
    public bool keepVfxWhileTouching = true;

    public float oneShotVfxLifetime = 1.5f;

    private PlayerAnxiety currentPlayerAnxiety;
    private Transform currentPlayerTransform;
    private GameObject activeTouchVfx;

    private bool wasTouchingLastFrame;

    private void Update()
    {
        FindTouchingPlayer();

        bool isTouching = currentPlayerAnxiety != null;

        if (isTouching)
        {
            currentPlayerAnxiety.AddAnxietyOverTime(anxietyPerSecond);

            if (!wasTouchingLastFrame)
                SpawnOrEnableTouchVfx();
        }
        else
        {
            if (wasTouchingLastFrame)
                StopTouchVfx();
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
            bool isPlayer =
                hit.CompareTag(playerTag) ||
                hit.transform.root.CompareTag(playerTag);

            if (!isPlayer)
                continue;

            PlayerAnxiety anxiety =
                hit.GetComponent<PlayerAnxiety>();

            if (anxiety == null)
                anxiety = hit.GetComponentInParent<PlayerAnxiety>();

            if (anxiety == null)
                anxiety = hit.transform.root.GetComponent<PlayerAnxiety>();

            if (anxiety == null)
                continue;

            currentPlayerAnxiety = anxiety;
            currentPlayerTransform = anxiety.transform;
            return;
        }
    }

    private void SpawnOrEnableTouchVfx()
    {
        if (touchVfxPrefab == null)
            return;

        Transform parent = playerVfxSpawnPoint != null
            ? playerVfxSpawnPoint
            : currentPlayerTransform;

        if (parent == null)
            return;

        if (keepVfxWhileTouching)
        {
            if (activeTouchVfx == null)
            {
                activeTouchVfx = Instantiate(touchVfxPrefab, parent);

                activeTouchVfx.transform.localPosition = vfxLocalOffset;
                activeTouchVfx.transform.localRotation =
                    Quaternion.Euler(vfxLocalRotationEuler);
                activeTouchVfx.transform.localScale = vfxLocalScale;
            }

            activeTouchVfx.SetActive(true);
            PlayParticles(activeTouchVfx);
        }
        else
        {
            GameObject vfx = Instantiate(touchVfxPrefab, parent);

            vfx.transform.localPosition = vfxLocalOffset;
            vfx.transform.localRotation =
                Quaternion.Euler(vfxLocalRotationEuler);
            vfx.transform.localScale = vfxLocalScale;

            PlayParticles(vfx);

            if (oneShotVfxLifetime > 0f)
                Destroy(vfx, oneShotVfxLifetime);
        }
    }

    private void StopTouchVfx()
    {
        if (activeTouchVfx == null)
            return;

        if (keepVfxWhileTouching)
        {
            StopParticles(activeTouchVfx);
            activeTouchVfx.SetActive(false);
        }
        else
        {
            Destroy(activeTouchVfx);
            activeTouchVfx = null;
        }
    }

    private void PlayParticles(GameObject target)
    {
        if (target == null)
            return;

        ParticleSystem[] particles =
            target.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem ps in particles)
            ps.Play(true);
    }

    private void StopParticles(GameObject target)
    {
        if (target == null)
            return;

        ParticleSystem[] particles =
            target.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem ps in particles)
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    private void OnDisable()
    {
        if (activeTouchVfx != null)
            Destroy(activeTouchVfx);

        activeTouchVfx = null;
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