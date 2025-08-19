
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 12f;
    public float mouseSensitivity = 100f;
    public float jumpHeight = 3f;
    public float gravity = -9.81f;
    public Transform playerCamera;

    private CharacterController controller;
    private float xRotation = 0f;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool jumpPressed = false;
    private Vector3 playerVelocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>().transform;
        }
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (controller.isGrounded)
        {
            jumpPressed = true;
        }
    }

    void Update()
    {
        // Ground Check
        if (controller.isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f; // Keep the player grounded
        }

        // Player Movement
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(move * speed * Time.deltaTime);

        // Jumping
        if (jumpPressed)
        {
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpPressed = false;
        }

        // Gravity
        playerVelocity.y += gravity * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);

        // Mouse Look
        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }
}
