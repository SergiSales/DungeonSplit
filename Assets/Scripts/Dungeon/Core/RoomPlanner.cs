using System.Collections.Generic;
using UnityEngine;

public sealed class RoomPlanner : MonoBehaviour
{
    public List<Room> rooms;
    public AssetsSpawner assetsSpawner;
    public GameObject playerPrefab;
    public float playerSpawnHeight;
    public float cellSize;
    public float roomSpacingMultiplier;

    private bool bossRoomAssigned;
    private bool playerSpawned;

    public void AssignRoomTypes()
    {
        bossRoomAssigned = false;
        playerSpawned = false;

        if (rooms == null || rooms.Count == 0)
        {
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

        ThirdPersonController player = GameObject.FindAnyObjectByType<ThirdPersonController>();
        if (player == null)
        {
            if (playerPrefab == null)
            {
                return;
            }

            GameObject playerInstance = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
            player = playerInstance.GetComponent<ThirdPersonController>();
        }

        if (player == null)
        {
            return;
        }

        player.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);

        Rigidbody playerRigidbody = player.GetComponent<Rigidbody>();
        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        CameraBehaviour cameraFollow =
        Camera.main.GetComponent<CameraBehaviour>();

        if (cameraFollow != null)
        {
            cameraFollow.target = player.transform;
        }

    }
}
