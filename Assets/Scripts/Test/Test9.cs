using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using UnityEngine;

public class Test9 : MonoBehaviour
{
    private const int CellEmpty = 0;
    private const int CellRoom = 1;
    private const int CellCorridor = 2;

    private static readonly Vector2Int[] CardinalDirections =
    {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left
    };

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
    [Range(0f, 1f)] public float deadEndKeepChance = 0.85f; // % que dejamos
    [Range(0f, 1f)] public float deadEndConnectChance = 0.15f; // % que conectamos

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

    [Header("UnityEngine.Debug Stats")]
    public int generatedRoomCount;
    public int generatedCorridorCount;
    public int carvedCorridorTiles;

    private BSPNode root;
    private List<Room> rooms = new List<Room>();
    private List<MSTEdge> mstEdges = new List<MSTEdge>();
    private List<MSTEdge> delaunayEdges = new List<MSTEdge>();
    private List<DelaunayTriangle> delaunayTriangles = new List<DelaunayTriangle>();
    private List<List<Vector2Int>> corridorPaths = new List<List<Vector2Int>>();
    private float[,] densityMap;
    private int[,] occupancyGrid;
    private int[,] roomIndexGrid;
    private readonly Dictionary<Vector2Int, Room> roomByCenter = new Dictionary<Vector2Int, Room>();
    private readonly Dictionary<Vector2Int, int> roomIndexByCenter = new Dictionary<Vector2Int, int>();

    void Start()
    {
        Stopwatch totalTimer = Stopwatch.StartNew();
        seed = UnityEngine.Random.Range(0, 100000);

        // Fase 1: Generación del mapa de densidad
        if (usePerlinNoise)
        {
            densityMap = new float[dungeonWidth, dungeonHeight];
            GeneratePerlinNoiseDensityMap();
        }
        
        // Fase 2: Generación BSP
        BSPGenerator generator = new BSPGenerator(minRoomSize, maxRoomSize, seed);
        root = generator.Generate(new IntRect(0, 0, dungeonWidth, dungeonHeight));
        rooms = generator.CreateRooms(root);
        
        // Fase 3: Filtrado por densidad
        if (usePerlinNoise && densityMap != null)
        {
            rooms = FilterRoomsByDensity(rooms);
        }
        
        
        // Fase 4: Construcción de lookups y navegación
        generatedRoomCount = rooms.Count;
        BuildRoomLookups();
        BuildNavigationGrid();
        
        if (rooms.Count > 1)
        {
            // Fase 5: Triangulación Delaunay
            
            GenerateDelaunayTriangulation();
            
            // Fase 6: Árbol de expansión mínima
            GenerateMinimumSpanningTree();
            ControlDeadEnds();
            
            // Fase 7: Generación de corredores
            if (generateCorridors && mstEdges.Count > 0)
            {
                GenerateCorridorsFromGraph();
            }
        }

        totalTimer.Stop();
        UnityEngine.Debug.Log($"[Test9] Generated rooms: {rooms.Count}");
        UnityEngine.Debug.Log($"[Test9] Total generation time: {totalTimer.ElapsedMilliseconds}ms");
    }

    void OnDrawGizmos()
    {
        if (root == null)
        {
            return;
        }

        if (showBSPNodes)
        {
            DrawNodeGizmos(root);
        }

        if (showRooms)
        {
            DrawRoomsGizmos();
        }

        if (showDensityMap && densityMap != null)
        {
            DrawDensityMapGizmos();
        }

        if (delaunayTriangles != null && showDelaunay)
        {
            DrawDelaunayGizmos();
        }

        if (mstEdges != null && showMST)
        {
            DrawMSTGizmos();
        }

        if (showCorridors)
        {
            DrawCorridorGizmos();
        }
    }

