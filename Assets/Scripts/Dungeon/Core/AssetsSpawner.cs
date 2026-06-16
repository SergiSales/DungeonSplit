using System.Collections.Generic;
using UnityEngine;

public class AssetsSpawner : MonoBehaviour
{
    private const int WallHeight = 10;
    private const float TreasureHeight = 0.5f;
    private const float PortalHeight = 1f;
    private const float PortalInset = 0.5f;


    public void SpawnRooms(
        GameObject[] objectsPrefab,
        List<Room> rooms,
        float cellSize,
        float wallThickness,
        float roomSpacingMultiplier,
        List<MSTEdge> mstEdges,
        UIMinimap uiMinimap,
        Transform roomsParentOverride = null)
    {
        GameObject floorPrefab = GetPrefab(objectsPrefab, 0);
        if (floorPrefab == null || rooms == null)
        {
            return;
        }

        Transform roomsRoot = roomsParentOverride != null
            ? roomsParentOverride
            : new GameObject("Rooms").transform;

        Dictionary<Vector2Int, List<Vector2Int>> roomConnections = BuildRoomConnections(mstEdges);
        Dictionary<Vector2Int, Room> roomLookup = BuildRoomLookup(rooms);

        foreach (Room room in rooms)
        {
            SpawnRoom(
                room,
                roomsRoot,
                floorPrefab,
                GetPrefab(objectsPrefab, 1),
                GetPrefab(objectsPrefab, 2),
                GetPrefab(objectsPrefab, 3),
                cellSize,
                wallThickness,
                roomSpacingMultiplier,
                roomConnections,
                roomLookup,
                uiMinimap);
        }
    }

    public Vector3 GridToWorld(Vector2 gridPos, float cellSize, float roomSpacingMultiplier)
    {
        return new Vector3(
            gridPos.x * cellSize * roomSpacingMultiplier,
            0f,
            gridPos.y * cellSize * roomSpacingMultiplier);
    }

    private GameObject GetPrefab(GameObject[] prefabs, int index)
    {
        if (prefabs == null || index < 0 || index >= prefabs.Length)
        {
            return null;
        }

        return prefabs[index];
    }

    private Dictionary<Vector2Int, List<Vector2Int>> BuildRoomConnections(List<MSTEdge> mstEdges)
    {
        Dictionary<Vector2Int, List<Vector2Int>> roomConnections = new Dictionary<Vector2Int, List<Vector2Int>>();
        if (mstEdges == null)
        {
            return roomConnections;
        }

        foreach (MSTEdge edge in mstEdges)
        {
            AddConnection(roomConnections, edge.p1, edge.p2);
            AddConnection(roomConnections, edge.p2, edge.p1);
        }

        return roomConnections;
    }

    private Dictionary<Vector2Int, Room> BuildRoomLookup(List<Room> rooms)
    {
        Dictionary<Vector2Int, Room> roomLookup = new Dictionary<Vector2Int, Room>();
        foreach (Room room in rooms)
        {
            roomLookup[room.center] = room;
        }

        return roomLookup;
    }

    private void AddConnection(Dictionary<Vector2Int, List<Vector2Int>> roomConnections, Vector2Int source, Vector2Int destination)
    {
        if (!roomConnections.TryGetValue(source, out List<Vector2Int> destinations))
        {
            destinations = new List<Vector2Int>();
            roomConnections[source] = destinations;
        }

        destinations.Add(destination);
    }

    private void SpawnRoom(
        Room room,
        Transform roomsRoot,
        GameObject floorPrefab,
        GameObject wallPrefab,
        GameObject portalPrefab,
        GameObject treasurePrefab,
        float cellSize,
        float wallThickness,
        float roomSpacingMultiplier,
        Dictionary<Vector2Int, List<Vector2Int>> roomConnections,
        Dictionary<Vector2Int, Room> roomLookup,
        UIMinimap uiMinimap)
    {
        Transform roomParent = new GameObject("Room_" + room.id).transform;
        roomParent.SetParent(roomsRoot);

        SpawnFloor(room, roomParent, floorPrefab, cellSize, roomSpacingMultiplier);
        SpawnWalls(room, roomParent, wallPrefab, cellSize, wallThickness, roomSpacingMultiplier);
        SpawnTreasure(room, roomParent, treasurePrefab, cellSize, roomSpacingMultiplier);

        if (roomConnections.TryGetValue(room.center, out List<Vector2Int> destinations))
        {
            SpawnPortals(portalPrefab, room, destinations, roomLookup, cellSize, roomSpacingMultiplier, uiMinimap, roomParent);
        }
    }

    private void SpawnTreasure(Room room, Transform parent, GameObject treasurePrefab, float cellSize, float roomSpacingMultiplier)
    {
        if (room.type != roomTypes.Treasure || treasurePrefab == null)
        {
            return;
        }

        Vector3 treasurePosition = GridToWorld(room.center, cellSize, roomSpacingMultiplier);
        treasurePosition.y = TreasureHeight;
        Object.Instantiate(treasurePrefab, treasurePosition, Quaternion.identity, parent);
    }

