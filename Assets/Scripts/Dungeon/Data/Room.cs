using UnityEngine;

public enum roomTypes { Wave, Treasure, Boss, Player}; // Tipo de sala

public class Room
{
    // Cada room es una sala en la mazmorra

    public IntRect bounds; // Dimensiones de la sala
    public Vector2Int center; // Centro de la sala
    public roomTypes type; 
    public Vector2Int topLeft, topRight, bottomLeft, bottomRight; // Puntos de conexión para pasillos
    public Room(IntRect bounds)
    {
        this.bounds = bounds;
        center = new Vector2Int(bounds.x + bounds.width / 2, bounds.y + bounds.height / 2);
        topLeft = new Vector2Int(bounds.x, bounds.y + bounds.height);
        topRight = new Vector2Int(bounds.x + bounds.width, bounds.y + bounds.height);
        bottomLeft = new Vector2Int(bounds.x, bounds.y);
        bottomRight = new Vector2Int(bounds.x + bounds.width, bounds.y);
    }
}
