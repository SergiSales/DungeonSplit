using System.Collections.Generic;
using UnityEngine;

public class CorridorGenerator
{
    private const int CellEmpty = 0;
    private const int CellCorridor = 2;
    private const int LocalSearchPadding = 6;

    public void GenerateCorridors(List<MSTEdge> mstEdges, Dictionary<Vector2Int, Room> roomByCenter,
        Dictionary<Vector2Int, int> roomIndexByCenter, int[,] occupancyGrid, int[,] roomIndexGrid,
        int dungeonWidth, int dungeonHeight, float roomCrossPenalty, float turnPenalty,
        float existingCorridorCost, out List<List<Vector2Int>> corridorPaths, out int carvedTiles)
    {
        corridorPaths = new List<List<Vector2Int>>();
        carvedTiles = 0;

        PathfindingAStar pathfinder = new PathfindingAStar();
        float[,] traversalNoise = BuildTraversalNoiseMap(dungeonWidth, dungeonHeight);

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

            List<Vector2Int> corridorPath = GenerateLShapedCorridor(
                startDoor,
                endDoor,
                startRoomIndex,
                endRoomIndex,
                occupancyGrid,
                roomIndexGrid,
                dungeonWidth,
                dungeonHeight,
                roomCrossPenalty,
                turnPenalty,
                existingCorridorCost,
                pathfinder,
                traversalNoise);

            if (corridorPath == null || corridorPath.Count == 0)
            {
                continue;
            }

            corridorPath = TrimTerminalRoomCells(corridorPath, startRoomIndex, endRoomIndex, roomIndexGrid);

            if (corridorPath.Count == 0)
            {
                continue;
            }

            carvedTiles += CarveCorridor(corridorPath, startRoomIndex, endRoomIndex, occupancyGrid, roomIndexGrid);
            corridorPaths.Add(new List<Vector2Int>(corridorPath));
        }
    }

    private List<Vector2Int> GenerateLShapedCorridor(Vector2Int startDoor, Vector2Int endDoor, int startRoomIndex,
        int endRoomIndex, int[,] occupancyGrid, int[,] roomIndexGrid, int dungeonWidth, int dungeonHeight,
        float roomCrossPenalty, float turnPenalty, float existingCorridorCost, PathfindingAStar pathfinder,
        float[,] traversalNoise)
    {
        int dx = endDoor.x - startDoor.x;
        int dy = endDoor.y - startDoor.y;
        bool isHorizontalFirst = Mathf.Abs(dx) >= Mathf.Abs(dy);

        Vector2Int intermediatePoint = isHorizontalFirst
            ? new Vector2Int(endDoor.x, startDoor.y)
            : new Vector2Int(startDoor.x, endDoor.y);

        if (TryBuildStraightCorridor(startDoor, intermediatePoint, startRoomIndex, endRoomIndex,
                roomIndexGrid, out List<Vector2Int> straightPart1) &&
            TryBuildStraightCorridor(intermediatePoint, endDoor, startRoomIndex, endRoomIndex,
                roomIndexGrid, out List<Vector2Int> straightPart2))
        {
            return CombinePathParts(straightPart1, straightPart2);
        }

        PathfindingAStar.SearchBounds localBounds = CreateSearchBounds(startDoor, endDoor, dungeonWidth, dungeonHeight);

        List<Vector2Int> pathPart1 = FindPathWithFallback(
            pathfinder,
            startDoor,
            intermediatePoint,
            startRoomIndex,
            endRoomIndex,
            startDoor,
            endDoor,
            occupancyGrid,
            roomIndexGrid,
            dungeonWidth,
            dungeonHeight,
            roomCrossPenalty,
            turnPenalty,
            existingCorridorCost,
            traversalNoise,
            localBounds);

        if (pathPart1 == null || pathPart1.Count == 0)
        {
            return FindPathWithFallback(
                pathfinder,
                startDoor,
                endDoor,
                startRoomIndex,
                endRoomIndex,
                startDoor,
                endDoor,
                occupancyGrid,
                roomIndexGrid,
                dungeonWidth,
                dungeonHeight,
                roomCrossPenalty,
                turnPenalty,
                existingCorridorCost,
                traversalNoise,
                localBounds);
        }

        List<Vector2Int> pathPart2 = FindPathWithFallback(
            pathfinder,
            intermediatePoint,
            endDoor,
            startRoomIndex,
            endRoomIndex,
            startDoor,
            endDoor,
            occupancyGrid,
            roomIndexGrid,
            dungeonWidth,
            dungeonHeight,
            roomCrossPenalty,
            turnPenalty,
            existingCorridorCost,
            traversalNoise,
            localBounds);

        if (pathPart2 == null || pathPart2.Count == 0)
        {
            return pathPart1;
        }

        return CombinePathParts(pathPart1, pathPart2);
    }

    private List<Vector2Int> FindPathWithFallback(PathfindingAStar pathfinder, Vector2Int start, Vector2Int goal,
        int startRoomIndex, int endRoomIndex, Vector2Int startRoomAccessCell, Vector2Int endRoomAccessCell,
        int[,] occupancyGrid, int[,] roomIndexGrid, int dungeonWidth, int dungeonHeight,
        float roomCrossPenalty, float turnPenalty, float existingCorridorCost, float[,] traversalNoise,
        PathfindingAStar.SearchBounds localBounds)
    {
        List<Vector2Int> boundedPath = pathfinder.FindPath(
            start,
            goal,
            startRoomIndex,
            endRoomIndex,
            startRoomAccessCell,
            endRoomAccessCell,
            occupancyGrid,
            roomIndexGrid,
            dungeonWidth,
            dungeonHeight,
            roomCrossPenalty,
            turnPenalty,
            existingCorridorCost,
            false,
            traversalNoise,
            localBounds);

        if (boundedPath != null)
        {
            return boundedPath;
        }

        boundedPath = pathfinder.FindPath(
            start,
            goal,
            startRoomIndex,
            endRoomIndex,
            startRoomAccessCell,
            endRoomAccessCell,
            occupancyGrid,
            roomIndexGrid,
            dungeonWidth,
            dungeonHeight,
            roomCrossPenalty,
            turnPenalty,
            existingCorridorCost,
            true,
            traversalNoise,
            localBounds);

        if (boundedPath != null)
        {
            return boundedPath;
        }

        List<Vector2Int> globalPath = pathfinder.FindPath(
            start,
            goal,
            startRoomIndex,
            endRoomIndex,
            startRoomAccessCell,
            endRoomAccessCell,
            occupancyGrid,
            roomIndexGrid,
            dungeonWidth,
            dungeonHeight,
            roomCrossPenalty,
            turnPenalty,
            existingCorridorCost,
            false,
            traversalNoise);

        if (globalPath != null)
        {
            return globalPath;
        }

        return pathfinder.FindPath(
            start,
            goal,
            startRoomIndex,
            endRoomIndex,
            startRoomAccessCell,
            endRoomAccessCell,
            occupancyGrid,
            roomIndexGrid,
            dungeonWidth,
            dungeonHeight,
            roomCrossPenalty,
            turnPenalty,
            existingCorridorCost,
            true,
            traversalNoise);
    }

    private bool TryBuildStraightCorridor(Vector2Int start, Vector2Int end, int startRoomIndex, int endRoomIndex,
        int[,] roomIndexGrid, out List<Vector2Int> path)
    {
        path = null;

        int stepX = 0;
        int stepY = 0;

        if (start.x == end.x)
        {
            stepY = start.y <= end.y ? 1 : -1;
        }
        else if (start.y == end.y)
        {
            stepX = start.x <= end.x ? 1 : -1;
        }
        else
        {
            return false;
        }

        path = new List<Vector2Int>();
        Vector2Int current = start;

        while (true)
        {
            int roomIndex = roomIndexGrid[current.x, current.y];
            if (roomIndex >= 0 &&
                roomIndex != startRoomIndex &&
                roomIndex != endRoomIndex)
            {
                path = null;
                return false;
            }

            path.Add(current);
            if (current == end)
            {
                return true;
            }

            current = new Vector2Int(current.x + stepX, current.y + stepY);
        }
    }

    private PathfindingAStar.SearchBounds CreateSearchBounds(Vector2Int start, Vector2Int end, int dungeonWidth, int dungeonHeight)
    {
        int minX = Mathf.Max(0, Mathf.Min(start.x, end.x) - LocalSearchPadding);
        int minY = Mathf.Max(0, Mathf.Min(start.y, end.y) - LocalSearchPadding);
        int maxX = Mathf.Min(dungeonWidth - 1, Mathf.Max(start.x, end.x) + LocalSearchPadding);
        int maxY = Mathf.Min(dungeonHeight - 1, Mathf.Max(start.y, end.y) + LocalSearchPadding);

        return new PathfindingAStar.SearchBounds(minX, minY, maxX, maxY);
    }

    private List<Vector2Int> CombinePathParts(List<Vector2Int> pathPart1, List<Vector2Int> pathPart2)
    {
        List<Vector2Int> fullPath = new List<Vector2Int>(pathPart1);
        for (int i = 1; i < pathPart2.Count; i++)
        {
            fullPath.Add(pathPart2[i]);
        }

        return fullPath;
    }

    private float[,] BuildTraversalNoiseMap(int dungeonWidth, int dungeonHeight)
    {
        float[,] noiseMap = new float[dungeonWidth, dungeonHeight];

        for (int x = 0; x < dungeonWidth; x++)
        {
            for (int y = 0; y < dungeonHeight; y++)
            {
                noiseMap[x, y] = Mathf.PerlinNoise(x * 0.08f, y * 0.08f);
            }
        }

        return noiseMap;
    }

    private Vector2Int GetDoorCell(Room room, Vector2Int target)
    {
        int dx = target.x - room.center.x;
        int dy = target.y - room.center.y;

        if (Mathf.Abs(dx) >= Mathf.Abs(dy))
        {
            return dx >= 0 ? room.derecha : room.izquierda;
        }

        return dy >= 0 ? room.abajo : room.arriba;
    }

    private List<Vector2Int> TrimTerminalRoomCells(List<Vector2Int> path, int startRoomIndex, int endRoomIndex, int[,] roomIndexGrid)
    {
        if (path == null || path.Count == 0)
        {
            return path;
        }

        int startTrimIndex = 0;
        while (startTrimIndex + 1 < path.Count &&
            roomIndexGrid[path[startTrimIndex].x, path[startTrimIndex].y] == startRoomIndex &&
            roomIndexGrid[path[startTrimIndex + 1].x, path[startTrimIndex + 1].y] == startRoomIndex)
        {
            startTrimIndex++;
        }

        int endTrimIndex = path.Count - 1;
        while (endTrimIndex - 1 >= startTrimIndex &&
            roomIndexGrid[path[endTrimIndex].x, path[endTrimIndex].y] == endRoomIndex &&
            roomIndexGrid[path[endTrimIndex - 1].x, path[endTrimIndex - 1].y] == endRoomIndex)
        {
            endTrimIndex--;
        }

        int trimmedCount = endTrimIndex - startTrimIndex + 1;
        if (trimmedCount <= 0)
        {
            return new List<Vector2Int>();
        }

        return path.GetRange(startTrimIndex, trimmedCount);
    }

    private int CarveCorridor(List<Vector2Int> path, int startRoomIndex, int endRoomIndex,
        int[,] occupancyGrid, int[,] roomIndexGrid)
    {
        int carved = 0;
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
                carved++;
            }
        }

        return carved;
    }

    public List<Vector2Int> SimplifyPath(List<Vector2Int> path)
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
}
