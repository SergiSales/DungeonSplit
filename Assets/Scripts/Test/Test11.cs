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

    [Header("Corridor Settings")]
    public bool generateCorridors = true;
    [Range(0f, 200f)] public float roomCrossPenalty = 30f;
    [Range(0f, 2f)] public float turnPenalty = 1.2f;
    [Range(0.1f, 2f)] public float existingCorridorCost = 0.4f;

    [Header("MST Settings")]
    [Range(0, 20)] public int extraCycleEdges = 2;

    [Header("Gizmo/Map Settings")]

    public bool showBSPNodes = true;
    public bool showDensityMap = false;
    public bool showRooms = true;
    public bool showDelaunay = true;
    public bool showMST = true;
    public bool showCorridors = true;
    public bool showCorridorCells = true;

    [Header("Room Spawning")]
    public bool spawnGeneratedRooms = true;
    public GameObject[] roomPrefabs;
    public float cellSize = 10f;
    public float roomHeight = 0f;
    public Transform roomsParent;

    [Header("Corridor Spawning")]
    public bool spawnGeneratedCorridors = true;
    public GameObject[] corridorPrefabs;
    public Transform corridorsParent;

    [Header("Player Spawn")]
    public bool spawnPlayerOnStart = true;
    public GameObject playerPrefab;
    public float playerSpawnHeight = 1f;

    [Header("Debug Stats")]
    public int generatedRoomCount;
    public int generatedCorridorCount;
    public int carvedCorridorTiles;

    #endregion

    #region Private Variables
    // Generation data
    private BSPNode root;
    private List<Room> rooms = new List<Room>();
    private List<MSTEdge> mstEdges = new List<MSTEdge>();
    private List<MSTEdge> delaunayEdges = new List<MSTEdge>();
    private List<DelaunayTriangle> delaunayTriangles = new List<DelaunayTriangle>();
    private List<List<Vector2Int>> corridorPaths = new List<List<Vector2Int>>();
    private float[,] densityMap;
    private int[,] occupancyGrid;
    private int[,] roomIndexGrid;
    private Dictionary<Vector2Int, Room> roomByCenter = new Dictionary<Vector2Int, Room>();
    private Dictionary<Vector2Int, int> roomIndexByCenter = new Dictionary<Vector2Int, int>();

    // Generators
    private DensityMapGenerator densityGenerator;
    private DelaunayGenerator delaunayGenerator;
    private MSTGenerator mstGenerator;
    private CorridorGenerator corridorGenerator;
    private GridUtilities gridUtilities;
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
            densityGenerator.FilterRoomsByDensity(ref rooms, densityMap, densityThreshold);
        }

        // Construccion de lookups y navegacion
        generatedRoomCount = rooms.Count;
        gridUtilities.BuildRoomLookups(rooms, out roomByCenter, out roomIndexByCenter);
        gridUtilities.BuildNavigationGrid(rooms, dungeonWidth, dungeonHeight, out occupancyGrid, out roomIndexGrid);

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

            // Generacion de corredores
            if (generateCorridors && mstEdges.Count > 0)
            {
                corridorGenerator.GenerateCorridors(
                    mstEdges,
                    roomByCenter,
                    roomIndexByCenter,
                    occupancyGrid,
                    roomIndexGrid,
                    dungeonWidth,
                    dungeonHeight,
                    roomCrossPenalty,
                    turnPenalty,
                    existingCorridorCost,
                    out corridorPaths,
                    out carvedCorridorTiles);
                generatedCorridorCount = corridorPaths.Count;
            }
        }

        if (spawnGeneratedRooms)
        {
            SpawnRooms();
        }

        if (spawnGeneratedCorridors)
        {
            SpawnCorridors();
        }

        if (spawnPlayerOnStart)
        {
            SpawnPlayerInRandomRoom();
        }

        

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

        if (showCorridors)
        {
            visualization.DrawCorridorGizmos(corridorPaths, showCorridorCells);
        }
    }

    private void InitializeGenerators()
    {
        densityGenerator = new DensityMapGenerator(dungeonWidth, dungeonHeight, seed);
        delaunayGenerator = new DelaunayGenerator();
        mstGenerator = new MSTGenerator();
        corridorGenerator = new CorridorGenerator();
        gridUtilities = new GridUtilities();
        visualization = new DungeonVisualization();
    }

    void SpawnRooms()
    {
        if (roomPrefabs == null || roomPrefabs.Length == 0)
        {
            UnityEngine.Debug.LogWarning("[Test11] No room prefabs assigned.");
            return;
        }

        Transform parent = roomsParent != null ? roomsParent : transform;
        int roomIndex = 1;

        foreach (Room room in rooms)
        {
            SpawnRoom(room, parent, roomIndex);
            roomIndex++;
        }
    }

    void SpawnRoom(Room room, Transform parent, int roomIndex)
    {
        Vector3 worldPos = GridToWorld(room.center);
        GameObject prefab = roomPrefabs[Random.Range(0, roomPrefabs.Length)];
        GameObject instance = Instantiate(prefab, worldPos, Quaternion.identity, parent);
        instance.name = $"suelo{roomIndex}";
        ScaleRoom(instance.transform, room);
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

    void SpawnPlayerInRandomRoom()
    {
        if (rooms == null || rooms.Count == 0)
        {
            UnityEngine.Debug.LogWarning("[Test11] Cannot spawn player because no rooms were generated.");
            return;
        }

        Room spawnRoom = rooms[Random.Range(0, rooms.Count)];
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
        UnityEngine.Debug.Log($"[Test11] Player spawned in room centered at {spawnRoom.center}.");
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

    void SpawnCorridors()
    {
        if (corridorPaths == null || corridorPaths.Count == 0)
        {
            return;
        }

        GameObject[] prefabsToUse = GetCorridorPrefabs();
        if (prefabsToUse == null || prefabsToUse.Length == 0)
        {
            UnityEngine.Debug.LogWarning("[Test11] No corridor prefabs assigned and no room prefabs available as fallback.");
            return;
        }

        Transform parent = corridorsParent != null ? corridorsParent : transform;
        int corridorIndex = 1;

        foreach (List<Vector2Int> path in corridorPaths)
        {
            corridorIndex = SpawnCorridorPath(path, prefabsToUse, parent, corridorIndex);
        }
    }

    int SpawnCorridorPath(List<Vector2Int> path, GameObject[] prefabsToUse, Transform parent, int corridorIndex)
    {
        if (path == null || path.Count == 0)
        {
            return corridorIndex;
        }

        List<Vector2Int> simplifiedPath = corridorGenerator.SimplifyPath(path);
        if (simplifiedPath.Count == 1)
        {
            SpawnCorridorSegment(simplifiedPath[0], simplifiedPath[0], prefabsToUse, parent, corridorIndex);
            return corridorIndex + 1;
        }

        for (int i = 0; i < simplifiedPath.Count - 1; i++)
        {
            SpawnCorridorSegment(simplifiedPath[i], simplifiedPath[i + 1], prefabsToUse, parent, corridorIndex);
            corridorIndex++;
        }

        return corridorIndex;
    }

    void SpawnCorridorSegment(Vector2Int startCell, Vector2Int endCell, GameObject[] prefabsToUse, Transform parent, int corridorIndex)
    {
        int deltaX = Mathf.Abs(endCell.x - startCell.x);
        int deltaY = Mathf.Abs(endCell.y - startCell.y);
        int segmentLength = Mathf.Max(deltaX, deltaY) + 1;

        float gridWidth = deltaX > 0 ? segmentLength : 1f;
        float gridHeight = deltaY > 0 ? segmentLength : 1f;
        Vector2 midpoint = new Vector2(
            (startCell.x + endCell.x) * 0.5f,
            (startCell.y + endCell.y) * 0.5f
        );

        GameObject prefab = prefabsToUse[Random.Range(0, prefabsToUse.Length)];
        GameObject instance = Instantiate(prefab, GridToWorld(midpoint, roomHeight), Quaternion.identity, parent);
        instance.name = $"pasillo{corridorIndex}";
        instance.transform.localScale = GetFootprintScale(gridWidth, gridHeight);
    }

    GameObject[] GetCorridorPrefabs()
    {
        if (corridorPrefabs != null && corridorPrefabs.Length > 0)
        {
            return corridorPrefabs;
        }

        return roomPrefabs;
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
