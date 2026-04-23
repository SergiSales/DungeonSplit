using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class Test11 : MonoBehaviour
{
    #region Inspector Variables
    [Header("Dungeon Settings")]
    public int dungeonWidth = 100;
    public int dungeonHeight = 100;
    public int minRoomSize = 14;
    public int maxRoomSize = 20;
    public int seed;

    [Header("Perlin Noise Settings")]
    public bool usePerlinNoise = true;
    public float perlinScale = 0.05f;
    public float densityThreshold = 0.4f;

    [Header("Loop Generation Settings")]
    [Range(0f, 1f)] public float loopQualityBias = 0.7f;
    [Range(0f, 1f)] public float randomnessFactor = 0.2f;
    [Range(1f, 10f)] public float minGraphDistanceThreshold = 2f;

    [Header("Connectivity")]
    [Range(0f, 1f)] public float extraConnectionFactor = 0.35f;

    [Header("Dead Ends Control")]
    [Range(0f, 1f)] public float deadEndKeepChance = 0.85f;
    [Range(0f, 1f)] public float deadEndConnectChance = 0.15f;

    [Header("Gizmo/Map Settings")]

    public bool showBSPNodes = true;
    public bool showDensityMap = false;
    public bool showRooms = true;
    public bool showDelaunay = true;
    public bool showMST = true;

    [Header("Room Spawning")]
    public GameObject[] roomPrefabs;
    public float cellSize = 10f;
    public float roomHeight = 0f;
    public Transform roomsParent;

    [Header("Wall Spawning")]
    public GameObject wallPrefab;
    public float wallHeight = 3f;
    public float wallThickness = 0.5f;

    [Header("Floor Objects")]
    public GameObject[] floorObjectsPrefabs;
    [Range(0f, 1f)] public float floorObjectsSpawnChance = 0.3f;

    [Header("Wall Objects")]
    public GameObject[] wallObjectsPrefabs;
    [Range(0f, 1f)] public float wallObjectsSpawnChance = 0.2f;

    [Header("Player Spawn")]
    public GameObject playerPrefab;
    public float playerSpawnHeight = 1f;

    [Header("Debug Stats")]
    public int generatedRoomCount;

    #endregion

    #region Private Variables
    // Generation data
    private BSPNode root;
    private List<Room> rooms = new List<Room>();
    private List<MSTEdge> mstEdges = new List<MSTEdge>();
    private List<MSTEdge> delaunayEdges = new List<MSTEdge>();
    private List<DelaunayTriangle> delaunayTriangles = new List<DelaunayTriangle>();
    private float[,] densityMap;
    private bool bossRoomAssigned = false;
    private bool playerSpawned = false;

    // Generators
    private DensityMapGenerator densityGenerator;
    private DelaunayGenerator delaunayGenerator;
    private MSTGenerator mstGenerator;
    private DungeonVisualization visualization;
    #endregion
    void Start()
    {
        Stopwatch totalTimer = Stopwatch.StartNew();
        seed = Random.Range(0, 100000);
        InitializeGenerators();

        // Generacion BSP
        BSPGenerator bspGenerator = new BSPGenerator(minRoomSize, maxRoomSize, seed);
        root = bspGenerator.Generate(new IntRect(0, 0, dungeonWidth, dungeonHeight));
        rooms = bspGenerator.CreateRooms(root);

        // Generacion del mapa de densidad
        if (usePerlinNoise)
        {
            densityMap = densityGenerator.GeneratePerlinNoise(perlinScale);
        }

        // Filtrado por densidad
        if (usePerlinNoise && densityMap != null)
        {
            densityGenerator.FilterRoomsByDensity(rooms, densityMap, densityThreshold);
        }

        generatedRoomCount = rooms.Count;

        if (rooms.Count > 1)
        {
            // Triangulacion Delaunay
            List<Vector2Int> points = new List<Vector2Int>();
            foreach (Room room in rooms)
            {
                points.Add(room.center);
            }

            delaunayTriangles = delaunayGenerator.GenerateTriangulation(points);
            delaunayEdges = delaunayGenerator.ExtractEdgesFromTriangles(delaunayTriangles);

            if (delaunayEdges.Count == 0 && points.Count > 1)
            {
                for (int i = 0; i < points.Count; i++)
                {
                    for (int j = i + 1; j < points.Count; j++)
                    {
                        delaunayEdges.Add(new MSTEdge(points[i], points[j]));
                    }
                }
            }

            delaunayEdges.Sort((a, b) => a.distance.CompareTo(b.distance));

            // Arbol de expansion minima
            mstEdges = mstGenerator.GenerateMST(delaunayEdges, rooms);
            mstGenerator.ControlDeadEnds(mstEdges, delaunayEdges, deadEndKeepChance, deadEndConnectChance);
            int targetExtraEdges = Mathf.RoundToInt(delaunayEdges.Count * extraConnectionFactor);
            mstGenerator.AddCyclesToMST(
                mstEdges,
                delaunayEdges,
                loopQualityBias,
                randomnessFactor,
                minGraphDistanceThreshold,
                targetExtraEdges);

        }

        SpawnRooms();
        AssignRoomTypes();


        

        totalTimer.Stop();
        UnityEngine.Debug.Log($"[Test11] Generated rooms: {rooms.Count}");
        UnityEngine.Debug.Log($"[Test11] Total generation time: {totalTimer.ElapsedMilliseconds}ms");
    }

    void OnDrawGizmos()
    {
        if (root == null || visualization == null)
        {
            return;
        }

        if (showBSPNodes)
        {
            visualization.DrawNodeGizmos(root);
        }

        if (showRooms)
        {
            visualization.DrawRoomsGizmos(rooms);
        }

        if (showDensityMap && densityMap != null)
        {
            visualization.DrawDensityMapGizmos(densityMap, dungeonWidth, dungeonHeight, densityThreshold);
        }

        if (delaunayTriangles != null && showDelaunay)
        {
            visualization.DrawDelaunayGizmos(delaunayTriangles);
        }

        if (mstEdges != null && showMST)
        {
            visualization.DrawMSTGizmos(mstEdges);
        }
    }

    private void InitializeGenerators()
    {
        densityGenerator = new DensityMapGenerator(dungeonWidth, dungeonHeight, seed);
        delaunayGenerator = new DelaunayGenerator();
        mstGenerator = new MSTGenerator();
        visualization = new DungeonVisualization();
    }

    string[] SpawnRooms()
    {
        if (roomPrefabs == null || roomPrefabs.Length == 0)
        {
            UnityEngine.Debug.LogWarning("[Test11] No room prefabs assigned.");
            return new string[] { "[Test11] No room prefabs assigned." };
        }

        Transform parent = roomsParent != null ? roomsParent : transform;
        int roomIndex = 1;

        string[] names = new string[rooms.Count];

        foreach (Room room in rooms)
        {
            names[roomIndex - 1] = SpawnRoom(room, parent, roomIndex);
            roomIndex++;
        }
        return names;
    }

    string SpawnRoom(Room room, Transform parent, int roomIndex)
    {
        Vector3 worldPos = GridToWorld(room.center);
        GameObject prefab = roomPrefabs[Random.Range(0, roomPrefabs.Length)];
        GameObject instance = Instantiate(prefab, worldPos, Quaternion.identity, parent);
        instance.name = $"suelo{roomIndex}";
        ScaleRoom(instance.transform, room);

        // Crear paredes alrededor de la sala
        CreateWalls(instance.transform, room, roomIndex);

        // Crear objetos en el suelo
        SpawnFloorObjects(instance.transform, room, roomIndex);

        // Crear objetos en las paredes
        SpawnWallObjects(instance.transform, room, roomIndex);

        return instance.name;
    }

    Vector3 GridToWorld(Vector2Int gridPos)
    {
        return GridToWorld((Vector2)gridPos, roomHeight);
    }

    Vector3 GridToWorld(Vector2 gridPos, float y)
    {
        return new Vector3(
            gridPos.x * cellSize * 5f,
            y,
            gridPos.y * cellSize * 5f
        );
    }

    void SpawnPlayer(int roomIndex)
    {
        Room spawnRoom = rooms[roomIndex];
        Vector3 spawnPosition = GridToWorld(spawnRoom.center) + Vector3.up * playerSpawnHeight;

        ThirdPersonController player = FindAnyObjectByType<ThirdPersonController>();
        if (player == null)
        {
            if (playerPrefab == null)
            {
                UnityEngine.Debug.LogWarning("[Test11] No player found in scene and no player prefab assigned.");
                return;
            }

            GameObject playerInstance = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
            player = playerInstance.GetComponent<ThirdPersonController>();
        }

        if (player == null)
        {
            UnityEngine.Debug.LogWarning("[Test11] The spawned player prefab does not have a ThirdPersonController.");
            return;
        }

        player.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);

        Rigidbody playerRigidbody = player.GetComponent<Rigidbody>();
        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        DisableOtherCameras(player.transform);
    }

    void AssignRoomTypes()
    {
        if (rooms == null || rooms.Count == 0)
        {
            UnityEngine.Debug.LogWarning("[Test11] Cannot assign room types because no rooms were generated.");
            return;
        }

        for (int i = 0; i < rooms.Count; i++)
        {
            Random.Range(0f, 1f);
            Room room = rooms[i];
            if (!bossRoomAssigned && Random.value < 0.1f)
            {
                room.type = roomTypes.Boss;
                bossRoomAssigned = true;
            }
            else if (!playerSpawned && Random.value < 0.2f)
            {
                room.type = roomTypes.Player;
                SpawnPlayer(i);
                playerSpawned = true;
            }
            else if (Random.value < 0.8f)
            {
                room.type = roomTypes.Wave;
            }
            else
            {
                room.type = roomTypes.Treasure;
            }
        }

        // Garantizar que siempre se asigne una sala de Boss si no se asignó
        if (!bossRoomAssigned && rooms.Count > 0)
        {
            int bossRoomIndex = Random.Range(0, rooms.Count);
            rooms[bossRoomIndex].type = roomTypes.Boss;
            bossRoomAssigned = true;
        }

        // Garantizar que siempre se asigne una sala de Player si no se asignó
        if (!playerSpawned && rooms.Count > 0)
        {
            int playerRoomIndex = Random.Range(0, rooms.Count);
            // Evitar asignar Player a la misma sala que Boss
            while (playerRoomIndex < rooms.Count && rooms[playerRoomIndex].type == roomTypes.Boss)
            {
                playerRoomIndex = (playerRoomIndex + 1) % rooms.Count;
            }
            rooms[playerRoomIndex].type = roomTypes.Player;
            playerSpawned = true;
            SpawnPlayer(playerRoomIndex);
        }
        
    }
    void DisableOtherCameras(Transform playerRoot)
    {
        Camera[] cameras = FindObjectsByType<Camera>();
        foreach (Camera camera in cameras)
        {
            bool belongsToPlayer = camera.transform.IsChildOf(playerRoot);
            camera.gameObject.SetActive(belongsToPlayer);
        }
    }

    void ScaleRoom(Transform roomTransform, Room room)
    {
        roomTransform.localScale = GetFootprintScale(room.bounds.width, room.bounds.height);
    }

    void CreateWalls(Transform roomParent, Room room, int roomIndex)
    {
        if (wallPrefab == null)
        {
            UnityEngine.Debug.LogWarning("[Test11] No wall prefab assigned.");
            return;
        }

        // Obtener el tamaño escalado de la sala
        Vector3 roomScale = GetFootprintScale(room.bounds.width, room.bounds.height);
        float roomWorldWidth = roomScale.x;
        float roomWorldDepth = roomScale.z;

        Vector3 roomCenter = roomParent.position;

        // Crear 4 paredes (arriba, abajo, izquierda, derecha en vista superior)
        // Pared frente (Z+)
        CreateWall(roomParent, roomIndex, 
            roomCenter + new Vector3(0, wallHeight / 2, roomWorldDepth / 2),
            new Vector3(roomWorldWidth, wallHeight, wallThickness),
            "front");

        // Pared atrás (Z-)
        CreateWall(roomParent, roomIndex, 
            roomCenter + new Vector3(0, wallHeight / 2, -roomWorldDepth / 2),
            new Vector3(roomWorldWidth, wallHeight, wallThickness),
            "back");

        // Pared derecha (X+)
        CreateWall(roomParent, roomIndex, 
            roomCenter + new Vector3(roomWorldWidth / 2, wallHeight / 2, 0),
            new Vector3(wallThickness, wallHeight, roomWorldDepth),
            "right");

        // Pared izquierda (X-)
        CreateWall(roomParent, roomIndex, 
            roomCenter + new Vector3(-roomWorldWidth / 2, wallHeight / 2, 0),
            new Vector3(wallThickness, wallHeight, roomWorldDepth),
            "left");
    }

    void CreateWall(Transform roomParent, int roomIndex, Vector3 position, Vector3 scale, string wallName)
    {
        GameObject wall = Instantiate(wallPrefab, position, Quaternion.identity, roomParent);
        wall.name = $"pared_{wallName}_{roomIndex}";
        wall.transform.localScale = scale;
    }

    void SpawnFloorObjects(Transform roomParent, Room room, int roomIndex)
    {
        if (floorObjectsPrefabs == null || floorObjectsPrefabs.Length == 0)
        {
            return;
        }

        Vector3 roomScale = GetFootprintScale(room.bounds.width, room.bounds.height);
        float roomWorldWidth = roomScale.x;
        float roomWorldDepth = roomScale.z;
        Vector3 roomCenter = roomParent.position;

        // Generar objetos de suelo aleatoriamente dentro de la sala
        int objectCount = Random.Range(1, Mathf.Max(2, Mathf.RoundToInt(room.bounds.width * room.bounds.height * 0.01f)));
        
        for (int i = 0; i < objectCount; i++)
        {
            if (Random.value < floorObjectsSpawnChance)
            {
                float randomX = Random.Range(-roomWorldWidth / 2 + 1f, roomWorldWidth / 2 - 1f);
                float randomZ = Random.Range(-roomWorldDepth / 2 + 1f, roomWorldDepth / 2 - 1f);
                Vector3 spawnPos = roomCenter + new Vector3(randomX, 0, randomZ);

                GameObject floorObjPrefab = floorObjectsPrefabs[Random.Range(0, floorObjectsPrefabs.Length)];
                GameObject floorObj = Instantiate(floorObjPrefab, spawnPos, Quaternion.Euler(0, Random.Range(0f, 360f), 0), roomParent);
                floorObj.name = $"floorObj_{roomIndex}_{i}";
            }
        }
    }

    void SpawnWallObjects(Transform roomParent, Room room, int roomIndex)
    {
        if (wallObjectsPrefabs == null || wallObjectsPrefabs.Length == 0)
        {
            return;
        }

        Vector3 roomScale = GetFootprintScale(room.bounds.width, room.bounds.height);
        float roomWorldWidth = roomScale.x;
        float roomWorldDepth = roomScale.z;
        Vector3 roomCenter = roomParent.position;

        // Objetos en pared frente
        SpawnObjectsOnWall(roomParent, roomIndex, 
            roomCenter, roomWorldWidth, wallThickness, wallHeight, "front",
            new Vector3(0, 0, roomWorldDepth / 2), Vector3.forward);

        // Objetos en pared atrás
        SpawnObjectsOnWall(roomParent, roomIndex, 
            roomCenter, roomWorldWidth, wallThickness, wallHeight, "back",
            new Vector3(0, 0, -roomWorldDepth / 2), Vector3.back);

        // Objetos en pared derecha
        SpawnObjectsOnWall(roomParent, roomIndex, 
            roomCenter, roomWorldDepth, wallThickness, wallHeight, "right",
            new Vector3(roomWorldWidth / 2, 0, 0), Vector3.right);

        // Objetos en pared izquierda
        SpawnObjectsOnWall(roomParent, roomIndex, 
            roomCenter, roomWorldDepth, wallThickness, wallHeight, "left",
            new Vector3(-roomWorldWidth / 2, 0, 0), Vector3.left);
    }

    void SpawnObjectsOnWall(Transform roomParent, int roomIndex, Vector3 roomCenter, 
        float wallLength, float wallThickness, float wallHeight, string wallName, 
        Vector3 wallOffset, Vector3 wallDirection)
    {
        int objectCount = Random.Range(1, Mathf.Max(2, Mathf.RoundToInt(wallLength * 0.05f)));

        for (int i = 0; i < objectCount; i++)
        {
            if (Random.value < wallObjectsSpawnChance)
            {
                float randomPos = Random.Range(-wallLength / 2 + 0.5f, wallLength / 2 - 0.5f);
                float randomHeight = Random.Range(0.5f, wallHeight - 0.5f);

                Vector3 spawnPos;
                if (wallName == "front" || wallName == "back")
                {
                    spawnPos = roomCenter + new Vector3(randomPos, randomHeight, wallOffset.z);
                }
                else // right or left
                {
                    spawnPos = roomCenter + new Vector3(wallOffset.x, randomHeight, randomPos);
                }

                GameObject wallObjPrefab = wallObjectsPrefabs[Random.Range(0, wallObjectsPrefabs.Length)];
                Quaternion rotation = Quaternion.LookRotation(-wallDirection);
                GameObject wallObj = Instantiate(wallObjPrefab, spawnPos, rotation, roomParent);
                wallObj.name = $"wallObj_{wallName}_{roomIndex}_{i}";
            }
        }
    }

    Vector3 GetFootprintScale(float gridWidth, float gridHeight)
    {
        Vector3 scale = new Vector3(
            gridWidth * 1.7f / 10f * 5f,
            1f,
            gridHeight * 1.7f / 10f * 5f
        );

        return scale;
    }
}
