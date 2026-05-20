using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class Test12 : TestBase
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

    [Header("Room Spawning")]
    public GameObject[] objectsPrefab;
    public float cellSize = 5f;
    public Transform roomsParent;
    public float roomSpacingMultiplier = 5f;
    public float wallHeight = 10f;
    public float wallThickness = 0.15f;

    [Header("Player Spawn")]
    public GameObject playerPrefab;
    public float playerSpawnHeight = 1f;

    [Header("Wave Rooms")]
    public GameObject enemyFather;
    public GameObject[] waveEnemyPrefab;
    public int enemyCountInCurrentWave;
    [Min(1)] public int maxEnemiesPerWaveRoom = 500;
    [Min(0f)] public float waveStartDelay = 2f;
    [Min(0.05f)] public float waveSpawnInterval = 0.05f;
    [Min(0f)] public float minSpawnDistanceFromPlayer = 12f;
    [Min(0f)] public float maxSpawnDistanceFromPlayer = 30f;
    public float enemySpawnHeight = 1f;
    [Min(1)] public int spawnPositionAttempts = 20;

    [Header("Debug Stats")]
    public int generatedRoomCount;
    #endregion

    #region Private Variables
    private readonly List<Room> rooms = new List<Room>();
    private readonly List<MSTEdge> mstEdges = new List<MSTEdge>();
    private AssetsSpawner assetsSpawner;
    private WaveEncounter waveEncounter;
    
    private UIMinimap uiMinimap;
    #endregion

    private void Start()
    {
        Stopwatch totalTimer = Stopwatch.StartNew();
        seed = Random.Range(0, 100000);
        InitializeTeleportState();

        assetsSpawner = new AssetsSpawner();
        BuildDungeon();
        SetupScene();

        totalTimer.Stop();
        UnityEngine.Debug.Log($"[Test12] Generated rooms: {rooms.Count}");
        UnityEngine.Debug.Log($"[Test12] Total generation time: {totalTimer.ElapsedMilliseconds}ms");
    }

    private void BuildDungeon()
    {
        DungeonBuilder dungeonBuilder = new DungeonBuilder(
            dungeonWidth,
            dungeonHeight,
            minRoomSize,
            maxRoomSize,
            seed,
            usePerlinNoise,
            perlinScale,
            densityThreshold,
            loopQualityBias,
            randomnessFactor,
            minGraphDistanceThreshold,
            extraConnectionFactor,
            deadEndKeepChance,
            deadEndConnectChance);

        DungeonBuildResult dungeon = dungeonBuilder.Build();
        rooms.Clear();
        rooms.AddRange(dungeon.Rooms);

        mstEdges.Clear();
        mstEdges.AddRange(dungeon.MstEdges);
        generatedRoomCount = dungeon.GeneratedRoomCount;

        RoomPlanner roomPlanner = new RoomPlanner(
            rooms,
            assetsSpawner,
            playerPrefab,
            playerSpawnHeight,
            cellSize,
            roomSpacingMultiplier);
        roomPlanner.AssignRoomTypes();
    }

    private void SetupScene()
    {
        uiMinimap = FindAnyObjectByType<UIMinimap>();
        assetsSpawner.SpawnRooms(
            objectsPrefab,
            rooms,
            cellSize,
            wallThickness,
            roomSpacingMultiplier,
            mstEdges,
            uiMinimap,
            roomsParent);

        InitializeMinimap();
        waveEncounter = CreateWaveEncounter();
    }

    private void InitializeMinimap()
    {
        if (uiMinimap == null)
        {
            UnityEngine.Debug.LogWarning("[Test12] No UIMinimap found in scene.");
            return;
        }

        ThirdPersonController playerController = FindAnyObjectByType<ThirdPersonController>();
        if (playerController == null)
        {
            UnityEngine.Debug.LogWarning("[Test12] No player found for minimap initialization.");
            return;
        }

        uiMinimap.playerTransform = playerController.transform;
        uiMinimap.GenerateAbstractMap(rooms, cellSize, roomSpacingMultiplier);
    }

    public override void HandlePlayerTeleported(Transform playerTransform, Room room)
    {
        UnityEngine.Debug.Log($"[Test12] Room Type: {room.type}");
        if (playerTransform == null || room == null)
        {
            return;
        }

        if (room.type != roomTypes.Wave || room.waveStarted || room.cleared)
        {
            return;
        }

        room.waveStarted = true;
        LockTeleportForWave();
        waveEncounter ??= CreateWaveEncounter();
        enemyFather = waveEncounter.EnsureEnemyParent(enemyFather);
        StartCoroutine(waveEncounter.RunWave(
            room,
            playerTransform,
            enemyFather,
            AreAllWaveEnemiesDefeated,
            () => CompleteWaveRoom(room)));
    }

    private WaveEncounter CreateWaveEncounter()
    {
        return new WaveEncounter(
            assetsSpawner,
            waveEnemyPrefab,
            maxEnemiesPerWaveRoom,
            waveStartDelay,
            waveSpawnInterval,
            minSpawnDistanceFromPlayer,
            maxSpawnDistanceFromPlayer,
            enemySpawnHeight,
            spawnPositionAttempts,
            cellSize,
            roomSpacingMultiplier);
    }

    private void CompleteWaveRoom(Room room)
    {
        room.cleared = true;
        UnlockTeleportAfterWave();
    }
}
