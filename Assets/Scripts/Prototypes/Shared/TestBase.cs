using System.Collections.Generic;
using UnityEngine;

public class TestBase : MonoBehaviour
{
    [HideInInspector] public bool enableTeleport = true;

    private bool teleportOnCooldown;
    private bool teleportLockedByRoom;

    protected void InitializeTeleportState()
    {
        teleportOnCooldown = false;
        teleportLockedByRoom = false;
        RefreshTeleportState();
    }

    public void SetTeleportCooldown(bool isActive)
    {
        teleportOnCooldown = isActive;
        RefreshTeleportState();
    }

    protected void LockTeleportForWave()
    {
        teleportLockedByRoom = true;
        RefreshTeleportState();
    }

    protected void UnlockTeleportAfterWave()
    {
        teleportLockedByRoom = false;
        RefreshTeleportState();
    }

    protected bool AreAllWaveEnemiesDefeated(List<GameObject> spawnedEnemies)
    {
        if (spawnedEnemies == null || spawnedEnemies.Count == 0)
        {
            return true;
        }

        for (int i = 0; i < spawnedEnemies.Count; i++)
        {
            GameObject enemyObject = spawnedEnemies[i];
            if (enemyObject == null)
            {
                continue;
            }

            EnemyBase enemy = enemyObject.GetComponent<EnemyBase>();
            if (enemy == null)
            {
                return false;
            }
        }

        return true;
    }

    private void RefreshTeleportState()
    {
        enableTeleport = !teleportOnCooldown && !teleportLockedByRoom;
    }

    public virtual void HandlePlayerTeleported(Transform playerTransform, Room room)
    {
        return;
    }
}