    private void SpawnFloor(Room room, Transform parent, GameObject floorPrefab, float cellSize, float roomSpacingMultiplier)
    {
        Vector3 worldPos = GridToWorld(room.center, cellSize, roomSpacingMultiplier);
        GameObject instance = Object.Instantiate(floorPrefab, worldPos, Quaternion.identity, parent);
        ScaleRoom(instance.transform, room, cellSize);
    }

    private  void ScaleRoom(Transform roomTransform, Room room, float cellSize)
    {
        roomTransform.localScale = new Vector3(room.bounds.width * cellSize, 1f, room.bounds.height * cellSize);
    }

    private void SpawnWalls(Room room, Transform parent, GameObject wallPrefab, float cellSize, float wallThickness, float roomSpacingMultiplier)
    {
        if (wallPrefab == null)
        {
            return;
        }

        Vector3 worldCenter = GridToWorld(room.center, cellSize, roomSpacingMultiplier);

        float sizeX = room.bounds.width * cellSize;
        float sizeZ = room.bounds.height * cellSize;
        float yPos = WallHeight / 2f;

        Vector3 topPos = worldCenter + new Vector3(0f, yPos, sizeZ / 2f);
        Vector3 bottomPos = worldCenter + new Vector3(0f, yPos, -sizeZ / 2f);
        Vector3 leftPos = worldCenter + new Vector3(-sizeX / 2f, yPos, 0f);
        Vector3 rightPos = worldCenter + new Vector3(sizeX / 2f, yPos, 0f);

        Vector3 horizontalScale = new Vector3(sizeX + wallThickness, WallHeight, wallThickness);
        CreateWall(topPos, horizontalScale, parent, wallPrefab);
        CreateWall(bottomPos, horizontalScale, parent, wallPrefab);

        Vector3 verticalScale = new Vector3(wallThickness, WallHeight, sizeZ + wallThickness);
        CreateWall(leftPos, verticalScale, parent, wallPrefab);
        CreateWall(rightPos, verticalScale, parent, wallPrefab);
    }

    private  void CreateWall(Vector3 position, Vector3 scale, Transform parent, GameObject wallPrefab)
    {
        if (wallPrefab == null)
        {
            return;
        }

        GameObject wall = Object.Instantiate(wallPrefab, position, Quaternion.identity, parent);
        wall.transform.localScale = scale;
    }

    private void SpawnPortals(
        GameObject portalPrefab,
        Room currentRoom,
        List<Vector2Int> destinations,
        Dictionary<Vector2Int, Room> roomLookup,
        float cellSize,
        float roomSpacingMultiplier,
        UIMinimap uiMinimap,
        Transform portalParent)
    {
        if (portalPrefab == null)
        {
            Debug.LogWarning("[AssetsSpawner] Portal prefab is missing.");
            return;
        }

        foreach (Vector2Int destinationCenter in destinations)
        {
            if (!roomLookup.TryGetValue(destinationCenter, out Room destinationRoom))
            {
                continue;
            }

            Vector3 centerCurrent = GridToWorld(currentRoom.center, cellSize, roomSpacingMultiplier);
            Vector3 centerDestination = GridToWorld(destinationRoom.center, cellSize, roomSpacingMultiplier);
            Vector3 portalPosition = CalculatePortalPosition(currentRoom, centerCurrent, centerDestination, cellSize);
            Quaternion portalRotation = Quaternion.LookRotation(centerDestination - portalPosition);

            GameObject portal = Object.Instantiate(portalPrefab, portalPosition, portalRotation, portalParent);
            RoomPortal portalScript = portal.GetComponent<RoomPortal>() ?? portal.AddComponent<RoomPortal>();
            portalScript.destinationPosition = new Vector3(
                destinationRoom.center.x * cellSize * roomSpacingMultiplier,
                PortalHeight,
                destinationRoom.center.y * cellSize * roomSpacingMultiplier);
            portalScript.destinationRoom = destinationRoom;
            portalScript.uiMinimap = uiMinimap;
        }
    }

    private  Vector3 CalculatePortalPosition(Room currentRoom, Vector3 centerCurrent, Vector3 centerDestination, float cellSize)
    {
        Vector3 direction = (centerDestination - centerCurrent).normalized;
        float roomWidth = currentRoom.bounds.width * cellSize;
        float roomHeight = currentRoom.bounds.height * cellSize;
        Vector3 portalPosition = centerCurrent;

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.z))
        {
            float offsetX = (roomWidth / 2f) - PortalInset;
            portalPosition += direction.x > 0f
                ? new Vector3(offsetX, 0f, 0f)
                : new Vector3(-offsetX, 0f, 0f);
        }
        else
        {
            float offsetZ = (roomHeight / 2f) - PortalInset;
            portalPosition += direction.z > 0f
                ? new Vector3(0f, 0f, offsetZ)
                : new Vector3(0f, 0f, -offsetZ);
        }

        portalPosition.y = PortalHeight;
        return portalPosition;
    }
}
