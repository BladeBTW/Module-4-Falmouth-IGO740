using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerHardYLock : MonoBehaviour
{
    [Header("Hard Y Lock")]
    public bool lockY = true;

    [Tooltip("Only correct upward popping. This is safest for CharacterController movement.")]
    public bool onlyPreventGoingUp = true;

    [Tooltip("Capture the player's Y after Start, once the scene has settled.")]
    public bool lockToStartingY = true;

    public float manualLockedY = 0f;

    [Tooltip("How far the player may drift upward before being snapped back.")]
    public float allowedUpwardDrift = 0.03f;

    [Header("CharacterController Safety")]
    public CharacterController characterController;
    public bool forceStepOffsetZero = true;

    private float lockedY;
    private bool initialized;

    private void Awake()
    {
        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        ApplyControllerSettings();
    }

    private void Start()
    {
        lockedY = lockToStartingY ? transform.position.y : manualLockedY;
        initialized = true;
    }

    private void LateUpdate()
    {
        if (!lockY || !initialized)
            return;

        ApplyControllerSettings();

        Vector3 pos = transform.position;

        if (onlyPreventGoingUp)
        {
            if (pos.y <= lockedY + allowedUpwardDrift)
                return;
        }
        else
        {
            if (Mathf.Abs(pos.y - lockedY) <= allowedUpwardDrift)
                return;
        }

        pos.y = lockedY;

        // Temporarily disabling the controller for one frame avoids it fighting the snap.
        if (characterController != null)
            characterController.enabled = false;

        transform.position = pos;

        if (characterController != null)
            characterController.enabled = true;
    }

    private void ApplyControllerSettings()
    {
        if (characterController == null)
            return;

        if (forceStepOffsetZero)
            characterController.stepOffset = 0f;

        characterController.minMoveDistance = 0f;
    }
}