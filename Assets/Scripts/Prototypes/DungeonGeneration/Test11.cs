using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class Test11 : TestBase
{
    private const float MinimumRoomEdgePadding = 0.75f;
    private const float RoomEdgePaddingFactor = 0.35f;
    private const float MinimumSpawnableHalfExtent = 0.25f;

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


    [Header("Room Spawning")]
    public GameObject[] objectsPrefab;
    public float cellSize = 5f;
    public Transform roomsParent;
    public float roomSpacingMultiplier = 5f;
    public float wallHeight = 10f;
    public float wallThickness = 0.15f;

    [Header("Floor Objects")]
    public GameObject[] floorObjectsPrefabs;
    [Range(0f, 1f)] public float floorObjectsSpawnChance = 0.3f;

    [Header("Player Spawn")]
    public GameObject playerPrefab;
    public float playerSpawnHeight = 1f;

    [Header("Wave Rooms")]
    public GameObject enemyFather;
    public GameObject waveEnemyPrefab;
    [Min(1)] public int maxEnemiesPerWaveRoom = 200;
    [Min(0f)] public float waveStartDelay = 5f;
    [Min(0.05f)] public float waveSpawnInterval = 0.35f;
    [Min(0f)] public float minSpawnDistanceFromPlayer = 12f;
    [Min(0f)] public float maxSpawnDistanceFromPlayer = 30f;
    public float enemySpawnHeight = 1f;
    [Min(1)] public int spawnPositionAttempts = 20;

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

    // Script references
    private DensityMapGenerator densityGenerator;
    private DelaunayGenerator delaunayGenerator;
    private MSTGenerator mstGenerator;
    private AssetsSpawner assetsSpawner;
    UIMinimap uiMinimap;
    ThirdPersonController player;
    #endregion

    
    void Start()
    {
        Stopwatch totalTimer = Stopwatch.StartNew();
        seed = Random.Range(0, 100000);
        enableTeleport = true;
        
        InitializeGenerators();
        // Generacion del mapa de densidad
        if (usePerlinNoise)
        {
            densityMap = densityGenerator.GeneratePerlinNoise(perlinScale);
        }

        // Generacion BSP
        BSPGenerator bspGenerator = new BSPGenerator(minRoomSize, maxRoomSize, seed);
        root = bspGenerator.Generate(new IntRect(0, 0, dungeonWidth, dungeonHeight));
        rooms = bspGenerator.CreateRooms(root);

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

        uiMinimap = FindAnyObjectByType<UIMinimap>();

        AssignRoomTypes();
        assetsSpawner.SpawnRooms(objectsPrefab, rooms, cellSize, wallThickness, roomSpacingMultiplier, mstEdges, uiMinimap);


        
        
        player = FindAnyObjectByType<ThirdPersonController>();
        uiMinimap.playerTransform = player.transform;
            
        // Llamamos a la función actualizada con los parámetros extra
        uiMinimap.GenerateAbstractMap(rooms, cellSize, roomSpacingMultiplier);
        
        
        

        totalTimer.Stop();
        UnityEngine.Debug.Log($"[Test11] Generated rooms: {rooms.Count}");
        UnityEngine.Debug.Log($"[Test11] Total generation time: {totalTimer.ElapsedMilliseconds}ms");
    }

    private void InitializeGenerators()
    {
        densityGenerator = new DensityMapGenerator(dungeonWidth, dungeonHeight, seed);
        delaunayGenerator = new DelaunayGenerator();
        mstGenerator = new MSTGenerator();
        //assetsSpawner = AssetsSpawner.GetOrCreate(gameObject);
    }


    void SpawnPlayer(int roomIndex)
    {
        Room spawnRoom = rooms[roomIndex];
        Vector3 spawnPosition = assetsSpawner.GridToWorld(spawnRoom.center, cellSize, roomSpacingMultiplier);
        spawnPosition.y = playerSpawnHeight;
        
        spawnRoom.visited = true; // Marcar la sala de spawn como visitada para el minimapa

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

    public override void HandlePlayerTeleported(Transform playerTransform, Room room)
    {
        UnityEngine.Debug.Log($"[Test11] Room Type: {room.type}");
        if (playerTransform == null || room == null)
        {
            return;
        }

        if (room.type != roomTypes.Wave || room.waveStarted || room.cleared)
        {
            return;
        }

        room.waveStarted = true;
        StartCoroutine(StartWaveAfterDelay(room, playerTransform));
    }

    private IEnumerator StartWaveAfterDelay(Room room, Transform playerTransform)
    {
        yield return new WaitForSeconds(waveStartDelay);

        if (waveEnemyPrefab == null)
        {
            UnityEngine.Debug.LogWarning("[Test11] No wave enemy prefab assigned. Wave room cannot spawn enemies.");
            yield break;
        }

        int spawnedEnemies = 0;
        
        if (enemyFather == null)
        {
            enemyFather = new GameObject("Enemy Father");
            UnityEngine.Debug.Log("enemyFather created");
        }
        while (spawnedEnemies < maxEnemiesPerWaveRoom)
        {
            Vector3 spawnPosition;
            if (TryGetSpawnPositionInRoom(room, playerTransform.position, out spawnPosition))
            {
                GameObject enemy = Instantiate(waveEnemyPrefab, spawnPosition, Quaternion.identity);
                
                enemy.transform.SetParent(enemyFather.transform);
                spawnedEnemies++;
            }

            yield return new WaitForSeconds(waveSpawnInterval);
        }

        room.cleared = true;
    }

    private bool TryGetSpawnPositionInRoom(Room room, Vector3 playerPosition, out Vector3 spawnPosition)
    {
        Vector3 roomCenter = assetsSpawner.GridToWorld(room.center, cellSize, roomSpacingMultiplier);
        float halfRoomWidth = (room.bounds.width * cellSize) / 2f;
        float halfRoomHeight = (room.bounds.height * cellSize) / 2f;
        float edgePadding = Mathf.Max(MinimumRoomEdgePadding, cellSize * RoomEdgePaddingFactor);
        float usableHalfWidth = Mathf.Max(MinimumSpawnableHalfExtent, halfRoomWidth - edgePadding);
        float usableHalfHeight = Mathf.Max(MinimumSpawnableHalfExtent, halfRoomHeight - edgePadding);
        float roomMinX = roomCenter.x - usableHalfWidth;
        float roomMaxX = roomCenter.x + usableHalfWidth;
        float roomMinZ = roomCenter.z - usableHalfHeight;
        float roomMaxZ = roomCenter.z + usableHalfHeight;

        float clampedMaxDistance = Mathf.Max(minSpawnDistanceFromPlayer, maxSpawnDistanceFromPlayer);

        for (int attempt = 0; attempt < spawnPositionAttempts; attempt++)
        {
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            if (randomDirection == Vector2.zero)
            {
                randomDirection = Vector2.right;
            }

            float randomDistance = Random.Range(minSpawnDistanceFromPlayer, clampedMaxDistance);
            Vector3 candidate = playerPosition + new Vector3(randomDirection.x, 0f, randomDirection.y) * randomDistance;

            candidate.x = Mathf.Clamp(candidate.x, roomMinX, roomMaxX);
            candidate.z = Mathf.Clamp(candidate.z, roomMinZ, roomMaxZ);
            candidate.y = enemySpawnHeight;

            float distanceToPlayer = Vector3.Distance(
                new Vector3(candidate.x, 0f, candidate.z),
                new Vector3(playerPosition.x, 0f, playerPosition.z));

            if (distanceToPlayer >= minSpawnDistanceFromPlayer)
            {
                spawnPosition = candidate;
                return true;
            }
        }

        spawnPosition = roomCenter;
        spawnPosition.y = enemySpawnHeight;
        return true;
    }
}

        

    
