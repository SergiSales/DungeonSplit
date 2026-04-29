using System.Collections.Generic;
using UnityEngine;

public class AssetsSpawner : MonoBehaviour
{
    Transform roomsParent;
    Transform p;
    int wallHeight = 10;

    public void SpawnRooms(GameObject floorPrefab, List<Room> rooms, GameObject wallPrefab, 
                            float cellSize, float wallThickness, float roomSpacingMultiplier, 
                            GameObject portalPrefab, List<MSTEdge> mstEdges, UIMinimap uiMinimap)
    {
        if (floorPrefab == null) return;
        
        if(roomsParent == null) roomsParent = new GameObject("Rooms").transform;

        Dictionary<Vector2Int, List<Vector2Int>> roomConnections = new Dictionary<Vector2Int, List<Vector2Int>>();
        
        foreach (MSTEdge edge in mstEdges)
        {
            if (!roomConnections.ContainsKey(edge.p1)) roomConnections[edge.p1] = new List<Vector2Int>();
            if (!roomConnections.ContainsKey(edge.p2)) roomConnections[edge.p2] = new List<Vector2Int>();

            // Añadimos la conexión en ambas direcciones
            roomConnections[edge.p1].Add(edge.p2);
            roomConnections[edge.p2].Add(edge.p1);
        }


        foreach (Room room in rooms)
        {
            p = new GameObject("Room_" + room.id).transform;
            p.SetParent(roomsParent);
            
            SpawnFloor(room, p, floorPrefab, cellSize, roomSpacingMultiplier);
            SpawnWalls(room, p, wallPrefab, cellSize, wallThickness, roomSpacingMultiplier);
            

            if (roomConnections.ContainsKey(room.center))
            {
                // Le pasamos SOLO los destinos que le corresponden a ESTA habitación
                SpawnPortals(portalPrefab, room, roomConnections[room.center], rooms, cellSize, roomSpacingMultiplier, uiMinimap, p);
            }
        }
    }

    public Vector3 GridToWorld(Vector2 gridPos, float cellSize, float roomSpacingMultiplier) 
    {
        Vector3 worldPos = new Vector3(
            gridPos.x * cellSize * roomSpacingMultiplier, 
            0f,
            gridPos.y * cellSize * roomSpacingMultiplier
        );
        return worldPos;
    }

    void SpawnFloor(Room room, Transform parent, GameObject floorPrefab, float cellSize, float roomSpacingMultiplier)
    {
        float exactCenterX = room.bounds.x + (room.bounds.width / 2f);
        float exactCenterY = room.bounds.y + (room.bounds.height / 2f);
        Vector2 exactCenter = new Vector2(exactCenterX, exactCenterY);

        Vector3 worldPos = GridToWorld(exactCenter, cellSize, roomSpacingMultiplier);
        
        GameObject instance = Instantiate(floorPrefab, worldPos, Quaternion.identity, parent);
        ScaleRoom(instance.transform, room, cellSize);
    }

    void ScaleRoom(Transform roomTransform, Room room, float cellSize)
    {
        Vector3 scale = new Vector3(room.bounds.width * cellSize, 1f, room.bounds.height * cellSize);
        roomTransform.localScale = scale;
    }

    void SpawnWalls(Room room, Transform parent, GameObject wallPrefab, float cellSize, float wallThickness, float roomSpacingMultiplier)
    {
        if (wallPrefab == null) return;

        float exactCenterX = room.bounds.x + (room.bounds.width / 2f);
        float exactCenterY = room.bounds.y + (room.bounds.height / 2f);
        Vector3 worldCenter = GridToWorld(new Vector2(exactCenterX, exactCenterY), cellSize, roomSpacingMultiplier);

        float sizeX = room.bounds.width * cellSize;
        float sizeZ = room.bounds.height * cellSize;
        float yPos = 5f;

        Vector3 topPos    = worldCenter + new Vector3(0, yPos, sizeZ / 2f);
        Vector3 bottomPos = worldCenter + new Vector3(0, yPos, -sizeZ / 2f);
        Vector3 leftPos   = worldCenter + new Vector3(-sizeX / 2f, yPos, 0);
        Vector3 rightPos  = worldCenter + new Vector3(sizeX / 2f, yPos, 0);

        Vector3 horizontalScale = new Vector3(sizeX + wallThickness, wallHeight, wallThickness);
        CreateWall(topPos, horizontalScale, parent, wallPrefab);
        CreateWall(bottomPos, horizontalScale, parent, wallPrefab);

        Vector3 verticalScale = new Vector3(wallThickness, wallHeight, sizeZ + wallThickness);
        CreateWall(leftPos, verticalScale, parent, wallPrefab);
        CreateWall(rightPos, verticalScale, parent, wallPrefab);
    }

    void CreateWall(Vector3 position, Vector3 scale, Transform parent, GameObject wallPrefab)
    {
        if (wallPrefab == null) return;
        GameObject wall = Instantiate(wallPrefab, position, Quaternion.identity, parent);
        wall.transform.localScale = scale;
    }

    void SpawnPortals(GameObject portalPrefab, Room currentRoom, List<Vector2Int> destinations, List<Room> rooms, float cellSize, float roomSpacingMultiplier, UIMinimap uiMinimap, Transform portalParent)
    {
        if (portalPrefab == null)
        {
            UnityEngine.Debug.LogWarning("[Test11] Portal prefab is missing!");
            return;
        }

        // Recorremos SOLO las conexiones reales de esta habitación
        foreach (Vector2Int destinationCenter in destinations)
        {
            Room destinationRoom = rooms.Find(r => r.center == destinationCenter);

            if (destinationRoom != null)
            {
                // Calculamos posiciones 3D
                Vector3 centerCurrent = GridToWorld(currentRoom.center, cellSize, roomSpacingMultiplier);
                Vector3 centerDestination = GridToWorld(destinationRoom.center, cellSize, roomSpacingMultiplier);

                // Dirección para que mire hacia la otra sala
                Vector3 dirToDestination = (centerDestination - centerCurrent).normalized;

                // Movemos el portal hacia el borde lógico desde el centro
                Vector3 portalPos = centerCurrent + (dirToDestination * cellSize * 0.5f);
                portalPos.y = 1f;

                // Instanciamos el portal como HIJO de la habitación actual
                GameObject portal = Instantiate(portalPrefab, portalPos, Quaternion.LookRotation(dirToDestination), portalParent);

                // Configuramos su script
                RoomPortal script = portal.AddComponent<RoomPortal>();
                script.destinationPosition = centerDestination;
                script.destinationRoom = destinationRoom;
                script.uiMinimap = uiMinimap;
            }
        }
    }
}