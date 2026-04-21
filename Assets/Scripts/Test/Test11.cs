using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class Test11 : MonoBehaviour
{
    [Header("Dungeon Settings")]
    public int dungeonWidth = 100;
    public int dungeonHeight = 100;
    public int minRoomSize = 14;
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
    public bool showDelaunay = false;
    public bool showMST = true;
    public bool showRooms = true;
    public bool showBSPNodes = false;
    public bool showDensityMap = false;
    public bool showCorridors = true;
    public bool showCorridorCells = true;

    [Header("Room Spawning")]
    public bool spawnGeneratedRooms = true;
    public GameObject[] roomPrefabs;
    public float cellSize = 1f;
    public float roomHeight = 0f;
    public Transform roomsParent;

    [Header("Debug Stats")]
    public int generatedRoomCount;
    public int generatedCorridorCount;
    public int carvedCorridorTiles;

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

    void Start()
    {
        Stopwatch totalTimer = Stopwatch.StartNew();
        seed = Random.Range(0, 100000);
        InitializeGenerators();

        // Fase 1: Generación del mapa de densidad
        if (usePerlinNoise)
        {
            densityMap = densityGenerator.GeneratePerlinNoise(perlinScale);
        }
        
        // Fase 2: Generación BSP
        BSPGenerator bspGenerator = new BSPGenerator(minRoomSize, seed);
        root = bspGenerator.Generate(new IntRect(0, 0, dungeonWidth, dungeonHeight));
        rooms = bspGenerator.CreateRooms(root);
        
        // Fase 3: Filtrado por densidad
        if (usePerlinNoise && densityMap != null)
        {
            densityGenerator.FilterRoomsByDensity(ref rooms, densityMap, densityThreshold);
        }
        
        // Fase 4: Construcción de lookups y navegación
        generatedRoomCount = rooms.Count;
        gridUtilities.BuildRoomLookups(rooms, out roomByCenter, out roomIndexByCenter);
        gridUtilities.BuildNavigationGrid(rooms, dungeonWidth, dungeonHeight, out occupancyGrid, out roomIndexGrid);
        
        if (rooms.Count > 1)
        {
            // Fase 5: Triangulación Delaunay
            List<Vector2Int> points = new List<Vector2Int>();
            foreach (Room room in rooms)
                points.Add(room.center);
            
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
            
            // Fase 6: Árbol de expansión mínima
            mstEdges = mstGenerator.GenerateMST(delaunayEdges, rooms);
            mstGenerator.ControlDeadEnds(mstEdges, delaunayEdges, deadEndKeepChance, deadEndConnectChance);
            int targetExtraEdges = Mathf.RoundToInt(delaunayEdges.Count * extraConnectionFactor);
            mstGenerator.AddCyclesToMST(mstEdges, delaunayEdges, loopQualityBias, randomnessFactor,
                minGraphDistanceThreshold, targetExtraEdges);
            
            // Fase 7: Generación de corredores
            if (generateCorridors && mstEdges.Count > 0)
            {
                corridorGenerator.GenerateCorridors(mstEdges, roomByCenter, roomIndexByCenter, 
                    occupancyGrid, roomIndexGrid, dungeonWidth, dungeonHeight, 
                    roomCrossPenalty, turnPenalty, existingCorridorCost, 
                    out corridorPaths, out carvedCorridorTiles);
                generatedCorridorCount = corridorPaths.Count;
            }
        }

        if (spawnGeneratedRooms)
        {
            SpawnRooms();
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

        foreach (Room room in rooms)
        {
            SpawnRoom(room, parent);
        }
    }

    void SpawnRoom(Room room, Transform parent)
    {
        Vector3 worldPos = GridToWorld(room.center);
        GameObject prefab = roomPrefabs[Random.Range(0, roomPrefabs.Length)];
        GameObject instance = Instantiate(prefab, worldPos, Quaternion.identity, parent);
        ScaleRoom(instance.transform, room);
    }

    Vector3 GridToWorld(Vector2Int gridPos)
    {
        return new Vector3(
            gridPos.x * cellSize,
            roomHeight,
            gridPos.y * cellSize
        );
    }

    void ScaleRoom(Transform roomTransform, Room room)
    {
        Vector3 scale = new Vector3(
            room.bounds.width * 1.7f / 10f,
            1f,
            room.bounds.height * 1.7f / 10f
        );

        roomTransform.localScale = scale;
    }
}
