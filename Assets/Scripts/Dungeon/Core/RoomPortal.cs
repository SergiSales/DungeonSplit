using System.Collections;
using UnityEngine;

public class RoomPortal : MonoBehaviour
{
    public Vector3 destinationPosition;
    public Room destinationRoom; // Nueva referencia a la sala lógica
    public UIMinimap uiMinimap;  // Nueva referencia al minimapa
    public TestBase test;
    public float teleportCooldown = 0.5f;
    public CameraBehaviour cam;
    

    void Start()
    {
        test = FindAnyObjectByType<TestBase>();
        cam = Camera.main.GetComponent<CameraBehaviour>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Asegúrate de que tu prefab del jugador tenga la etiqueta (Tag) "Player"
        if (!other.CompareTag("Player"))
        {
            return;
        }

        // Si el jugador está en una sala de tipo Wave y aún no la ha limpiado, no permitir teletransportarse.
        Room currentRoom = GameManager.instance?.GetCurrentRoom();
        if (currentRoom != null && currentRoom.type == roomTypes.Wave && !currentRoom.cleared)
        {
            return;
        }

        if (test == null || test.enableTeleport)
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
            uiMinimap.setPlayerMinimap(destinationRoom);
        }

        if (destinationRoom != null && test != null)
        {
            test.HandlePlayerTeleported(player.transform, destinationRoom);
            
        }
        cam.Teleport(destinationPosition);
    }


}