using UnityEngine;

public class Room
{
    // Cada room es una sala en la mazmorra
    
    public IntRect bounds; // Dimensiones de la sala
    public Vector2Int center; // Centro de la sala

    public Vector2Int izquierda;
    public Vector2Int derecha;
    public Vector2Int arriba;
    public Vector2Int abajo;

    public Room(IntRect bounds)
    {
        this.bounds = bounds;
        center = new Vector2Int(bounds.x + bounds.width / 2, bounds.y + bounds.height / 2); // Calcular centro de la sala, se usará en Delaunay
        izquierda = new Vector2Int(bounds.x, center.y);
        derecha = new Vector2Int(bounds.xMax - 1, center.y);
        arriba = new Vector2Int(center.x, bounds.y);
        abajo = new Vector2Int(center.x, bounds.yMax - 1);
    }
}
