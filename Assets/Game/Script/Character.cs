using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class Character : MonoBehaviour
{
    private CharacterController _cc;
    public float MoveSpeed = 5f;
    private Vector3 _movementVelocity;
    private PlayerInput _playerInput;
    private void Awake()
    {
        //called when instant of this script is loaded
        _cc = GetComponent<CharacterController>();
        _playerInput = GetComponent<PlayerInput>();
    }

    private void CalculatePlayerMovement()
    {
        _movementVelocity.Set(_playerInput.HorizontalInput,0f,_playerInput.VerticalInput);
        _movementVelocity.Normalize();
        _movementVelocity = Quaternion.Euler(0,-45f,0) * _movementVelocity;
        _movementVelocity *= MoveSpeed * Time.deltaTime;
    }

    private void FixedUpdate()
    {
        CalculatePlayerMovement();
        _cc.Move(_movementVelocity);
    }
}

