using System;
using UnityEngine;
using System.Collections;

public enum GameState
{
    Playing,
    LevelUp,
    GameEnd
}

public class GameManager : MonoBehaviour
{
    public static GameManager instance { get; private set; }

    public GameState state = GameState.Playing;
    
    private Room currentRoom;

    public event Action<Room> CurrentRoomChanged;
    public event Action<Room> WaveEnemiesCleared;

    public bool IsPlaying() => state == GameState.Playing;

    public GameObject WIN;
    public GameObject LOSE;

    public Transform cameraTransform;

    
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

    private void Start()
    {
        WIN.SetActive(false);
        LOSE.SetActive(false);
        
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

    public IEnumerator SetGameOver(bool c)
    {

        yield return new WaitForSeconds(2.5f);
        if (c)
        {
            WIN.SetActive(true);
            
        }
        else
        {
            LOSE.SetActive(true);
        }
        
    }
    
}
