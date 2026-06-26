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
    public float wallThickness = 0.15f;

    [Header("Player Spawn")]
    public GameObject playerPrefab;
    public float playerSpawnHeight = 1f;

    [Header("Wave Rooms")]
    public GameObject enemyFather;
    public GameObject[] waveEnemyPrefab;
    public int maxEnemiesPerWaveRoom = 200;
    public int enemiesSimultaneouslyInRoom = 30;
    public float waveStartDelay = 2f;
    private float waveSpawnInterval = 0.2f;
    public float minSpawnDistanceFromPlayer = 12f;
    public float maxSpawnDistanceFromPlayer = 30f;
    public float enemySpawnHeight = 1f;
    public int spawnPositionAttempts = 20;

    [Header("Debug Stats")]
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
        if (GameManager.instance == null)
        {
            GameObject gmObject = new GameObject("GameManager");
            gmObject.AddComponent<GameManager>();
        }
        
        Stopwatch totalTimer = Stopwatch.StartNew();
        seed = Random.Range(0, 100000);
        InitializeTeleportState();

        assetsSpawner = gameObject.GetComponent<AssetsSpawner>() ?? gameObject.AddComponent<AssetsSpawner>();
        BuildDungeon();
        SetupScene();

        totalTimer.Stop();
        UnityEngine.Debug.Log($"[Test12] Generated rooms: {rooms.Count}");
        UnityEngine.Debug.Log($"[Test12] Total generation time: {totalTimer.ElapsedMilliseconds}ms");
    }

    private void BuildDungeon()
    {
        DungeonBuilder dungeonBuilder = gameObject.GetComponent<DungeonBuilder>() ?? gameObject.AddComponent<DungeonBuilder>();
        dungeonBuilder.dungeonWidth = dungeonWidth;
        dungeonBuilder.dungeonHeight = dungeonHeight;
        dungeonBuilder.minRoomSize = minRoomSize;
        dungeonBuilder.maxRoomSize = maxRoomSize;
        dungeonBuilder.seed = seed;
        dungeonBuilder.usePerlinNoise = usePerlinNoise;
        dungeonBuilder.perlinScale = perlinScale;
        dungeonBuilder.densityThreshold = densityThreshold;
        dungeonBuilder.loopQualityBias = loopQualityBias;
        dungeonBuilder.randomnessFactor = randomnessFactor;
        dungeonBuilder.minGraphDistanceThreshold = minGraphDistanceThreshold;
        dungeonBuilder.extraConnectionFactor = extraConnectionFactor;
        dungeonBuilder.deadEndKeepChance = deadEndKeepChance;
        dungeonBuilder.deadEndConnectChance = deadEndConnectChance;

        DungeonBuildResult dungeon = dungeonBuilder.Build();
        rooms.Clear();
        rooms.AddRange(dungeon.Rooms);

        mstEdges.Clear();
        mstEdges.AddRange(dungeon.MstEdges);

        RoomPlanner roomPlanner = gameObject.GetComponent<RoomPlanner>() ?? gameObject.AddComponent<RoomPlanner>();
        roomPlanner.rooms = rooms;
        roomPlanner.assetsSpawner = assetsSpawner;
        roomPlanner.playerPrefab = playerPrefab;
        roomPlanner.playerSpawnHeight = playerSpawnHeight;
        roomPlanner.cellSize = cellSize;
        roomPlanner.roomSpacingMultiplier = roomSpacingMultiplier;
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
        uiMinimap.GenerateAbstractMap(rooms);
    }

    public override void HandlePlayerTeleported(Transform playerTransform, Room room)
    {
        UnityEngine.Debug.Log($"[Test12] Room Type: {room.type}");
        if (playerTransform == null || room == null)
        {
            return;
        }

        // Actualizar sala actual en GameManager
        if (GameManager.instance != null)
        {
            GameManager.instance.SetCurrentRoom(room);
        }

        if (room.type != roomTypes.Wave || room.waveStarted || room.cleared)
        {
            return;
        }

        room.waveStarted = true;
        LockTeleportForWave();
        waveEncounter ??= CreateWaveEncounter();
        enemyFather = waveEncounter.EnsureEnemyParent(enemyFather);
        StartCoroutine(UpdateInfoText("Prepare for the wave!"));
        StartCoroutine(waveEncounter.RunWave(
            room,
            playerTransform,
            enemyFather,
            AreAllWaveEnemiesDefeated,
            () => CompleteWaveRoom(room)));
    }

    private WaveEncounter CreateWaveEncounter()
    {
        WaveEncounter encounter = gameObject.GetComponent<WaveEncounter>() ?? gameObject.AddComponent<WaveEncounter>();
        encounter.assetsSpawner = assetsSpawner;
        encounter.waveEnemyPrefabs = waveEnemyPrefab;
        encounter.maxEnemiesPerWaveRoom = maxEnemiesPerWaveRoom;
        encounter.enemiesSimultaneouslyInRoom = enemiesSimultaneouslyInRoom;
        encounter.waveStartDelay = waveStartDelay;
        encounter.waveSpawnInterval = waveSpawnInterval;
        encounter.minSpawnDistanceFromPlayer = minSpawnDistanceFromPlayer;
        encounter.maxSpawnDistanceFromPlayer = maxSpawnDistanceFromPlayer;
        encounter.enemySpawnHeight = enemySpawnHeight;
        encounter.spawnPositionAttempts = spawnPositionAttempts;
        encounter.cellSize = cellSize;
        encounter.roomSpacingMultiplier = roomSpacingMultiplier;
        return encounter;
    }

    private void CompleteWaveRoom(Room room)
    {
        room.cleared = true;
        GameManager.instance?.NotifyWaveEnemiesCleared(room);
        UnlockTeleportAfterWave();
    }

    IEnumerator UpdateInfoText(string message)
    {
        if (uiMinimap == null || uiMinimap.TextInfo == null)
        {
            yield break;
        }

        uiMinimap.TextInfo.text = message;
        yield return new WaitForSeconds(3f);
        uiMinimap.TextInfo.text = "";
    }

}