    void DrawNodeGizmos(BSPNode node)
    {
        Gizmos.color = Color.white;

        Vector3 center = new Vector3(node.Area.x + node.Area.width / 2f, 0f, node.Area.y + node.Area.height / 2f);
        Vector3 size = new Vector3(node.Area.width, 0f, node.Area.height);
        Gizmos.DrawWireCube(center, size);

        if (!node.IsLeaf)
        {
            DrawNodeGizmos(node.left);
            DrawNodeGizmos(node.right);
        }
    }

    void DrawRoomsGizmos()
    {
        if (rooms == null)
        {
            return;
        }

        Gizmos.color = Color.green;
        foreach (Room room in rooms)
        {
            Vector3 center = new Vector3(room.center.x, 0f, room.center.y);
            Vector3 size = new Vector3(room.bounds.width, 0.1f, room.bounds.height);
            Gizmos.DrawWireCube(center, size);
        }
    }

    void GeneratePerlinNoiseDensityMap()
    {
        for (int x = 0; x < dungeonWidth; x++)
        {
            for (int y = 0; y < dungeonHeight; y++)
            {
                float noiseX = (x + seed * 0.1f) * perlinScale;
                float noiseY = (y + seed * 0.2f) * perlinScale;
                densityMap[x, y] = Mathf.PerlinNoise(noiseX, noiseY);
            }
        }
    }

    List<Room> FilterRoomsByDensity(List<Room> roomsToFilter)
    {
        List<Room> filteredRooms = new List<Room>();

        foreach (Room room in roomsToFilter)
        {
            float avgDensity = 0f;
            int count = 0;

            for (int x = room.bounds.x; x < room.bounds.x + room.bounds.width && x < dungeonWidth; x++)
            {
                for (int y = room.bounds.y; y < room.bounds.y + room.bounds.height && y < dungeonHeight; y++)
                {
                    avgDensity += densityMap[x, y];
                    count++;
                }
            }

            if (count > 0)
            {
                avgDensity /= count;
            }

            if (avgDensity >= densityThreshold)
            {
                filteredRooms.Add(room);
            }
        }

        return filteredRooms;
    }

    void BuildRoomLookups()
    {
        roomByCenter.Clear();
        roomIndexByCenter.Clear();

        for (int i = 0; i < rooms.Count; i++)
        {
            roomByCenter[rooms[i].center] = rooms[i];
            roomIndexByCenter[rooms[i].center] = i;
        }
    }

    void BuildNavigationGrid()
    {
        occupancyGrid = new int[dungeonWidth, dungeonHeight];
        roomIndexGrid = new int[dungeonWidth, dungeonHeight];

        for (int x = 0; x < dungeonWidth; x++)
        {
            for (int y = 0; y < dungeonHeight; y++)
            {
                roomIndexGrid[x, y] = -1;
            }
        }

        for (int i = 0; i < rooms.Count; i++)
        {
            Room room = rooms[i];
            for (int x = room.bounds.x; x < room.bounds.xMax && x < dungeonWidth; x++)
            {
                for (int y = room.bounds.y; y < room.bounds.yMax && y < dungeonHeight; y++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    if (!IsInsideGrid(cell))
                    {
                        continue;
                    }

                    occupancyGrid[x, y] = CellRoom;
                    roomIndexGrid[x, y] = i;
                }
            }
        }
    }

    void GenerateMinimumSpanningTree()
    {
        if (rooms.Count < 2)
        {
            return;
        }

        mstEdges = PrimAlgorithmFromDelaunay(delaunayEdges, rooms);

        int targetExtraEdges = Mathf.RoundToInt(delaunayEdges.Count * extraConnectionFactor);
        int addedCycles = AddCyclesToMST(targetExtraEdges);
    }

