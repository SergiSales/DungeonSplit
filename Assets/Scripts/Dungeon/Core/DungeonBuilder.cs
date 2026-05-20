using System.Collections.Generic;
using UnityEngine;

public sealed class DungeonBuilder
{
    private readonly int dungeonWidth;
    private readonly int dungeonHeight;
    private readonly int minRoomSize;
    private readonly int maxRoomSize;
    private readonly int seed;
    private readonly bool usePerlinNoise;
    private readonly float perlinScale;
    private readonly float densityThreshold;
    private readonly float loopQualityBias;
    private readonly float randomnessFactor;
    private readonly float minGraphDistanceThreshold;
    private readonly float extraConnectionFactor;
    private readonly float deadEndKeepChance;
    private readonly float deadEndConnectChance;
    private readonly DensityMapGenerator densityGenerator;
    private readonly DelaunayGenerator delaunayGenerator;
    private readonly MSTGenerator mstGenerator;

    public DungeonBuilder(
        int dungeonWidth,
        int dungeonHeight,
        int minRoomSize,
        int maxRoomSize,
        int seed,
        bool usePerlinNoise,
        float perlinScale,
        float densityThreshold,
        float loopQualityBias,
        float randomnessFactor,
        float minGraphDistanceThreshold,
        float extraConnectionFactor,
        float deadEndKeepChance,
        float deadEndConnectChance)
    {
        this.dungeonWidth = dungeonWidth;
        this.dungeonHeight = dungeonHeight;
        this.minRoomSize = minRoomSize;
        this.maxRoomSize = maxRoomSize;
        this.seed = seed;
        this.usePerlinNoise = usePerlinNoise;
        this.perlinScale = perlinScale;
        this.densityThreshold = densityThreshold;
        this.loopQualityBias = loopQualityBias;
        this.randomnessFactor = randomnessFactor;
        this.minGraphDistanceThreshold = minGraphDistanceThreshold;
        this.extraConnectionFactor = extraConnectionFactor;
        this.deadEndKeepChance = deadEndKeepChance;
        this.deadEndConnectChance = deadEndConnectChance;
        densityGenerator = new DensityMapGenerator(dungeonWidth, dungeonHeight, seed);
        delaunayGenerator = new DelaunayGenerator();
        mstGenerator = new MSTGenerator();
    }

    public DungeonBuildResult Build()
    {
        List<Room> rooms = GenerateRooms();
        List<MSTEdge> mstEdges = GenerateConnections(rooms);
        return new DungeonBuildResult(rooms, mstEdges);
    }

    private List<Room> GenerateRooms()
    {
        float[,] densityMap = null;
        if (usePerlinNoise)
        {
            densityMap = densityGenerator.GeneratePerlinNoise(perlinScale);
        }

        BSPGenerator bspGenerator = new BSPGenerator(minRoomSize, maxRoomSize, seed);
        BSPNode root = bspGenerator.Generate(new IntRect(0, 0, dungeonWidth, dungeonHeight));
        List<Room> rooms = bspGenerator.CreateRooms(root);

        if (usePerlinNoise && densityMap != null)
        {
            rooms = densityGenerator.FilterRoomsByDensity(rooms, densityMap, densityThreshold);
        }

        return rooms;
    }

    private List<MSTEdge> GenerateConnections(List<Room> rooms)
    {
        List<MSTEdge> mstEdges = new List<MSTEdge>();
        if (rooms == null || rooms.Count <= 1)
        {
            return mstEdges;
        }

        List<Vector2Int> points = new List<Vector2Int>(rooms.Count);
        foreach (Room room in rooms)
        {
            points.Add(room.center);
        }

        List<DelaunayTriangle> delaunayTriangles = delaunayGenerator.GenerateTriangulation(points);
        List<MSTEdge> delaunayEdges = delaunayGenerator.ExtractEdgesFromTriangles(delaunayTriangles);
        if (delaunayEdges.Count == 0)
        {
            delaunayEdges = BuildFallbackEdges(points);
        }

        delaunayEdges.Sort((a, b) => a.distance.CompareTo(b.distance));
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

        return mstEdges;
    }

    private static List<MSTEdge> BuildFallbackEdges(List<Vector2Int> points)
    {
        List<MSTEdge> fallbackEdges = new List<MSTEdge>();
        for (int i = 0; i < points.Count; i++)
        {
            for (int j = i + 1; j < points.Count; j++)
            {
                fallbackEdges.Add(new MSTEdge(points[i], points[j]));
            }
        }

        return fallbackEdges;
    }
}
