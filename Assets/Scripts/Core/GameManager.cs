using System;
using UnityEngine;

public enum GameState
{
    Playing,
    LevelUp
}

public class GameManager : MonoBehaviour
{
    public static GameManager instance { get; private set; }

    public GameState state = GameState.Playing;
    
    private Room currentRoom;

    public event Action<Room> CurrentRoomChanged;
    public event Action<Room> WaveEnemiesCleared;

    public bool IsPlaying() => state == GameState.Playing;
    
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
    public bool IsInBossRoom()
    {
        return currentRoom != null && currentRoom.type == roomTypes.Boss;
    }

    public void NotifyWaveEnemiesCleared(Room room)
    {
        WaveEnemiesCleared?.Invoke(room);
    }

    public void SetLevelUpState()
    {
        state = GameState.LevelUp;
        Time.timeScale = 0f; // congela el juego
    }

    public void ResumeGame()
    {
        state = GameState.Playing;
        Time.timeScale = 1f;
    }
    
}