    void ControlDeadEnds()
{
    Dictionary<Vector2Int, int> nodeDegree = new Dictionary<Vector2Int, int>();

    foreach (var edge in mstEdges)
    {
        if (!nodeDegree.ContainsKey(edge.p1)) nodeDegree[edge.p1] = 0;
        if (!nodeDegree.ContainsKey(edge.p2)) nodeDegree[edge.p2] = 0;

        nodeDegree[edge.p1]++;
        nodeDegree[edge.p2]++;
    }

    foreach (var node in nodeDegree)
    {
        if (node.Value == 1) // dead-end
        {
            float roll = UnityEngine.Random.value;

            // 🎯 1. DECIDIR SI LO DEJAMOS
            if (roll < deadEndKeepChance)
                continue;

            // 🎯 2. DECIDIR SI LO CONECTAMOS
            if (roll < deadEndKeepChance + deadEndConnectChance)
            {
                var candidates = delaunayEdges
                    .Where(e => e.p1 == node.Key || e.p2 == node.Key)
                    .Where(e => !mstEdges.Any(me =>
                        (me.p1 == e.p1 && me.p2 == e.p2) ||
                        (me.p1 == e.p2 && me.p2 == e.p1)))
                    .OrderBy(e => e.distance)
                    .ToList();

                if (candidates.Count > 0)
                {
                    mstEdges.Add(candidates[0]);
                }
            }
        }
    }
}
    void GenerateDelaunayTriangulation()
    {
        List<Vector2Int> points = rooms.Select(room => room.center).ToList();
        delaunayTriangles = new List<DelaunayTriangle>();
        delaunayEdges = new List<MSTEdge>();

        if (points.Count == 2)
        {
            delaunayEdges.Add(new MSTEdge(points[0], points[1]));
            return;
        }

        delaunayTriangles = BowyerWatsonTriangulation(points);

        HashSet<(Vector2Int, Vector2Int)> uniqueEdges = new HashSet<(Vector2Int, Vector2Int)>();
        foreach (DelaunayTriangle triangle in delaunayTriangles)
        {
            AddEdgeNormalized(uniqueEdges, triangle.p1, triangle.p2);
            AddEdgeNormalized(uniqueEdges, triangle.p2, triangle.p3);
            AddEdgeNormalized(uniqueEdges, triangle.p3, triangle.p1);
        }

        foreach ((Vector2Int, Vector2Int) edge in uniqueEdges)
        {
            delaunayEdges.Add(new MSTEdge(edge.Item1, edge.Item2));
        }

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

        delaunayEdges = delaunayEdges.OrderBy(edge => edge.distance).ToList();
    }

    void GenerateCorridorsFromGraph()
    {
        corridorPaths = new List<List<Vector2Int>>();
        generatedCorridorCount = 0;
        carvedCorridorTiles = 0;

        foreach (MSTEdge edge in mstEdges)
        {
            if (!roomByCenter.TryGetValue(edge.p1, out Room startRoom) ||
                !roomByCenter.TryGetValue(edge.p2, out Room endRoom))
            {
                continue;
            }

            int startRoomIndex = roomIndexByCenter[edge.p1];
            int endRoomIndex = roomIndexByCenter[edge.p2];

            Vector2Int startDoor = GetDoorCell(startRoom, endRoom.center);
            Vector2Int endDoor = GetDoorCell(endRoom, startRoom.center);

            List<Vector2Int> corridorPath = FindPathAStar(startDoor, endDoor, startRoomIndex, endRoomIndex, false);
            if (corridorPath == null)
            {
                corridorPath = FindPathAStar(startDoor, endDoor, startRoomIndex, endRoomIndex, true);
            }

            if (corridorPath == null || corridorPath.Count == 0)
            {
                continue;
            }

            CarveCorridor(corridorPath, startRoomIndex, endRoomIndex);
            corridorPaths.Add(new List<Vector2Int>(corridorPath));
        }

        generatedCorridorCount = corridorPaths.Count;
    }

