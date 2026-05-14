using UnityEngine;

public enum roomTypes { Wave, Treasure, Boss, Player}; // Tipo de sala

public class Room
{
    // Cada room es una sala en la mazmorra
    private static int index = 0;
    public int id = 0;

    public bool visited = false; // Para el minimapa

    public IntRect bounds; // Dimensiones de la sala
    public Vector2Int center; // Centro de la sala
    public roomTypes type;
    public bool cleared = false; // Para saber si se ha hecho el evento de la sala (derrotar enemigos, recoger tesoro, etc)
    public bool waveStarted = false; // Evita arrancar varias veces la misma oleada
    
    public Room(IntRect bounds)
    {
        this.bounds = bounds;
        center = new Vector2Int(bounds.x + bounds.width / 2, bounds.y + bounds.height / 2);
        id = index;
        index++;
    }
}
