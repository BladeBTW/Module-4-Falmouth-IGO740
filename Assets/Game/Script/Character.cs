using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Character : MonoBehaviour
{
    private CharacterController _cc;
    private PlayerInput _playerInput;
    private Animator _animator;
    private PlayerAnxiety _playerAnxiety;

    [Header("Movement")]
    public float MoveSpeed = 8f;

    [Tooltip("How quickly the character visually turns toward movement direction.")]
    public float turnSpeed = 20f;

    [Header("Anxiety Movement")]
    public bool changeSpeedDuringAnxiety = true;

    [Tooltip("1 = same speed. 0.75 = slower. 1.2 = faster.")]
    public float anxietyMoveSpeedMultiplier = 0.75f;

    [Tooltip("Optional. If empty, this script searches for PlayerAnxiety on the same GameObject.")]
    public PlayerAnxiety playerAnxiety;

    [Header("Animator")]
    public string speedFloatParam = "Speed";
    public string airborneBoolParam = "AirBorne";

    [Tooltip("Smooths the Animator Speed value. Lower = snappier, higher = smoother.")]
    public float animatorSpeedDampTime = 0.05f;

    [Tooltip("Input below this value counts as idle.")]
    public float moveInputDeadzone = 0.05f;

    [Header("Isometric Direction")]
    public bool useIsometricRotation = true;
    public float isometricYawOffset = -45f;

    [Header("Top Down Lock")]
    public bool lockYPosition = false;
    public float allowedYDrift = 0.03f;

    private float _lockedY;
    private float _currentAnimatorSpeed;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _playerInput = GetComponent<PlayerInput>();
        _animator = GetComponentInChildren<Animator>();

        _playerAnxiety = playerAnxiety;

        if (_playerAnxiety == null)
            _playerAnxiety = GetComponent<PlayerAnxiety>();

        _lockedY = transform.position.y;

        if (_cc != null)
        {
            _cc.stepOffset = 0f;
            _cc.minMoveDistance = 0f;
        }
    }

    private void Update()
    {
        if (_cc == null || _playerInput == null)
            return;

        Vector2 rawInput = new Vector2(
            _playerInput.HorizontalInput,
            _playerInput.VerticalInput
        );

        float inputAmount = rawInput.magnitude;

        if (inputAmount < moveInputDeadzone)
            inputAmount = 0f;

        inputAmount = Mathf.Clamp01(inputAmount);

        Vector3 moveDirection = Vector3.zero;

        if (inputAmount > 0f)
        {
            Vector3 inputDirection = new Vector3(
                rawInput.x,
                0f,
                rawInput.y
            );

            inputDirection.Normalize();

            if (useIsometricRotation)
            {
                moveDirection =
                    Quaternion.Euler(0f, isometricYawOffset, 0f) *
                    inputDirection;
            }
            else
            {
                moveDirection = inputDirection;
            }

            moveDirection.y = 0f;
            moveDirection.Normalize();

            RotateToward(moveDirection);
        }

        float currentMoveSpeed = GetCurrentMoveSpeed();

        Vector3 move = moveDirection * currentMoveSpeed * Time.deltaTime;
        move.y = 0f;

        _cc.Move(move);

        LockYOnlyIfReallyNeeded();
        UpdateAnimator(inputAmount);
    }

    private float GetCurrentMoveSpeed()
    {
        if (!changeSpeedDuringAnxiety)
            return MoveSpeed;

        if (_playerAnxiety == null)
            return MoveSpeed;

        if (_playerAnxiety.IsAnimatorAnxietyActive())
            return MoveSpeed * anxietyMoveSpeedMultiplier;

        return MoveSpeed;
    }

    private void RotateToward(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * turnSpeed
        );
    }

    private void UpdateAnimator(float inputAmount)
    {
        if (_animator == null)
            return;

        _currentAnimatorSpeed = Mathf.MoveTowards(
            _currentAnimatorSpeed,
            inputAmount,
            Time.deltaTime / Mathf.Max(0.0001f, animatorSpeedDampTime)
        );

        if (!string.IsNullOrEmpty(speedFloatParam))
        {
            _animator.SetFloat(
                speedFloatParam,
                _currentAnimatorSpeed
            );
        }

        if (!string.IsNullOrEmpty(airborneBoolParam))
        {
            _animator.SetBool(
                airborneBoolParam,
                false
            );
        }
    }

    private void LockYOnlyIfReallyNeeded()
    {
        if (!lockYPosition)
            return;

        float yDifference = _lockedY - transform.position.y;

        if (Mathf.Abs(yDifference) < allowedYDrift)
            return;

        _cc.Move(new Vector3(0f, yDifference, 0f));
    }
}