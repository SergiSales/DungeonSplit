using UnityEngine;
using UnityEngine.InputSystem;
public class TreasureDetector : MonoBehaviour
{
    [Header("Settings")]
    public float detectionRadius = 5f;
    private GameObject textObject;
    private PlayerStats playerStats;
    private Treasure closestChest;

    void Start()
    {
        playerStats = GetComponent<PlayerStats>();

        // Buscamos la UI una sola vez desde el jugador
        GameObject uiParent = GameObject.Find("UI-Continua");
        if (uiParent != null)
        {
            Transform childTransform = uiParent.transform.Find("TextInteraccion");
            if (childTransform != null)
            {
                textObject = childTransform.gameObject;
                textObject.SetActive(false);
            }
        }
    }

    void Update()
    {
        FindClosestChest();

        // Si hay un cofre cerca y no está abierto...
        if (closestChest != null && !closestChest.chestOpened)
        {
            if (textObject != null && !textObject.activeSelf) 
                textObject.SetActive(true);

            // Si presiona la E, abrimos ESE cofre específico
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                closestChest.OpenChest(playerStats);
                if (textObject != null) textObject.SetActive(false);
            }
        }
        else
        {
            // Si no hay ningún cofre cerca, nos aseguramos de apagar el texto
            if (textObject != null && textObject.activeSelf) 
                textObject.SetActive(false);
        }
    }

    void FindClosestChest()
    {
        // Buscamos todos los objetos con el script Treasure en la escena
        Treasure[] allChests = FindObjectsByType<Treasure>();
        
        Treasure targetChest = null;
        float closestDistance = detectionRadius;

        foreach (Treasure chest in allChests)
        {
            if (chest.chestOpened) continue; // Ignoramos los que ya están abiertos

            float distance = Vector3.Distance(transform.position, chest.transform.position);
            
            // Si este cofre está más cerca que el rango de detección y más cerca que el anterior evaluado
            if (distance < closestDistance)
            {
                closestDistance = distance;
                targetChest = chest;
            }
        }

        // Guardamos el ganador de la evaluación
        closestChest = targetChest;
    }
}