    Vector2Int GetDoorCell(Room room, Vector2Int target)
    {
        int minX = room.bounds.x;
        int maxX = room.bounds.xMax - 1;
        int minY = room.bounds.y;
        int maxY = room.bounds.yMax - 1;

        int dx = target.x - room.center.x;
        int dy = target.y - room.center.y;

        // Determinar cuál pared está más cerca del objetivo
        if (Mathf.Abs(dx) >= Mathf.Abs(dy))
        {
            // Pared izquierda o derecha
            int x = dx >= 0 ? maxX : minX;
            int y = (minY + maxY) / 2; // Centro de la pared
            return new Vector2Int(x, y);
        }
        else
        {
            // Pared superior o inferior
            int x = (minX + maxX) / 2; // Centro de la pared
            int y = dy >= 0 ? maxY : minY;
            return new Vector2Int(x, y);
        }
    }

    List<Vector2Int> FindPathAStar(Vector2Int start, Vector2Int goal, int startRoomIndex, int endRoomIndex, bool allowRoomPenalty)
    {
        if (!IsInsideGrid(start) || !IsInsideGrid(goal))
        {
            return null;
        }

        List<Vector2Int> openSet = new List<Vector2Int> { start };
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        Dictionary<Vector2Int, float> gScore = new Dictionary<Vector2Int, float> { [start] = 0f };
        Dictionary<Vector2Int, float> fScore = new Dictionary<Vector2Int, float> { [start] = Heuristic(start, goal) };

        while (openSet.Count > 0)
        {
            Vector2Int current = GetLowestFScore(openSet, fScore);
            if (current == goal)
            {
                return ReconstructPath(cameFrom, current);
            }

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (Vector2Int direction in CardinalDirections)
            {
                Vector2Int neighbor = current + direction;

                if (closedSet.Contains(neighbor) || !IsInsideGrid(neighbor))
                {
                    continue;
                }

                if (!TryGetTraversalCost(neighbor, startRoomIndex, endRoomIndex, allowRoomPenalty, out float traversalCost))
                {
                    continue;
                }
                
                float noise = Mathf.PerlinNoise(neighbor.x * 0.08f, neighbor.y * 0.08f);

                float tentativeScore =
                    gScore[current]
                    + traversalCost
                    + GetTurnPenalty(cameFrom, current, neighbor)
                    + noise * 0.8f;


                if (gScore.TryGetValue(neighbor, out float existingScore) && tentativeScore >= existingScore)
                {
                    continue;
                }

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeScore;
                fScore[neighbor] = tentativeScore + Heuristic(neighbor, goal);

                if (!openSet.Contains(neighbor))
                {
                    openSet.Add(neighbor);
                }
            }
        }

        return null;
    }

    bool TryGetTraversalCost(Vector2Int cell, int startRoomIndex, int endRoomIndex, bool allowRoomPenalty, out float cost)
    {
        cost = 1f;

        int roomIndex = roomIndexGrid[cell.x, cell.y];
        if (roomIndex >= 0)
        {
            bool isTerminalRoom = roomIndex == startRoomIndex || roomIndex == endRoomIndex;
            if (!isTerminalRoom)
            {
                if (!allowRoomPenalty)
                {
                    return false;
                }

                cost = roomCrossPenalty;
                return true;
            }

            cost = 1f;
            return true;
        }

        if (occupancyGrid[cell.x, cell.y] == CellCorridor)
        {
            cost = existingCorridorCost;
        }

        return true;
    }

    float GetTurnPenalty(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current, Vector2Int next)
    {
        if (!cameFrom.TryGetValue(current, out Vector2Int previous))
        {
            return 0f;
        }

        Vector2Int previousDirection = current - previous;
        Vector2Int newDirection = next - current;
        return previousDirection == newDirection ? 0f : turnPenalty;
    }

    void CarveCorridor(List<Vector2Int> path, int startRoomIndex, int endRoomIndex)
    {
        foreach (Vector2Int cell in path)
        {
            int roomIndex = roomIndexGrid[cell.x, cell.y];
            if (roomIndex >= 0 && roomIndex != startRoomIndex && roomIndex != endRoomIndex)
            {
                continue;
            }

            if (occupancyGrid[cell.x, cell.y] == CellEmpty)
            {
                occupancyGrid[cell.x, cell.y] = CellCorridor;
                carvedCorridorTiles++;
            }
        }
    }

