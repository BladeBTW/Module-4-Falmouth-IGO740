using UnityEngine;

public class PlayerAutoShove : MonoBehaviour
{
    [Header("Detection")]
    public LayerMask chickenLayers;
    public float shoveRadius = 0.75f;
    public float forwardOffset = 0.55f;
    public float minMoveInput = 0.2f;
    public float shoveCooldown = 0f;

    [Header("Shove")]
    public float shoveForce = 1.4f;
    public float anxietyIncrease = 4f;

    [Header("Chain Push")]
    public bool allowChainPush = true;
    public float chainRadius = 0.8f;
    public float forceLossPerChicken = 0.65f;
    public float minimumChainForce = 0.5f;
    public int maxChainPushes = 8;

    [Header("Anti Climb")]
    public bool forceStepOffsetZero = true;

    private PlayerInput input;
    private CharacterController characterController;
    private PlayerAnxiety playerAnxiety;

    private float nextShoveTime;

    private void Awake()
    {
        input = GetComponent<PlayerInput>();
        characterController = GetComponent<CharacterController>();
        playerAnxiety = GetComponent<PlayerAnxiety>();

        if (characterController != null && forceStepOffsetZero)
            characterController.stepOffset = 0f;
    }

    private void Update()
    {
        if (characterController != null && forceStepOffsetZero)
            characterController.stepOffset = 0f;

        if (Time.time < nextShoveTime)
            return;

        if (input == null)
            return;

        Vector2 moveInput = new Vector2(
            input.HorizontalInput,
            input.VerticalInput
        );

        if (moveInput.magnitude < minMoveInput)
            return;

        TryAutoShove();
    }

    private void TryAutoShove()
    {
        Vector3 checkPos = transform.position + transform.forward * forwardOffset;
        checkPos.y = transform.position.y;

        Collider[] hits = Physics.OverlapSphere(
            checkPos,
            shoveRadius,
            chickenLayers,
            QueryTriggerInteraction.Collide
        );

        foreach (Collider hit in hits)
        {
            ShoveableChicken chicken = hit.GetComponentInParent<ShoveableChicken>();

            if (chicken == null)
                continue;

            Vector3 shoveDir = chicken.transform.position - transform.position;
            shoveDir.y = 0f;

            if (shoveDir.sqrMagnitude < 0.01f)
                shoveDir = transform.forward;

            shoveDir.y = 0f;
            shoveDir.Normalize();

            bool shoved = chicken.TryShove(
                shoveDir,
                shoveForce,
                gameObject
            );

            if (!shoved)
                continue;

            if (allowChainPush)
            {
                ChainPushFrom(
                    chicken,
                    shoveDir,
                    shoveForce * forceLossPerChicken,
                    1
                );
            }

            AddShoveAnxiety();

            nextShoveTime = Time.time + shoveCooldown;
            break;
        }
    }

    private void AddShoveAnxiety()
    {
        if (anxietyIncrease <= 0f)
            return;

        if (playerAnxiety == null)
            playerAnxiety = GetComponent<PlayerAnxiety>();

        if (playerAnxiety != null)
        {
            playerAnxiety.AddAnxiety(anxietyIncrease);
            return;
        }

        SendMessage(
            "AddAnxiety",
            anxietyIncrease,
            SendMessageOptions.DontRequireReceiver
        );
    }

    private void ChainPushFrom(
        ShoveableChicken sourceChicken,
        Vector3 direction,
        float force,
        int depth)
    {
        if (sourceChicken == null)
            return;

        if (depth > maxChainPushes)
            return;

        if (force < minimumChainForce)
            return;

        direction.y = 0f;
        direction.Normalize();

        Vector3 checkPos =
            sourceChicken.transform.position +
            direction * chainRadius;

        checkPos.y = sourceChicken.transform.position.y;

        Collider[] hits = Physics.OverlapSphere(
            checkPos,
            chainRadius,
            chickenLayers,
            QueryTriggerInteraction.Collide
        );

        foreach (Collider hit in hits)
        {
            ShoveableChicken nextChicken =
                hit.GetComponentInParent<ShoveableChicken>();

            if (nextChicken == null)
                continue;

            if (nextChicken == sourceChicken)
                continue;

            if (!nextChicken.canBeShoved)
                continue;

            Vector3 toNext =
                nextChicken.transform.position -
                sourceChicken.transform.position;

            toNext.y = 0f;

            if (toNext.sqrMagnitude < 0.01f)
                continue;

            float dot =
                Vector3.Dot(direction, toNext.normalized);

            if (dot < 0.35f)
                continue;

            bool shoved = nextChicken.TryShove(
                direction,
                force,
                gameObject,
                false
            );

            if (!shoved)
                continue;

            ChainPushFrom(
                nextChicken,
                direction,
                force * forceLossPerChicken,
                depth + 1
            );
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Vector3 checkPos = transform.position + transform.forward * forwardOffset;
        Gizmos.DrawWireSphere(checkPos, shoveRadius);
    }
}