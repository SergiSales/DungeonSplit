using UnityEngine;

public struct DelaunayTriangle
{
    public Vector2Int p1, p2, p3;
    public DelaunayTriangle(Vector2Int a, Vector2Int b, Vector2Int c)
    {
        p1 = a;
        p2 = b;
        p3 = c;
    }
}
