using UnityEngine;

public class Room
{
    // Cada room es una sala en la mazmorra
    
    public IntRect bounds; // Dimensiones de la sala
    public Vector2Int center; // Centro de la sala

    public Room(IntRect bounds)
    {
        this.bounds = bounds;
        center = new Vector2Int(bounds.x + bounds.width / 2, bounds.y + bounds.height / 2); // Calcular centro de la sala, se usará en Delaunay

    }
}