    List<Vector2Int> SimplifyPath(List<Vector2Int> path)
    {
        if (path.Count <= 2)
        {
            return new List<Vector2Int>(path);
        }

        List<Vector2Int> simplified = new List<Vector2Int> { path[0] };
        Vector2Int lastDirection = path[1] - path[0];

        for (int i = 1; i < path.Count - 1; i++)
        {
            Vector2Int currentDirection = path[i + 1] - path[i];
            if (currentDirection != lastDirection)
            {
                simplified.Add(path[i]);
                lastDirection = currentDirection;
            }
        }

        simplified.Add(path[path.Count - 1]);
        return simplified;
    }

    Vector2Int GetLowestFScore(List<Vector2Int> openSet, Dictionary<Vector2Int, float> fScore)
    {
        Vector2Int best = openSet[0];
        float bestScore = fScore.TryGetValue(best, out float score) ? score : float.MaxValue;

        for (int i = 1; i < openSet.Count; i++)
        {
            Vector2Int candidate = openSet[i];
            float candidateScore = fScore.TryGetValue(candidate, out float value) ? value : float.MaxValue;
            if (candidateScore < bestScore)
            {
                best = candidate;
                bestScore = candidateScore;
            }
        }

        return best;
    }

    float Heuristic(Vector2Int current, Vector2Int goal)
    {
        return Mathf.Abs(current.x - goal.x) + Mathf.Abs(current.y - goal.y);
    }

    List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
    {
        List<Vector2Int> path = new List<Vector2Int> { current };

        while (cameFrom.TryGetValue(current, out Vector2Int previous))
        {
            current = previous;
            path.Add(current);
        }

        path.Reverse();
        return path;
    }

    bool IsInsideGrid(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < dungeonWidth && cell.y >= 0 && cell.y < dungeonHeight;
    }

    void AddEdgeNormalized(HashSet<(Vector2Int, Vector2Int)> edges, Vector2Int p1, Vector2Int p2)
    {
        if (p1.x > p2.x || (p1.x == p2.x && p1.y > p2.y))
        {
            Vector2Int temp = p1;
            p1 = p2;
            p2 = temp;
        }

        edges.Add((p1, p2));
    }

    List<MSTEdge> PrimAlgorithmFromDelaunay(List<MSTEdge> edges, List<Room> roomList)
    {
        List<MSTEdge> mst = new List<MSTEdge>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        List<Vector2Int> points = roomList.Select(room => room.center).ToList();

        if (points.Count == 0)
        {
            return mst;
        }

        visited.Add(points[0]);

        while (visited.Count < points.Count)
        {
            float minDistance = float.MaxValue;
            MSTEdge bestEdge = default;
            bool foundEdge = false;

            foreach (MSTEdge edge in edges)
            {
                bool p1Visited = visited.Contains(edge.p1);
                bool p2Visited = visited.Contains(edge.p2);

                if ((p1Visited && !p2Visited) || (!p1Visited && p2Visited))
                {
                    if (edge.distance < minDistance)
                    {
                        minDistance = edge.distance;
                        bestEdge = edge;
                        foundEdge = true;
                    }
                }
            }

            if (!foundEdge)
            {
                break;
            }

            mst.Add(bestEdge);
            visited.Add(visited.Contains(bestEdge.p1) ? bestEdge.p2 : bestEdge.p1);
        }

        return mst;
    }

