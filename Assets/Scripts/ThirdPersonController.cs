using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonController : MonoBehaviour
{

    [Header("Camera Settings")]
    public Transform cameraPivot;
    public float cameraDistance = 5f;
    public float lookDownExtraDistance = 0.75f;
    public float minCameraHeight = 0.5f;
    public float maxCameraHeight = 3f;
    public float cameraCollisionRadius = 0.1f;

    [Header("Player Settings")]
    public float speed;
    public float walkSpeed = 7f;
    public float runSpeed = 11f;
    public bool jump;
    public float jumpForce = 5f;
    public float mouseSensitivity = 30f;
    public Rigidbody rb;

    public LayerMask collisionLayers;

    float yaw = 0f;
    float pitch = 0f;

    void Awake()
    {
        if (cameraPivot == null)
        {
            Camera childCamera = GetComponentInChildren<Camera>(true);
            if (childCamera != null)
            {
                cameraPivot = childCamera.transform;
            }
        }
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // ROTACION CON RATON 
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        float mouseX = mouseDelta.x * mouseSensitivity * Time.deltaTime;
        float mouseY = mouseDelta.y * mouseSensitivity * Time.deltaTime;

        // Actualizar ángulos de órbita
        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -70f, 70f);

        // Rotar el jugador según la cámara (yaw)
        transform.rotation = Quaternion.Euler(0, yaw, 0);

        // Posicionar la cámara orbitando alrededor del jugador con colisiones
        if (cameraPivot != null)
        {
            float lookDownFactor = Mathf.InverseLerp(0f, 70f, pitch);
            float currentCameraDistance = cameraDistance + lookDownFactor * lookDownExtraDistance;

            Vector3 offset = new Vector3(
                Mathf.Sin(yaw * Mathf.Deg2Rad) * Mathf.Cos(pitch * Mathf.Deg2Rad) * currentCameraDistance,
                Mathf.Sin(pitch * Mathf.Deg2Rad) * currentCameraDistance,
                Mathf.Cos(yaw * Mathf.Deg2Rad) * Mathf.Cos(pitch * Mathf.Deg2Rad) * currentCameraDistance
            );

            // Limitar altura de la cámara y ajustar pitch si alcanza el mínimo
            if (offset.y < minCameraHeight)
            {
                offset.y = minCameraHeight;
                // Limitar pitch para evitar que siga intentando bajar
                float minHeightRatio = Mathf.Clamp(minCameraHeight / currentCameraDistance, -1f, 1f);
                pitch = Mathf.Max(pitch, Mathf.Asin(minHeightRatio) * Mathf.Rad2Deg);
            }
            else if (offset.y > maxCameraHeight)
            {
                offset.y = maxCameraHeight;
            }

            Vector3 desiredCameraPos = transform.position + offset;
            Vector3 finalCameraPos = desiredCameraPos;

            // Raycast para detectar colisiones
            Vector3 directionToCamera = (desiredCameraPos - transform.position).normalized;
            float distanceToCamera = Vector3.Distance(transform.position, desiredCameraPos);

            if (Physics.Raycast(transform.position, directionToCamera, out RaycastHit hit, distanceToCamera, collisionLayers))
            {
                // Si hay colisión, acercar la cámara al punto de impacto
                finalCameraPos = transform.position + directionToCamera * (hit.distance - cameraCollisionRadius);
                finalCameraPos.y = Mathf.Clamp(finalCameraPos.y, minCameraHeight, transform.position.y + maxCameraHeight);
            }

            cameraPivot.position = finalCameraPos;
            cameraPivot.LookAt(transform.position);
        }

        // MOVIMIENTO 
        float h = Keyboard.current.dKey.isPressed ? 1f : (Keyboard.current.aKey.isPressed ? -1f : 0f);
        float v = Keyboard.current.wKey.isPressed ? 1f : (Keyboard.current.sKey.isPressed ? -1f : 0f);
        
        speed = Keyboard.current.leftShiftKey.isPressed ? runSpeed : walkSpeed;
        jump = Keyboard.current.spaceKey.wasPressedThisFrame ? true : false;

        Vector3 move = transform.forward * v + transform.right * h;
        transform.position -= move * speed * Time.deltaTime;

        

        if (jump)
        {
            if (rb != null && Mathf.Abs(rb.linearVelocity.y) < 0.01f)
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
        }
    }
}
