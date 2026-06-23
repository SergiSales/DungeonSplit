using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIMinimap : MonoBehaviour
{
    [Header("Referencias UI")]
    public RectTransform mapContent;
    public GameObject roomUIPrefab;
    public GameObject playerIconPrefab; // <--- Ahora pedimos un Prefab
    public TextMeshProUGUI TextInfo; // <--- Referencia al TextMeshProUGUI para mostrar información

    [Header("Referencias Jugador")]
    public Transform playerTransform; 

    [Header("Ajustes")]
    public float uiScale = 3f;

    // Variables internas
    private Dictionary<Room, GameObject> roomObjects = new Dictionary<Room, GameObject>();
    private Vector2 dungeonCenter;
    private float cellSize;
    private float spacing;
    private RectTransform playerIconInstance;
    public void Start()
    {
        TextInfo = GetComponentInChildren<TextMeshProUGUI>();
        TextInfo.text = "";
    }

    public void GenerateAbstractMap(List<Room> rooms, float cellSize, float spacing)
    {
        this.cellSize = cellSize;
        this.spacing = spacing;
        roomObjects.Clear();
        
        // 1. Limpiamos TODO el contenido (ahora sí destruimos todo, ya que el icono es un prefab)
        foreach (Transform child in mapContent)
        {
            Destroy(child.gameObject);
        }

        // 2. Calculamos el centro
        Vector2 minBounds = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 maxBounds = new Vector2(float.MinValue, float.MinValue);

        foreach (Room room in rooms)
        {
            if (room.bounds.x < minBounds.x) minBounds.x = room.bounds.x;
            if (room.bounds.y < minBounds.y) minBounds.y = room.bounds.y;
            if (room.bounds.x + room.bounds.width > maxBounds.x) maxBounds.x = room.bounds.x + room.bounds.width;
            if (room.bounds.y + room.bounds.height > maxBounds.y) maxBounds.y = room.bounds.y + room.bounds.height;
        }

        dungeonCenter = new Vector2((minBounds.x + maxBounds.x) / 2f, (minBounds.y + maxBounds.y) / 2f);

        // 3. Generamos las salas
        foreach (Room room in rooms)
        {
            GameObject roomUI = Instantiate(roomUIPrefab, mapContent);
            RectTransform rt = roomUI.GetComponent<RectTransform>();

            float uiX = (room.bounds.x - dungeonCenter.x) * uiScale;
            float uiY = (room.bounds.y - dungeonCenter.y) * uiScale; 

            rt.anchoredPosition = new Vector2(uiX, uiY);
            rt.sizeDelta = new Vector2(room.bounds.width * uiScale, room.bounds.height * uiScale);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0f, 0f); 
            switch (room.type)
            {
                case roomTypes.Player:
                    roomUI.GetComponent<Image>().color = Color.green;
                    break;
                case roomTypes.Boss:
                    roomUI.GetComponent<Image>().color = Color.red;
                    break;
                case roomTypes.Treasure:
                    roomUI.GetComponent<Image>().color = Color.yellow;
                    break;
                default:
                    roomUI.GetComponent<Image>().color = Color.white;
                    break;
                    
            }
            roomObjects.Add(room, roomUI);
            roomUI.SetActive(room.visited);
        }

        // 4. Instanciamos el icono del jugador
        if (playerIconPrefab != null)
        {
            GameObject iconObj = Instantiate(playerIconPrefab, mapContent);
            playerIconInstance = iconObj.GetComponent<RectTransform>();
            
            // Forzamos a que el icono sea el último hijo para que se dibuje SIEMPRE por encima de las salas
            playerIconInstance.SetAsLastSibling();
        }
    }

    public void RevealRoom(Room room)
    {
        if (roomObjects.ContainsKey(room))
        {
            room.visited = true;
            roomObjects[room].SetActive(true);
        }
    }

    void Update()
    {
        // 1. Si no tenemos al jugador asignado, lo buscamos automáticamente
        if (playerTransform == null)
        {
            ThirdPersonController player = FindAnyObjectByType<ThirdPersonController>();
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                // Si aún no ha spawneado el jugador, salimos del Update
                return; 
            }
        }

        // 2. Si el icono no se ha generado o faltan datos, salimos
        if (playerIconInstance == null || cellSize == 0 || spacing == 0) return;

        // 3. Calculamos la posición lógica
        float gridX = playerTransform.position.x / (cellSize * spacing);
        float gridY = playerTransform.position.z / (cellSize * spacing);

        // 4. Lo convertimos a píxeles de UI
        float uiX = (gridX - dungeonCenter.x) * uiScale;
        float uiY = (gridY - dungeonCenter.y) * uiScale;

        // 5. Aplicamos la posición y la rotación al icono generado
        playerIconInstance.anchoredPosition = new Vector2(uiX, uiY);
        playerIconInstance.localRotation = Quaternion.Euler(0, 0, playerTransform.eulerAngles.y);
    }

    
}