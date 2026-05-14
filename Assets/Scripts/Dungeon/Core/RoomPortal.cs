using System.Collections;
using UnityEngine;

public class RoomPortal : MonoBehaviour
{
    [Header("Portal Settings")]
    public Vector3 destinationPosition;
    public Room destinationRoom; // Nueva referencia a la sala lógica
    public UIMinimap uiMinimap;  // Nueva referencia al minimapa
    public Test12 test12;
    public float teleportCooldown = 0.5f;
    

    void Start()
    {
        test12 = FindAnyObjectByType<Test12>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Asegúrate de que tu prefab del jugador tenga la etiqueta (Tag) "Player"
        if (other.CompareTag("Player") && test12.enableTeleport)
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

        if (destinationRoom != null && uiMinimap != null)
        {
            // Marcamos como visitada y actualizamos la UI
            uiMinimap.RevealRoom(destinationRoom);
        }

        if (destinationRoom != null && test12 != null)
        {
            test12.HandlePlayerTeleported(player.transform, destinationRoom);
        }

        test12.enableTeleport = false;
        StartCoroutine(ReenableTeleportAfterDelay());

    }

    private IEnumerator ReenableTeleportAfterDelay()
    {
        yield return new WaitForSeconds(teleportCooldown);

        if (test12 != null)
        {
            test12.enableTeleport = true;
        }
    }
}
