using UnityEngine;

public class Treasure : MonoBehaviour
{
    [HideInInspector] public bool chestOpened = false;

    public void OpenChest(PlayerStats ps)
    {
        ps.LevelUp();
        chestOpened = true;
    }
}