using System.Collections;
using UnityEngine;

public class RoomPortal : MonoBehaviour
{
    [Header("Portal Settings")]
    public Vector3 destinationPosition;
    public Room destinationRoom; // Nueva referencia a la sala lógica
    public UIMinimap uiMinimap;  // Nueva referencia al minimapa
    public TestBase test;
    public float teleportCooldown = 0.5f;
    

    void Start()
    {
        test = FindAnyObjectByType<TestBase>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Asegúrate de que tu prefab del jugador tenga la etiqueta (Tag) "Player"
        if (other.CompareTag("Player") && (test == null || test.enableTeleport))
        {
            TeleportPlayer(other.gameObject);
        }
    }

    private void TeleportPlayer(GameObject player)
    {
        // 1. Desactivamos el Rigidbody temporalmente para evitar problemas de físicas al moverlo de golpe
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 2. Calculamos dónde aparece (un poco hacia adelante desde el centro de la sala destino)
        Vector3 offset = new Vector3(0f, 1f, 0f); // Puedes ajustar este valor para posicionar el jugador de forma diferente
        player.transform.position = destinationPosition + offset;
        
        // Enfocamos la cámara en la sala de destino con sus límites
        if (destinationRoom != null)
        {
            RoomCameraController.FocusRoom(destinationPosition, destinationRoom.bounds);
        }
        else
        {
            RoomCameraController.FocusRoom(destinationPosition);
        }

        if (destinationRoom != null && uiMinimap != null)
        {
            // Marcamos como visitada y actualizamos la UI
            uiMinimap.RevealRoom(destinationRoom);
        }

        if (destinationRoom != null && test != null)
        {
            test.HandlePlayerTeleported(player.transform, destinationRoom);
        }

        if (test != null)
        {
            test.SetTeleportCooldown(true);
            StartCoroutine(ReenableTeleportAfterDelay());
        }

    }

    private IEnumerator ReenableTeleportAfterDelay()
    {
        yield return new WaitForSeconds(teleportCooldown);

        if (test != null)
        {
            test.SetTeleportCooldown(false);
        }
    }
}

[DisallowMultipleComponent]
public sealed class RoomCameraController : MonoBehaviour
{
    private static RoomCameraController instance;

    [SerializeField] private float roomHeight = 80f;
    [SerializeField] private bool preserveInitialRotation = true;
    [SerializeField] private float minFieldOfView = 30f;
    [SerializeField] private float maxFieldOfView = 90f;

    private Quaternion initialRotation;
    private bool rotationCached;
    private Camera mainCamera;
    private IntRect currentRoomBounds;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            enabled = false;
            return;
        }

        instance = this;
        CacheInitialRotation();
    }

    /// <summary>
    /// Enfoca la cámara en una sala, ajustando el FOV para ver toda la sala sin ver fuera de ella.
    /// </summary>
    public static void FocusRoom(Vector3 roomCenter, IntRect roomBounds)
    {
        RoomCameraController controller = EnsureInstance();
        if (controller == null)
        {
            Debug.LogWarning("[RoomCameraController] No camera was found to focus the current room.");
            return;
        }

        controller.SnapToRoomCenter(roomCenter, roomBounds);
    }

    /// <summary>
    /// Versión legacy que solo recibe el centro (para compatibilidad hacia atrás).
    /// </summary>
    public static void FocusRoom(Vector3 roomCenter)
    {
        RoomCameraController controller = EnsureInstance();
        if (controller == null)
        {
            Debug.LogWarning("[RoomCameraController] No camera was found to focus the current room.");
            return;
        }

        // Crea un bounds predeterminado (se usará si no se proporciona uno específico)
        controller.SnapToRoomCenter(roomCenter);
    }

    private static RoomCameraController EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        Camera targetCamera = Camera.main;
        if (targetCamera == null)
        {
            targetCamera = FindAnyObjectByType<Camera>();
        }

        if (targetCamera == null)
        {
            return null;
        }

        instance = targetCamera.GetComponent<RoomCameraController>();
        if (instance == null)
        {
            instance = targetCamera.gameObject.AddComponent<RoomCameraController>();
        }

        instance.CacheInitialRotation();
        return instance;
    }

    /// <summary>
    /// Posiciona la cámara en el centro de la sala y ajusta el FOV para ver toda la sala.
    /// </summary>
    private void SnapToRoomCenter(Vector3 roomCenter, IntRect roomBounds)
    {
        mainCamera = GetComponent<Camera>();
        if (mainCamera == null)
        {
            mainCamera = GetComponentInParent<Camera>();
        }

        currentRoomBounds = roomBounds;
        Vector3 flatRoomCenter = new Vector3(roomCenter.x, 0f, roomCenter.z);
        transform.position = flatRoomCenter + Vector3.up * roomHeight;

        // Calcular el FOV necesario para ver toda la sala
        CalculateAndApplyFieldOfView(roomBounds, roomHeight);

        if (preserveInitialRotation)
        {
            transform.rotation = initialRotation;
        }
    }

    /// <summary>
    /// Posiciona la cámara sin bounds específicos (legacy).
    /// </summary>
    private void SnapToRoomCenter(Vector3 roomCenter)
    {
        mainCamera = GetComponent<Camera>();
        if (mainCamera == null)
        {
            mainCamera = GetComponentInParent<Camera>();
        }

        Vector3 flatRoomCenter = new Vector3(roomCenter.x, 0f, roomCenter.z);
        transform.position = flatRoomCenter + Vector3.up * roomHeight;

        if (preserveInitialRotation)
        {
            transform.rotation = initialRotation;
        }
    }

    /// <summary>
    /// Calcula el FOV necesario para que la cámara vea toda la sala desde su altura actual.
    /// </summary>
    private void CalculateAndApplyFieldOfView(IntRect roomBounds, float cameraHeight)
    {
        if (mainCamera == null)
        {
            return;
        }

        // Calcular la mitad de la diagonal de la sala
        float roomWidth = roomBounds.width;
        float roomDepth = roomBounds.height; // En términos de Z
        float halfDiagonal = Mathf.Sqrt(roomWidth * roomWidth + roomDepth * roomDepth) * 0.5f;

        // Calcular el ángulo necesario para ver desde la cámara hasta la esquina de la sala
        // tan(FOV/2) = halfDiagonal / cameraHeight
        float angleRad = Mathf.Atan2(halfDiagonal, cameraHeight);
        float requiredFOV = angleRad * 2f * Mathf.Rad2Deg;

        // Agregar un pequeño margen para asegurar que toda la sala sea visible
        requiredFOV *= 1.05f;

        // Limitar el FOV entre los valores mínimo y máximo
        float finalFOV = Mathf.Clamp(requiredFOV, minFieldOfView, maxFieldOfView);
        mainCamera.fieldOfView = finalFOV;

        Debug.Log($"[RoomCameraController] Sala ajustada. Dimensiones: {roomWidth}x{roomDepth}, FOV calculado: {requiredFOV:F2}°, FOV final: {finalFOV:F2}°");
    }

    private void CacheInitialRotation()
    {
        if (rotationCached)
        {
            return;
        }

        initialRotation = transform.rotation;
        rotationCached = true;
    }
}
