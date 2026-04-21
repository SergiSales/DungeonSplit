using System.Collections.Generic;
using UnityEngine;

public class GridUtilities
{
    private const int CellEmpty = 0;
    private const int CellRoom = 1;

    public void BuildRoomLookups(List<Room> rooms, out Dictionary<Vector2Int, Room> roomByCenter, 
        out Dictionary<Vector2Int, int> roomIndexByCenter)
    {
        roomByCenter = new Dictionary<Vector2Int, Room>();
        roomIndexByCenter = new Dictionary<Vector2Int, int>();

        for (int i = 0; i < rooms.Count; i++)
        {
            roomByCenter[rooms[i].center] = rooms[i];
            roomIndexByCenter[rooms[i].center] = i;
        }
    }

    public void BuildNavigationGrid(List<Room> rooms, int dungeonWidth, int dungeonHeight, 
        out int[,] occupancyGrid, out int[,] roomIndexGrid)
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
                    if (!IsInsideGrid(cell, dungeonWidth, dungeonHeight))
                    {
                        continue;
                    }

                    occupancyGrid[x, y] = CellRoom;
                    roomIndexGrid[x, y] = i;
                }
            }
        }
    }

    public bool IsInsideGrid(Vector2Int cell, int dungeonWidth, int dungeonHeight)
    {
        return cell.x >= 0 && cell.x < dungeonWidth && cell.y >= 0 && cell.y < dungeonHeight;
    }
}
