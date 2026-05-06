using UnityEngine;

public class Character : MonoBehaviour
{
    private CharacterController _cc;
    private PlayerInput _playerInput;
    private Animator _animator;

    public float MoveSpeed = 5f;
    public float Gravity = -20f;

    private Vector3 _movementVelocity;
    private float _verticalVelocity;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _playerInput = GetComponent<PlayerInput>();
        _animator = GetComponentInChildren<Animator>();

        if (_cc == null)
            Debug.LogError("CharacterController missing on Player.", this);

        if (_playerInput == null)
            Debug.LogError("PlayerInput missing on Player.", this);

        if (_animator == null)
            Debug.LogError("Animator missing on Player child Visual.", this);
    }

    private void Update()
    {
        CalculatePlayerMovement();
        ApplyGravityAndMove();
    }

    private void CalculatePlayerMovement()
    {
        if (_playerInput == null)
            return;

        Vector3 inputDirection = new Vector3(
            _playerInput.HorizontalInput,
            0f,
            _playerInput.VerticalInput
        );

        float inputMagnitude = Mathf.Clamp01(inputDirection.magnitude);

        if (inputMagnitude > 0.01f)
        {
            inputDirection.Normalize();

            Vector3 rotatedDirection = Quaternion.Euler(0f, -45f, 0f) * inputDirection;

            _movementVelocity = rotatedDirection * MoveSpeed;
            transform.rotation = Quaternion.LookRotation(rotatedDirection);
        }
        else
        {
            _movementVelocity = Vector3.zero;
        }

        if (_animator != null)
        {
            _animator.SetFloat("Speed", inputMagnitude);
            _animator.SetBool("AirBorne", !_cc.isGrounded);
        }
    }

    private void ApplyGravityAndMove()
    {
        if (_cc == null)
            return;

        if (_cc.isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = -2f;
        else
            _verticalVelocity += Gravity * Time.deltaTime;

        Vector3 finalMovement = _movementVelocity;
        finalMovement.y = _verticalVelocity;

        _cc.Move(finalMovement * Time.deltaTime);
    }
}