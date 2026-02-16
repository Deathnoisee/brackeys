using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class inputSystem : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Mouse Look Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 80f;
    [SerializeField] private Transform cameraTransform;

    // Private variables
    private CharacterController controller;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private Vector3 velocity;
    private float currentSpeed;
    private bool isSprinting;
    private bool isGrounded;
    private float cameraPitch = 0f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        // Lock and hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // If camera not assigned, try to find main camera
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main?.transform;
        }
    }

    private void Update()
    {
        HandleMovement();
        HandleMouseLook();
        ApplyGravity();
    }

    #region Input System Event Callbacks

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            isSprinting = true;
        }
        else if (context.canceled)
        {
            isSprinting = false;
        }
    }

    #endregion

    private void HandleMovement()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;

        currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

        controller.Move(move * currentSpeed * Time.deltaTime);
    }

    private void HandleMouseLook()
    {
        if (cameraTransform == null) return;

        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        transform.Rotate(Vector3.up * mouseX);

        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -maxLookAngle, maxLookAngle);

        cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    private void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
