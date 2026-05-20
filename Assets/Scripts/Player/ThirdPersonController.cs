using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonController : MonoBehaviour
{
    [Header("Camera")]
    public Transform cameraPivot;
    public float mouseSensitivity = 20f;
    public float cameraDistance = 5f;

    [Header("Movement")]
    public float walkSpeed = 7f;
    public float runSpeed = 11f;

    [Header("Physics")]
    public Rigidbody rb;

    private float yaw;
    private float pitch;

    private Vector2 moveInput;
    private bool runInput;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        cameraPivot = GetComponentInChildren<Camera>().transform;
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // ===== INPUT =====
        Vector2 mouse = Mouse.current.delta.ReadValue();
        yaw += mouse.x * mouseSensitivity * Time.deltaTime;
        pitch -= mouse.y * mouseSensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, -70f, 70f);

        moveInput.x = Keyboard.current.dKey.isPressed ? 1 :
                     Keyboard.current.aKey.isPressed ? -1 : 0;

        moveInput.y = Keyboard.current.wKey.isPressed ? 1 :
                     Keyboard.current.sKey.isPressed ? -1 : 0;

        runInput = Keyboard.current.leftShiftKey.isPressed;

        // rotación jugador
        transform.rotation = Quaternion.Euler(0, yaw, 0);

        UpdateCamera();
    }

    void FixedUpdate()
    {
        Move();
    }

    void Move()
    {
        float speed = runInput ? runSpeed : walkSpeed;

        Vector3 direction = (transform.forward * moveInput.y +
                             transform.right * moveInput.x).normalized;

        Vector3 targetVelocity = direction * speed;

        Vector3 currentVelocity = rb.linearVelocity;
        currentVelocity.y = rb.linearVelocity.y;

        rb.linearVelocity = new Vector3(
            targetVelocity.x,
            currentVelocity.y,
            targetVelocity.z
        );
    }


    void UpdateCamera()
    {
        if (cameraPivot == null) return;

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0);
        Vector3 offset = rot * new Vector3(0, 0, -cameraDistance);

        cameraPivot.position = transform.position + offset;
        cameraPivot.LookAt(transform.position + Vector3.up * 1.5f);
    }
}