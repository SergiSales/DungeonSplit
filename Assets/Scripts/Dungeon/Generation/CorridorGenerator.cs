using System.Collections.Generic;
using UnityEngine;

public class CorridorGenerator
{
    private const int CellEmpty = 0;
    private const int CellRoom = 1;
    private const int CellCorridor = 2;

    public void GenerateCorridors(List<MSTEdge> mstEdges, Dictionary<Vector2Int, Room> roomByCenter, 
        Dictionary<Vector2Int, int> roomIndexByCenter, int[,] occupancyGrid, int[,] roomIndexGrid, 
        int dungeonWidth, int dungeonHeight, float roomCrossPenalty, float turnPenalty, 
        float existingCorridorCost, out List<List<Vector2Int>> corridorPaths, out int carvedTiles)
    {
        corridorPaths = new List<List<Vector2Int>>();
        carvedTiles = 0;

        PathfindingAStar pathfinder = new PathfindingAStar();

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

            List<Vector2Int> corridorPath = pathfinder.FindPath(startDoor, endDoor, startRoomIndex, endRoomIndex, 
                occupancyGrid, roomIndexGrid, dungeonWidth, dungeonHeight, roomCrossPenalty, turnPenalty, existingCorridorCost, false);
            
            if (corridorPath == null)
            {
                corridorPath = pathfinder.FindPath(startDoor, endDoor, startRoomIndex, endRoomIndex, 
                    occupancyGrid, roomIndexGrid, dungeonWidth, dungeonHeight, roomCrossPenalty, turnPenalty, existingCorridorCost, true);
            }

            if (corridorPath == null || corridorPath.Count == 0)
            {
                continue;
            }

            carvedTiles += CarveCorridor(corridorPath, startRoomIndex, endRoomIndex, occupancyGrid, roomIndexGrid);
            corridorPaths.Add(new List<Vector2Int>(corridorPath));
        }
    }

    private Vector2Int GetDoorCell(Room room, Vector2Int target)
    {
        int minX = room.bounds.x;
        int maxX = room.bounds.xMax - 1;
        int minY = room.bounds.y;
        int maxY = room.bounds.yMax - 1;

        int dx = target.x - room.center.x;
        int dy = target.y - room.center.y;

        if (Mathf.Abs(dx) >= Mathf.Abs(dy))
        {
            int x = dx >= 0 ? maxX : minX;
            int y = (minY + maxY) / 2;
            return new Vector2Int(x, y);
        }
        else
        {
            int x = (minX + maxX) / 2;
            int y = dy >= 0 ? maxY : minY;
            return new Vector2Int(x, y);
        }
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
