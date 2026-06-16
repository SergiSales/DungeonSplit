using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance { get; private set; }
    
    private Room currentRoom;

    public event Action<Room> CurrentRoomChanged;
    public event Action<Room> WaveEnemiesCleared;
    
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    public void SetCurrentRoom(Room room)
    {
        if (currentRoom == room)
        {
            return;
        }

        currentRoom = room;
        CurrentRoomChanged?.Invoke(currentRoom);
    }
    
    public Room GetCurrentRoom()
    {
        return currentRoom;
    }
    
    public bool IsInWaveRoom()
    {
        return currentRoom != null && currentRoom.type == roomTypes.Wave;
    }

    public void NotifyWaveEnemiesCleared(Room room)
    {
        WaveEnemiesCleared?.Invoke(room);
    }
}
