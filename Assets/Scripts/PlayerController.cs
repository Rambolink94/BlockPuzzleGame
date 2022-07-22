using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float movementSpeed;
    [SerializeField] private float maxVelocity;
    [SerializeField] private float cameraSensitivity;
    [SerializeField] private float minLookAngle = -90f;
    [SerializeField] private float maxLookAngle = 90f;
    
    private Rigidbody _rigidbody;
    private Camera _camera;
    
    private Vector3 _movement;
    private float _xRotation;
    private float _yRotation;
    
    // Start is called before the first frame update
    private void Start()
    {
        // TODO: Make actually good FPS Camera
        _rigidbody = GetComponent<Rigidbody>();
        _camera = Camera.main;

        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetMouseButton(1))
            return;
        
        _movement = GetMovementInput();
        RotateCamera();
    }

    private Vector3 GetMovementInput()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        bool up = Input.GetKeyDown(KeyCode.Space);
        bool down = Input.GetKeyDown(KeyCode.C) | Input.GetKeyDown(KeyCode.LeftControl);

        float yMovement = up.GetHashCode() + down.GetHashCode() * -1;

        return transform.right * horizontal + transform.forward * vertical + Vector3.up * yMovement;
    }

    private void RotateCamera()
    {
        float mouseX = Input.GetAxis("Mouse X") * cameraSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * cameraSensitivity * Time.deltaTime;
        
        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, minLookAngle, maxLookAngle);

        _camera.transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    private void FixedUpdate()
    {
        _rigidbody.AddForce(_movement * movementSpeed);
    }
}
