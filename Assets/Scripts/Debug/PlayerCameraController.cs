using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCameraController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float lookSpeed = 0.1f;

    private PlayerInputActions inputActions;

    private Vector2 moveInput;
    private Vector2 lookInput;

    private float pitch;
    private float yaw;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void Update()
    {
        moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        lookInput = inputActions.Player.Look.ReadValue<Vector2>();

        Move();
        Look();
    }

    private void Move()
    {
        Vector3 move =
            transform.forward * moveInput.y +
            transform.right * moveInput.x;

        transform.position += move * moveSpeed * Time.deltaTime;
    }

    private void Look()
    {
        yaw += lookInput.x * lookSpeed;
        pitch -= lookInput.y * lookSpeed;

        pitch = Mathf.Clamp(pitch, -90f, 90f);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}