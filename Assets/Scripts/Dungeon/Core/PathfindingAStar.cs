using System.Collections.Generic;
using UnityEngine;

public class PathfindingAStar
{
    public struct SearchBounds
    {
        public int MinX;
        public int MinY;
        public int MaxX;
        public int MaxY;

        public SearchBounds(int minX, int minY, int maxX, int maxY)
        {
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
        }

        public bool Contains(Vector2Int cell)
        {
            return cell.x >= MinX && cell.x <= MaxX && cell.y >= MinY && cell.y <= MaxY;
        }
    }

    private static readonly Vector2Int[] CardinalDirections =
    {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left
    };

    private const int CellEmpty = 0;
    private const int CellRoom = 1;
    private const int CellCorridor = 2;

    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal, int startRoomIndex, int endRoomIndex,
        Vector2Int startRoomAccessCell, Vector2Int endRoomAccessCell,
        int[,] occupancyGrid, int[,] roomIndexGrid, int dungeonWidth, int dungeonHeight, 
        float roomCrossPenalty, float turnPenalty, float existingCorridorCost, bool allowRoomPenalty,
        float[,] traversalNoise = null, SearchBounds? searchBounds = null)
    {
        if (!IsInsideGrid(start, dungeonWidth, dungeonHeight) || !IsInsideGrid(goal, dungeonWidth, dungeonHeight))
        {
            return null;
        }

        if (searchBounds.HasValue &&
            (!searchBounds.Value.Contains(start) || !searchBounds.Value.Contains(goal)))
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

                if (closedSet.Contains(neighbor) ||
                    !IsInsideGrid(neighbor, dungeonWidth, dungeonHeight) ||
                    (searchBounds.HasValue && !searchBounds.Value.Contains(neighbor)))
                {
                    continue;
                }

                if (!TryGetTraversalCost(neighbor, startRoomIndex, endRoomIndex, startRoomAccessCell, endRoomAccessCell, allowRoomPenalty,
                    occupancyGrid, roomIndexGrid, roomCrossPenalty, existingCorridorCost, out float traversalCost))
                {
                    continue;
                }
                
                float noise = traversalNoise != null
                    ? traversalNoise[neighbor.x, neighbor.y]
                    : Mathf.PerlinNoise(neighbor.x * 0.08f, neighbor.y * 0.08f);

                float tentativeScore =
                    gScore[current]
                    + traversalCost
                    + GetTurnPenalty(cameFrom, current, neighbor, turnPenalty)
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

    private bool TryGetTraversalCost(Vector2Int cell, int startRoomIndex, int endRoomIndex, Vector2Int startRoomAccessCell,
        Vector2Int endRoomAccessCell, bool allowRoomPenalty,
        int[,] occupancyGrid, int[,] roomIndexGrid, float roomCrossPenalty, float existingCorridorCost, out float cost)
    {
        cost = 1f;

        int roomIndex = roomIndexGrid[cell.x, cell.y];
        if (roomIndex >= 0)
        {
            if (roomIndex == startRoomIndex)
            {
                if (cell != startRoomAccessCell)
                {
                    return false;
                }

                cost = 1f;
                return true;
            }

            if (roomIndex == endRoomIndex)
            {
                if (cell != endRoomAccessCell)
                {
                    return false;
                }

                cost = 1f;
                return true;
            }

            if (!allowRoomPenalty)
            {
                return false;
            }

            cost = roomCrossPenalty;
            return true;
        }

        if (occupancyGrid[cell.x, cell.y] == CellCorridor)
        {
            cost = existingCorridorCost;
        }

        return true;
    }

    private float GetTurnPenalty(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current, Vector2Int next, float turnPenalty)
    {
        if (!cameFrom.TryGetValue(current, out Vector2Int previous))
        {
            return 0f;
        }

        Vector2Int previousDirection = current - previous;
        Vector2Int newDirection = next - current;
        return previousDirection == newDirection ? 0f : turnPenalty;
    }

    private Vector2Int GetLowestFScore(List<Vector2Int> openSet, Dictionary<Vector2Int, float> fScore)
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

    private float Heuristic(Vector2Int current, Vector2Int goal)
    {
        return Mathf.Abs(current.x - goal.x) + Mathf.Abs(current.y - goal.y);
    }

    private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
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

    private bool IsInsideGrid(Vector2Int cell, int width, int height)
    {
        return cell.x >= 0 && cell.x < width && cell.y >= 0 && cell.y < height;
    }
}
