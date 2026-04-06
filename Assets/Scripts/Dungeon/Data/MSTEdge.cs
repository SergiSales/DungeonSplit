using UnityEngine;

public struct MSTEdge
{
    public Vector2Int p1, p2;
        public float distance;
        public MSTEdge(Vector2Int a, Vector2Int b)
        {
            p1 = a;
            p2 = b;
            distance = Vector2Int.Distance(a, b);
        }
}