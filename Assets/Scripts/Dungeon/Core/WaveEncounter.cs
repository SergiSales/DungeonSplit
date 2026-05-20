using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class WaveEncounter
{
    private const float MinimumRoomEdgePadding = 0.75f;
    private const float RoomEdgePaddingFactor = 0.35f;
    private const float MinimumSpawnableHalfExtent = 0.25f;

    private readonly AssetsSpawner assetsSpawner;
    private readonly GameObject[] waveEnemyPrefabs;
    private readonly int maxEnemiesPerWaveRoom;
    private readonly float waveStartDelay;
    private readonly float waveSpawnInterval;
    private readonly float minSpawnDistanceFromPlayer;
    private readonly float maxSpawnDistanceFromPlayer;
    private readonly float enemySpawnHeight;
    private readonly int spawnPositionAttempts;
    private readonly float cellSize;
    private readonly float roomSpacingMultiplier;

    public WaveEncounter(
        AssetsSpawner assetsSpawner,
        GameObject[] waveEnemyPrefabs,
        int maxEnemiesPerWaveRoom,
        float waveStartDelay,
        float waveSpawnInterval,
        float minSpawnDistanceFromPlayer,
        float maxSpawnDistanceFromPlayer,
        float enemySpawnHeight,
        int spawnPositionAttempts,
        float cellSize,
        float roomSpacingMultiplier)
    {
        this.assetsSpawner = assetsSpawner;
        this.waveEnemyPrefabs = waveEnemyPrefabs;
        this.maxEnemiesPerWaveRoom = maxEnemiesPerWaveRoom;
        this.waveStartDelay = waveStartDelay;
        this.waveSpawnInterval = waveSpawnInterval;
        this.minSpawnDistanceFromPlayer = minSpawnDistanceFromPlayer;
        this.maxSpawnDistanceFromPlayer = maxSpawnDistanceFromPlayer;
        this.enemySpawnHeight = enemySpawnHeight;
        this.spawnPositionAttempts = spawnPositionAttempts;
        this.cellSize = cellSize;
        this.roomSpacingMultiplier = roomSpacingMultiplier;
    }

    public GameObject EnsureEnemyParent(GameObject currentEnemyParent)
    {
        if (currentEnemyParent != null)
        {
            return currentEnemyParent;
        }

        GameObject createdEnemyParent = new GameObject("Enemy Father");
        Debug.Log("[Test12] enemyFather created");
        return createdEnemyParent;
    }

    public IEnumerator RunWave(
        Room room,
        Transform playerTransform,
        GameObject enemyParent,
        Func<List<GameObject>, bool> areAllWaveEnemiesDefeated,
        Action onWaveCompleted)
    {
        yield return new WaitForSeconds(waveStartDelay);

        GameObject enemyPrefab = GetWaveEnemyPrefab();
        if (enemyPrefab == null)
        {
            Debug.LogWarning("[Test12] No wave enemy prefab assigned. Wave room cannot spawn enemies.");
            onWaveCompleted?.Invoke();
            yield break;
        }

        List<GameObject> spawnedWaveEnemies = new List<GameObject>(maxEnemiesPerWaveRoom);
        for (int spawnedEnemies = 0; spawnedEnemies < maxEnemiesPerWaveRoom; )
        {
            if (TryGetSpawnPositionInRoom(room, playerTransform.position, out Vector3 spawnPosition))
            {
                GameObject enemy = UnityEngine.Object.Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
                if (enemyParent != null)
                {
                    enemy.transform.SetParent(enemyParent.transform);
                }

                spawnedWaveEnemies.Add(enemy);
                spawnedEnemies++;
            }

            yield return new WaitForSeconds(waveSpawnInterval);
        }

        yield return new WaitUntil(() => areAllWaveEnemiesDefeated == null || areAllWaveEnemiesDefeated(spawnedWaveEnemies));
        onWaveCompleted?.Invoke();
    }

    private GameObject GetWaveEnemyPrefab()
    {
        if (waveEnemyPrefabs == null || waveEnemyPrefabs.Length == 0)
        {
            return null;
        }

        int startIndex = UnityEngine.Random.Range(0, waveEnemyPrefabs.Length);
        for (int offset = 0; offset < waveEnemyPrefabs.Length; offset++)
        {
            GameObject prefab = waveEnemyPrefabs[(startIndex + offset) % waveEnemyPrefabs.Length];
            if (prefab != null)
            {
                return prefab;
            }
        }

        return null;
    }

    private bool TryGetSpawnPositionInRoom(Room room, Vector3 playerPosition, out Vector3 spawnPosition)
    {
        Vector3 roomCenter = assetsSpawner.GridToWorld(room.center, cellSize, roomSpacingMultiplier);
        float halfRoomWidth = room.bounds.width * cellSize / 2f;
        float halfRoomHeight = room.bounds.height * cellSize / 2f;
        float edgePadding = Mathf.Max(MinimumRoomEdgePadding, cellSize * RoomEdgePaddingFactor);
        float usableHalfWidth = Mathf.Max(MinimumSpawnableHalfExtent, halfRoomWidth - edgePadding);
        float usableHalfHeight = Mathf.Max(MinimumSpawnableHalfExtent, halfRoomHeight - edgePadding);
        float roomMinX = roomCenter.x - usableHalfWidth;
        float roomMaxX = roomCenter.x + usableHalfWidth;
        float roomMinZ = roomCenter.z - usableHalfHeight;
        float roomMaxZ = roomCenter.z + usableHalfHeight;
        float clampedMaxDistance = Mathf.Max(minSpawnDistanceFromPlayer, maxSpawnDistanceFromPlayer);

        for (int attempt = 0; attempt < spawnPositionAttempts; attempt++)
        {
            Vector2 randomDirection = UnityEngine.Random.insideUnitCircle.normalized;
            if (randomDirection == Vector2.zero)
            {
                randomDirection = Vector2.right;
            }

            float randomDistance = UnityEngine.Random.Range(minSpawnDistanceFromPlayer, clampedMaxDistance);
            Vector3 candidate = playerPosition + new Vector3(randomDirection.x, 0f, randomDirection.y) * randomDistance;
            candidate.x = Mathf.Clamp(candidate.x, roomMinX, roomMaxX);
            candidate.z = Mathf.Clamp(candidate.z, roomMinZ, roomMaxZ);
            candidate.y = enemySpawnHeight;

            float distanceToPlayer = Vector3.Distance(
                new Vector3(candidate.x, 0f, candidate.z),
                new Vector3(playerPosition.x, 0f, playerPosition.z));

            if (distanceToPlayer >= minSpawnDistanceFromPlayer)
            {
                spawnPosition = candidate;
                return true;
            }
        }

        spawnPosition = roomCenter;
        spawnPosition.y = enemySpawnHeight;
        return true;
    }
}
