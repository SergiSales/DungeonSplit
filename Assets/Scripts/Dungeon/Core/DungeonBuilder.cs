using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DungeonBuilder : MonoBehaviour
{
    public int dungeonWidth;
    public int dungeonHeight;
    public int minRoomSize;
    public int maxRoomSize;
    public int seed;
    public bool usePerlinNoise;
    public float perlinScale;
    public float densityThreshold;
    public float loopQualityBias;
    public float randomnessFactor;
    public float minGraphDistanceThreshold;
    public float extraConnectionFactor;
    public float deadEndKeepChance;
    public float deadEndConnectChance;

    public DungeonBuildResult Build()
    {
        List<Room> rooms = GenerateRooms();
        List<MSTEdge> mstEdges = GenerateConnections(rooms);

        DungeonBuildResult result = GetComponent<DungeonBuildResult>();
        if (result == null)
        {
            result = gameObject.AddComponent<DungeonBuildResult>();
        }

        
        result.Rooms = rooms ?? new List<Room>();
        result.MstEdges = mstEdges ?? new List<MSTEdge>();
        return result;
    }

    private List<Room> GenerateRooms()
    {
        float[,] densityMap = null;
        if (usePerlinNoise)
        {
            DensityMapGenerator densityGenerator = new DensityMapGenerator(dungeonWidth, dungeonHeight, seed);
            densityMap = densityGenerator.GeneratePerlinNoise(perlinScale);
        }

        BSPGenerator bspGenerator = new BSPGenerator(minRoomSize, maxRoomSize, seed);
        BSPNode root = bspGenerator.Generate(new IntRect(0, 0, dungeonWidth, dungeonHeight));
        List<Room> rooms = bspGenerator.CreateRooms(root);

        if (usePerlinNoise && densityMap != null)
        {
            DensityMapGenerator densityGenerator = new DensityMapGenerator(dungeonWidth, dungeonHeight, seed);
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

        DelaunayGenerator delaunayGenerator = new DelaunayGenerator();
        MSTGenerator mstGenerator = new MSTGenerator();
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
