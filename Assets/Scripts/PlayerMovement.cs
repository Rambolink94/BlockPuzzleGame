using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerCam;
    [SerializeField] private Transform orientation;

    [Header("Control Sensitivity")]
    [SerializeField] private float sensitivity = 50f;
    [SerializeField] private float sensMultiplier = 1f;

    [Header("Movement")]
    [SerializeField] private float movementSpeed = 4500f;
    [SerializeField] private float maxSpeed = 20f;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private float maxSlopeAngle = 35f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 550f;

    private Rigidbody _rigidbody;
    private BlockController _blockController;
    
    private float _xRotation;
    private bool _grounded;

    // Jumping
    private bool _readyToJump = true;
    private float _jumpCooldown = 0.25f;
    private bool _jumping;
    
    // Input
    private float _xMovement;
    private float _yMovement;

    private Vector3 _normalVector;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _blockController = GetComponent<BlockController>();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void FixedUpdate()
    {
        Movement();
    }

    private void Update()
    {
        // Don't do movement if holding right mouse button
        if (_blockController.HasBlock) return;
        
        GetInput();
        Look();
    }

    private void GetInput()
    {
        _xMovement = Input.GetAxisRaw("Horizontal");
        _yMovement = Input.GetAxisRaw("Vertical");
        _jumping = Input.GetButton("Jump");
    }
    
    private void Movement()
    {
        // Extra Gravity
        _rigidbody.AddForce(Vector3.down * (Time.deltaTime * 10f));
        
        // Find velocity relative to player look direction
        Vector2 mag = FindVelRelativeToLook();
        float xMag = mag.x;
        float yMag = mag.y;

        if (_readyToJump && _jumping) Jump();

        if ((_xMovement > 0 && xMag > maxSpeed) || (_xMovement < 0 && xMag < -maxSpeed)) _xMovement = 0;
        if ((_yMovement > 0 && yMag > maxSpeed) || (_yMovement < 0 && yMag < -maxSpeed)) _yMovement = 0;

        float xMult = 1f;
        float yMult = 1f;

        if (!_grounded)
        {
            xMult = 0.5f;
            yMult = 0.5f;
        }
        
        // Move player
        _rigidbody.AddForce(orientation.forward * (_yMovement * movementSpeed * Time.deltaTime * xMult * yMult));
        _rigidbody.AddForce(orientation.right * (_xMovement * movementSpeed * Time.deltaTime * xMult));
    }

    private Vector2 FindVelRelativeToLook()
    {
        float lookAngle = orientation.eulerAngles.y;
        float moveAngle = Mathf.Atan2(_rigidbody.velocity.x, _rigidbody.velocity.z) * Mathf.Rad2Deg;

        float u = Mathf.DeltaAngle(lookAngle, moveAngle);
        float v = 90 - u;

        float mag = _rigidbody.velocity.magnitude;
        float yMag = mag * Mathf.Cos(u * Mathf.Deg2Rad);
        float xMag = mag * Mathf.Cos(v * Mathf.Deg2Rad);

        return new Vector2(xMag, yMag);
    }

    private void Jump()
    {
        if (_grounded && _readyToJump)
        {
            _readyToJump = false;
            
            // Add Jump Forces
            _rigidbody.AddForce(Vector3.up * (jumpForce * 1.5f));
            _rigidbody.AddForce(_normalVector * (jumpForce * 0.5f));
            
            // If jumping while falling, reset y velocity
            Vector3 vel = _rigidbody.velocity;
            if (_rigidbody.velocity.y < 0.5f)
                _rigidbody.velocity = new Vector3(vel.x, 0f, vel.z);
            else if (_rigidbody.velocity.y > 0f)
                _rigidbody.velocity = new Vector3(vel.x, vel.y / 2f, vel.z);
            
            Invoke(nameof(ResetJump), _jumpCooldown);
        }
    }

    private void ResetJump()
    {
        _readyToJump = true;
    }

    private float _desiredX;
    private void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.fixedDeltaTime * sensMultiplier;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.fixedDeltaTime * sensMultiplier;
        
        // Find current look rotation
        Vector3 rot = playerCam.localRotation.eulerAngles;
        _desiredX = rot.y + mouseX;
        
        // Rotate, and clamp rotation
        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);
        
        // Apply Rotations
        playerCam.localRotation = Quaternion.Euler(_xRotation, _desiredX, 0f);
        orientation.localRotation = Quaternion.Euler(0f, _desiredX, 0f);
    }

    private bool _cancellingGrounded;
    private void OnCollisionStay(Collision collision)
    {
        int layer = collision.gameObject.layer;
        if (whatIsGround != (whatIsGround | (1 << layer))) return;

        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.contacts[i].normal;

            if (IsFloor(normal))
            {
                _grounded = true;
                _cancellingGrounded = false;
                _normalVector = normal;
                CancelInvoke(nameof(StopGrounded));
            }
        }

        float delay = 3f;
        if (!_cancellingGrounded)
        {
            _cancellingGrounded = true;
            Invoke(nameof(StopGrounded), Time.deltaTime * delay);
        }
    }

    private bool IsFloor(Vector3 normal)
    {
        float angle = Vector3.Angle(Vector3.up, normal);
        return angle < maxSlopeAngle;
    }

    private void StopGrounded()
    {
        _grounded = false;
    }
}
