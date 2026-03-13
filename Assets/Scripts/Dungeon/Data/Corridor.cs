using UnityEngine;

public struct Corridor
{
    public Vector2Int start;
    public Vector2Int end;
    public Vector2Int bend;
    public bool hasBend;

    public Corridor(Vector2Int start, Vector2Int end)
    {
        this.start = start;
        this.end = end;
        bend = default;
        hasBend = false;
    }

    public Corridor(Vector2Int start, Vector2Int end, Vector2Int bend)
    {
        this.start = start;
        this.end = end;
        this.bend = bend;
        hasBend = true;
    }
}
