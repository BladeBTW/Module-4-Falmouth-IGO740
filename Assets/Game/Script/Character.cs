using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//temp

public class Character : MonoBehaviour
{
    private CharacterController _cc;
    public float MoveSpeed = 5f;
    private Vector3 _movementVelocity;
    private PlayerInput _playerInput;
    private float _verticalVelocity;
    public float Gravity = -9.8f;
    private Animator _animator;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();

        _playerInput = GetComponent<PlayerInput>();
        if (_playerInput == null)
            Debug.LogError("PlayerInput component missing on Player object!", this);
    }

    private void CalculatePlayerMovement()
    {
        if (_playerInput == null)
            return;

        // 1. Input direction
        _movementVelocity.Set(_playerInput.HorizontalInput, 0f, _playerInput.VerticalInput);
        _movementVelocity.Normalize();

        // 2. Rotate input 45 degrees (just like your original script)
        _movementVelocity = Quaternion.Euler(0, -45f, 0) * _movementVelocity;

        // 3. Animator SPEED uses normalized magnitude (0–1)
        _animator.SetFloat("Speed", _movementVelocity.magnitude);

        // 4. Apply movement speed afterwards for actual world movement
        _movementVelocity *= MoveSpeed * Time.deltaTime;

        // Rotate player toward movement direction
        if (_movementVelocity != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(_movementVelocity);

        // Use airborne animation same as original
        _animator.SetBool("AirBorne", !_cc.isGrounded);
    }

    private void FixedUpdate()
    {
        CalculatePlayerMovement();

        // Gravity (player only)
        if (_cc.isGrounded == false)
            _verticalVelocity = Gravity;
        else
            _verticalVelocity = Gravity * 0.3f;

        _movementVelocity += _verticalVelocity * Vector3.up * Time.deltaTime;

        // CharacterController final move
        _cc.Move(_movementVelocity);
    }
}