    List<DelaunayTriangle> BowyerWatsonTriangulation(List<Vector2Int> points)
    {
        List<DelaunayTriangle> triangles = new List<DelaunayTriangle>();

        if (points.Count < 3)
        {
            return triangles;
        }

        Vector2Int minPoint = new Vector2Int(int.MaxValue, int.MaxValue);
        Vector2Int maxPoint = new Vector2Int(int.MinValue, int.MinValue);

        foreach (Vector2Int point in points)
        {
            minPoint = new Vector2Int(Mathf.Min(minPoint.x, point.x), Mathf.Min(minPoint.y, point.y));
            maxPoint = new Vector2Int(Mathf.Max(maxPoint.x, point.x), Mathf.Max(maxPoint.y, point.y));
        }

        int deltaMax = Mathf.Max(maxPoint.x - minPoint.x, maxPoint.y - minPoint.y);
        Vector2Int mid = new Vector2Int((minPoint.x + maxPoint.x) / 2, (minPoint.y + maxPoint.y) / 2);

        Vector2Int p1 = new Vector2Int(mid.x, mid.y + 2 * deltaMax);
        Vector2Int p2 = new Vector2Int(mid.x - 2 * deltaMax, mid.y - deltaMax);
        Vector2Int p3 = new Vector2Int(mid.x + 2 * deltaMax, mid.y - deltaMax);

        triangles.Add(new DelaunayTriangle(p1, p2, p3));

        foreach (Vector2Int point in points)
        {
            List<DelaunayTriangle> badTriangles = new List<DelaunayTriangle>();

            foreach (DelaunayTriangle triangle in triangles)
            {
                if (IsPointInCircumcircle(point, triangle))
                {
                    badTriangles.Add(triangle);
                }
            }

            HashSet<(Vector2Int, Vector2Int)> polygon = new HashSet<(Vector2Int, Vector2Int)>();
            foreach (DelaunayTriangle triangle in badTriangles)
            {
                AddEdgeToPolygon(polygon, triangle.p1, triangle.p2);
                AddEdgeToPolygon(polygon, triangle.p2, triangle.p3);
                AddEdgeToPolygon(polygon, triangle.p3, triangle.p1);
            }

            foreach (DelaunayTriangle triangle in badTriangles)
            {
                triangles.Remove(triangle);
            }

            foreach ((Vector2Int, Vector2Int) edge in polygon)
            {
                triangles.Add(new DelaunayTriangle(edge.Item1, edge.Item2, point));
            }
        }

        triangles.RemoveAll(triangle =>
            triangle.p1 == p1 || triangle.p1 == p2 || triangle.p1 == p3 ||
            triangle.p2 == p1 || triangle.p2 == p2 || triangle.p2 == p3 ||
            triangle.p3 == p1 || triangle.p3 == p2 || triangle.p3 == p3);

        return triangles;
    }

    void AddEdgeToPolygon(HashSet<(Vector2Int, Vector2Int)> polygon, Vector2Int p1, Vector2Int p2)
    {
        (Vector2Int, Vector2Int) edge = (p1, p2);
        (Vector2Int, Vector2Int) reverseEdge = (p2, p1);

        if (polygon.Contains(reverseEdge))
        {
            polygon.Remove(reverseEdge);
        }
        else
        {
            polygon.Add(edge);
        }
    }

    bool IsPointInCircumcircle(Vector2Int point, DelaunayTriangle triangle)
    {
        float ax = triangle.p1.x;
        float ay = triangle.p1.y;
        float bx = triangle.p2.x;
        float by = triangle.p2.y;
        float cx = triangle.p3.x;
        float cy = triangle.p3.y;
        float px = point.x;
        float py = point.y;

        float determinant = 2f * (ax * (by - cy) + bx * (cy - ay) + cx * (ay - by));
        if (Mathf.Abs(determinant) < 0.0001f)
        {
            return false;
        }

        float ux = ((ax * ax + ay * ay) * (by - cy) + (bx * bx + by * by) * (cy - ay) + (cx * cx + cy * cy) * (ay - by)) / determinant;
        float uy = ((ax * ax + ay * ay) * (cx - bx) + (bx * bx + by * by) * (ax - cx) + (cx * cx + cy * cy) * (bx - ax)) / determinant;

        float radiusSq = (ax - ux) * (ax - ux) + (ay - uy) * (ay - uy);
        float distSq = (px - ux) * (px - ux) + (py - uy) * (py - uy);
        return distSq <= radiusSq + 0.01f;
    }

