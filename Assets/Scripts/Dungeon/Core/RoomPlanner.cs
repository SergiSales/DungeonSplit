using System.Collections.Generic;
using UnityEngine;

public sealed class RoomPlanner
{
    private readonly List<Room> rooms;
    private readonly AssetsSpawner assetsSpawner;
    private readonly GameObject playerPrefab;
    private readonly float playerSpawnHeight;
    private readonly float cellSize;
    private readonly float roomSpacingMultiplier;
    private bool bossRoomAssigned;
    private bool playerSpawned;

    public RoomPlanner(
        List<Room> rooms,
        AssetsSpawner assetsSpawner,
        GameObject playerPrefab,
        float playerSpawnHeight,
        float cellSize,
        float roomSpacingMultiplier)
    {
        this.rooms = rooms;
        this.assetsSpawner = assetsSpawner;
        this.playerPrefab = playerPrefab;
        this.playerSpawnHeight = playerSpawnHeight;
        this.cellSize = cellSize;
        this.roomSpacingMultiplier = roomSpacingMultiplier;
    }

    public void AssignRoomTypes()
    {
        if (rooms == null || rooms.Count == 0)
        {
            Debug.LogWarning("[Test12] Cannot assign room types because no rooms were generated.");
            return;
        }

        for (int i = 0; i < rooms.Count; i++)
        {
            Room room = rooms[i];
            if (!bossRoomAssigned && Random.value < 0.1f)
            {
                room.type = roomTypes.Boss;
                bossRoomAssigned = true;
            }
            else if (!playerSpawned && Random.value < 0.2f)
            {
                room.type = roomTypes.Player;
                SpawnPlayer(i);
                playerSpawned = true;
            }
            else if (Random.value < 0.8f)
            {
                room.type = roomTypes.Wave;
            }
            else
            {
                room.type = roomTypes.Treasure;
            }
        }

        EnsureBossRoomAssigned();
        EnsurePlayerRoomAssigned();
    }

    private void EnsureBossRoomAssigned()
    {
        if (bossRoomAssigned || rooms.Count == 0)
        {
            return;
        }

        int bossRoomIndex = Random.Range(0, rooms.Count);
        rooms[bossRoomIndex].type = roomTypes.Boss;
        bossRoomAssigned = true;
    }

    private void EnsurePlayerRoomAssigned()
    {
        if (playerSpawned || rooms.Count == 0)
        {
            return;
        }

        int playerRoomIndex = Random.Range(0, rooms.Count);
        while (rooms[playerRoomIndex].type == roomTypes.Boss)
        {
            playerRoomIndex = (playerRoomIndex + 1) % rooms.Count;
        }

        rooms[playerRoomIndex].type = roomTypes.Player;
        playerSpawned = true;
        SpawnPlayer(playerRoomIndex);
    }

    private void SpawnPlayer(int roomIndex)
    {
        Room spawnRoom = rooms[roomIndex];
        Vector3 spawnPosition = assetsSpawner.GridToWorld(spawnRoom.center, cellSize, roomSpacingMultiplier);
        spawnPosition.y = playerSpawnHeight;
        spawnRoom.visited = true;

        ThirdPersonController player = Object.FindAnyObjectByType<ThirdPersonController>();
        if (player == null)
        {
            if (playerPrefab == null)
            {
                Debug.LogWarning("[Test12] No player found in scene and no player prefab assigned.");
                return;
            }

            GameObject playerInstance = Object.Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
            player = playerInstance.GetComponent<ThirdPersonController>();
        }

        if (player == null)
        {
            Debug.LogWarning("[Test12] The spawned player prefab does not have a ThirdPersonController.");
            return;
        }

        player.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);

        Rigidbody playerRigidbody = player.GetComponent<Rigidbody>();
        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }
    }
}
