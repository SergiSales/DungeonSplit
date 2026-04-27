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
    public GameObject floorPrefab;
    public float cellSize = 10f;
    public float roomHeight = 0f;
    public Transform roomsParent;

    [Header("Wall Spawning")]
    public GameObject wallPrefab;
    public float wallHeight = 3f;
    public float wallThickness = 0.15f;
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
            rooms = densityGenerator.FilterRoomsByDensity(rooms, densityMap, densityThreshold);
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

void SpawnRooms()
    {
        if (floorPrefab == null)
        {
            UnityEngine.Debug.LogWarning("[Test11] No room prefab assigned.");
            UnityEngine.Debug.Log("[Test11] No room prefab assigned.");
            return;
        }

        Transform parent = roomsParent != null ? roomsParent : transform;


        foreach (Room room in rooms)
        {
            SpawnFloor(room, parent);
            SpawnWalls(room, parent);
        }
        return;
    }

    string SpawnFloor(Room room, Transform parent)
    {
        Vector3 worldPos = GridToWorld(room.center);
        GameObject instance = Instantiate(floorPrefab, worldPos, Quaternion.identity, parent);
        ScaleRoom(instance.transform, room);

        return instance.name;
    }

    void SpawnWalls(Room room, Transform parent)
    {
        if (wallPrefab == null) return;

        Vector3 bottomLeft = GridToWorld(room.bottomLeft);
        Vector3 bottomRight = GridToWorld(room.bottomRight);
        Vector3 topLeft = GridToWorld(room.topLeft);
        Vector3 topRight = GridToWorld(room.topRight);

        // 🔹 Bottom wall
        CreateWall(bottomLeft, bottomRight, parent);

        // 🔹 Top wall
        CreateWall(topLeft, topRight, parent);

        // 🔹 Left wall
        CreateWall(bottomLeft, topLeft, parent);

        // 🔹 Right wall
        CreateWall(bottomRight, topRight, parent);
    }

    void CreateWall(Vector3 start, Vector3 end, Transform parent)
    {
        Vector3 direction = end - start;
        float length = direction.magnitude;

        Vector3 position = start + direction * 0.5f;

        GameObject wall = Instantiate(wallPrefab, position, Quaternion.identity, parent);

        // Rotación (alinear con dirección)
        wall.transform.right = direction.normalized;

        // Escala: largo en X (o Z según prefab)
        wall.transform.localScale = new Vector3(length * 1.666666f, wallHeight, wallThickness);
    }

    Vector3 GridToWorld(Vector2Int gridPos)
    {
        Vector3 worldPos = new Vector3(
            gridPos.x * cellSize * 7f,
            roomHeight,
            gridPos.y * cellSize * 7f
        );
        return worldPos;
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
        Vector3 scale = new Vector3(
            room.bounds.width * 1.7f / 10f * 7f,
            1f,
            room.bounds.height * 1.7f / 10f * 7f
        );
        roomTransform.localScale = scale;
    }

}

        

    