    int AddCyclesToMST(int cycleCount)
    {
        if (delaunayEdges == null || delaunayEdges.Count == 0)
        {
            return 0;
        }

        HashSet<(Vector2Int, Vector2Int)> existingEdges =
            new HashSet<(Vector2Int, Vector2Int)>(mstEdges.Select(edge => NormalizeEdgeTuple(edge.p1, edge.p2)));
        Dictionary<Vector2Int, List<(Vector2Int, float)>> currentGraph = BuildEdgeGraph(mstEdges);

        int added = 0;
        while (added < cycleCount)
        {
            var candidateEdges = delaunayEdges
                .Where(edge => !existingEdges.Contains(NormalizeEdgeTuple(edge.p1, edge.p2)))
                .Select(edge => new { Edge = edge, Score = EvaluateLoopEdge(edge, currentGraph) })
                .Where(candidate => candidate.Score > 0f)
                .OrderByDescending(candidate => candidate.Score)
                .ToList();

            if (candidateEdges.Count == 0)
            {
                break;
            }

            MSTEdge best = candidateEdges[0].Edge;
            mstEdges.Add(best);
            existingEdges.Add(NormalizeEdgeTuple(best.p1, best.p2));
            AddEdgeToGraph(currentGraph, best);
            added++;
        }

        return added;
    }

    float EvaluateLoopEdge(MSTEdge edge, Dictionary<Vector2Int, List<(Vector2Int, float)>> graph)
    {
        float pathDistance = GetPathDistance(graph, edge.p1, edge.p2);
        if (pathDistance == float.MaxValue || edge.distance <= 0.001f)
        {
            return 0f;
        }

        float graphRatio = pathDistance / edge.distance;
        if (graphRatio < minGraphDistanceThreshold)
        {
            return 0f;
        }

        float improvement = pathDistance - edge.distance;
        float distanceFactor = improvement > 0f ? graphRatio : 0.5f;
        float euclideanFactor = Mathf.Log(1f + edge.distance);
        float combinedScore = (distanceFactor * loopQualityBias) + (euclideanFactor * (1f - loopQualityBias));
        float randomMultiplier = UnityEngine.Random.Range(1f - randomnessFactor, 1f + randomnessFactor);

        return combinedScore * randomMultiplier;
    }

    Dictionary<Vector2Int, List<(Vector2Int, float)>> BuildEdgeGraph(IEnumerable<MSTEdge> edges)
    {
        Dictionary<Vector2Int, List<(Vector2Int, float)>> graph = new Dictionary<Vector2Int, List<(Vector2Int, float)>>();

        foreach (MSTEdge edge in edges)
        {
            if (!graph.ContainsKey(edge.p1))
            {
                graph[edge.p1] = new List<(Vector2Int, float)>();
            }

            if (!graph.ContainsKey(edge.p2))
            {
                graph[edge.p2] = new List<(Vector2Int, float)>();
            }

            graph[edge.p1].Add((edge.p2, edge.distance));
            graph[edge.p2].Add((edge.p1, edge.distance));
        }

        return graph;
    }

    void AddEdgeToGraph(Dictionary<Vector2Int, List<(Vector2Int, float)>> graph, MSTEdge edge)
    {
        if (!graph.ContainsKey(edge.p1))
        {
            graph[edge.p1] = new List<(Vector2Int, float)>();
        }

        if (!graph.ContainsKey(edge.p2))
        {
            graph[edge.p2] = new List<(Vector2Int, float)>();
        }

        graph[edge.p1].Add((edge.p2, edge.distance));
        graph[edge.p2].Add((edge.p1, edge.distance));
    }

    float GetPathDistance(Dictionary<Vector2Int, List<(Vector2Int, float)>> graph, Vector2Int start, Vector2Int target)
    {
        if (!graph.ContainsKey(start) || !graph.ContainsKey(target))
        {
            return float.MaxValue;
        }

        Dictionary<Vector2Int, float> distances = graph.Keys.ToDictionary(node => node, _ => float.MaxValue);
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        distances[start] = 0f;

        while (visited.Count < graph.Count)
        {
            Vector2Int current = default;
            float currentDist = float.MaxValue;

            foreach (Vector2Int node in graph.Keys)
            {
                if (visited.Contains(node))
                {
                    continue;
                }

                if (distances[node] < currentDist)
                {
                    currentDist = distances[node];
                    current = node;
                }
            }

            if (currentDist == float.MaxValue)
            {
                break;
            }

            if (current == target)
            {
                return currentDist;
            }

            visited.Add(current);

            foreach ((Vector2Int, float) connection in graph[current])
            {
                Vector2Int neighbor = connection.Item1;
                float weight = connection.Item2;

                if (visited.Contains(neighbor))
                {
                    continue;
                }

                float nextDist = currentDist + weight;
                if (nextDist < distances[neighbor])
                {
                    distances[neighbor] = nextDist;
                }
            }
        }

        return distances[target];
    }

    (Vector2Int, Vector2Int) NormalizeEdgeTuple(Vector2Int a, Vector2Int b)
    {
        if (a.x > b.x || (a.x == b.x && a.y > b.y))
        {
            return (b, a);
        }

        return (a, b);
    }

    void DrawMSTGizmos()
    {
        if (mstEdges == null || mstEdges.Count == 0)
        {
            return;
        }

        Gizmos.color = new Color(1f, 0f, 0f, 0.8f);
        foreach (MSTEdge edge in mstEdges)
        {
            Vector3 p1 = new Vector3(edge.p1.x, 0.03f, edge.p1.y);
            Vector3 p2 = new Vector3(edge.p2.x, 0.03f, edge.p2.y);
            Gizmos.DrawLine(p1, p2);
        }
    }

    void DrawDelaunayGizmos()
    {
        if (delaunayTriangles == null || delaunayTriangles.Count == 0)
        {
            return;
        }

        Gizmos.color = new Color(0.1f, 0.25f, 0.85f, 0.8f);
        foreach (DelaunayTriangle triangle in delaunayTriangles)
        {
            Vector3 p1 = new Vector3(triangle.p1.x, 0.02f, triangle.p1.y);
            Vector3 p2 = new Vector3(triangle.p2.x, 0.02f, triangle.p2.y);
            Vector3 p3 = new Vector3(triangle.p3.x, 0.02f, triangle.p3.y);

            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawLine(p2, p3);
            Gizmos.DrawLine(p3, p1);
        }
    }

    void DrawCorridorGizmos()
    {
        if (corridorPaths == null || corridorPaths.Count == 0)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        foreach (List<Vector2Int> path in corridorPaths)
        {
            if (showCorridorCells)
            {
                foreach (Vector2Int cell in path)
                {
                    Gizmos.DrawCube(new Vector3(cell.x, 0.05f, cell.y), new Vector3(0.7f, 0.1f, 0.7f));
                }
            }

            List<Vector2Int> previewPath = SimplifyPath(path);
            for (int i = 0; i < previewPath.Count - 1; i++)
            {
                Vector3 from = new Vector3(previewPath[i].x, 0.06f, previewPath[i].y);
                Vector3 to = new Vector3(previewPath[i + 1].x, 0.06f, previewPath[i + 1].y);
                Gizmos.DrawLine(from, to);
            }
        }
    }

    void DrawDensityMapGizmos()
    {
        if (densityMap == null)
        {
            return;
        }

        for (int x = 0; x < dungeonWidth; x++)
        {
            for (int y = 0; y < dungeonHeight; y++)
            {
                float density = densityMap[x, y];
                float alpha = density >= densityThreshold ? 0.3f : 0.1f;
                Color densityColor = Color.Lerp(new Color(1f, 0f, 0f, alpha), new Color(0f, 1f, 0f, alpha), density);
                Gizmos.color = densityColor;

                Vector3 pos = new Vector3(x, 0.01f, y);
                Gizmos.DrawCube(pos, Vector3.one * 0.8f);
            }
        }
    }
}
