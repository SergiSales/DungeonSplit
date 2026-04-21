using System.Collections.Generic;
using UnityEngine;

public class DelaunayGenerator
{
    public List<DelaunayTriangle> GenerateTriangulation(List<Vector2Int> points)
    {
        List<DelaunayTriangle> triangles = new List<DelaunayTriangle>();

        if (points.Count < 3)
        {
            return triangles;
        }

        Vector2Int minPoint = new Vector2Int(int.MaxValue, int.MaxValue);
        Vector2Int maxPoint = new Vector2Int(int.MinValue, int.MinValue);

        foreach (Vector2Int point in points)
        {
            minPoint = new Vector2Int(Mathf.Min(minPoint.x, point.x), Mathf.Min(minPoint.y, point.y));
            maxPoint = new Vector2Int(Mathf.Max(maxPoint.x, point.x), Mathf.Max(maxPoint.y, point.y));
        }

        int deltaMax = Mathf.Max(maxPoint.x - minPoint.x, maxPoint.y - minPoint.y);
        Vector2Int mid = new Vector2Int((minPoint.x + maxPoint.x) / 2, (minPoint.y + maxPoint.y) / 2);

        Vector2Int p1 = new Vector2Int(mid.x, mid.y + 2 * deltaMax);
        Vector2Int p2 = new Vector2Int(mid.x - 2 * deltaMax, mid.y - deltaMax);
        Vector2Int p3 = new Vector2Int(mid.x + 2 * deltaMax, mid.y - deltaMax);

        triangles.Add(new DelaunayTriangle(p1, p2, p3));

        foreach (Vector2Int point in points)
        {
            List<DelaunayTriangle> badTriangles = new List<DelaunayTriangle>();

            foreach (DelaunayTriangle triangle in triangles)
            {
                if (IsPointInCircumcircle(point, triangle))
                {
                    badTriangles.Add(triangle);
                }
            }

            HashSet<(Vector2Int, Vector2Int)> polygon = new HashSet<(Vector2Int, Vector2Int)>();
            foreach (DelaunayTriangle triangle in badTriangles)
            {
                AddEdgeToPolygon(polygon, triangle.p1, triangle.p2);
                AddEdgeToPolygon(polygon, triangle.p2, triangle.p3);
                AddEdgeToPolygon(polygon, triangle.p3, triangle.p1);
            }

            foreach (DelaunayTriangle triangle in badTriangles)
            {
                triangles.Remove(triangle);
            }

            foreach ((Vector2Int, Vector2Int) edge in polygon)
            {
                triangles.Add(new DelaunayTriangle(edge.Item1, edge.Item2, point));
            }
        }

        triangles.RemoveAll(triangle =>
            triangle.p1 == p1 || triangle.p1 == p2 || triangle.p1 == p3 ||
            triangle.p2 == p1 || triangle.p2 == p2 || triangle.p2 == p3 ||
            triangle.p3 == p1 || triangle.p3 == p2 || triangle.p3 == p3);

        return triangles;
    }

    public List<MSTEdge> ExtractEdgesFromTriangles(List<DelaunayTriangle> triangles)
    {
        List<MSTEdge> edges = new List<MSTEdge>();
        HashSet<(Vector2Int, Vector2Int)> uniqueEdges = new HashSet<(Vector2Int, Vector2Int)>();

        foreach (DelaunayTriangle triangle in triangles)
        {
            AddEdgeNormalized(uniqueEdges, triangle.p1, triangle.p2);
            AddEdgeNormalized(uniqueEdges, triangle.p2, triangle.p3);
            AddEdgeNormalized(uniqueEdges, triangle.p3, triangle.p1);
        }

        foreach ((Vector2Int, Vector2Int) edge in uniqueEdges)
        {
            edges.Add(new MSTEdge(edge.Item1, edge.Item2));
        }

        return edges;
    }

    private void AddEdgeNormalized(HashSet<(Vector2Int, Vector2Int)> edges, Vector2Int p1, Vector2Int p2)
    {
        if (p1.x > p2.x || (p1.x == p2.x && p1.y > p2.y))
        {
            Vector2Int temp = p1;
            p1 = p2;
            p2 = temp;
        }

        edges.Add((p1, p2));
    }

    private void AddEdgeToPolygon(HashSet<(Vector2Int, Vector2Int)> polygon, Vector2Int p1, Vector2Int p2)
    {
        (Vector2Int, Vector2Int) edge = (p1, p2);
        (Vector2Int, Vector2Int) reverseEdge = (p2, p1);

        if (polygon.Contains(reverseEdge))
        {
            polygon.Remove(reverseEdge);
        }
        else
        {
            polygon.Add(edge);
        }
    }

    private bool IsPointInCircumcircle(Vector2Int point, DelaunayTriangle triangle)
    {
        float ax = triangle.p1.x;
        float ay = triangle.p1.y;
        float bx = triangle.p2.x;
        float by = triangle.p2.y;
        float cx = triangle.p3.x;
        float cy = triangle.p3.y;
        float px = point.x;
        float py = point.y;

        float determinant = 2f * (ax * (by - cy) + bx * (cy - ay) + cx * (ay - by));
        if (Mathf.Abs(determinant) < 0.0001f)
        {
            return false;
        }

        float ux = ((ax * ax + ay * ay) * (by - cy) + (bx * bx + by * by) * (cy - ay) + (cx * cx + cy * cy) * (ay - by)) / determinant;
        float uy = ((ax * ax + ay * ay) * (cx - bx) + (bx * bx + by * by) * (ax - cx) + (cx * cx + cy * cy) * (bx - ax)) / determinant;

        float radiusSq = (ax - ux) * (ax - ux) + (ay - uy) * (ay - uy);
        float distSq = (px - ux) * (px - ux) + (py - uy) * (py - uy);
        return distSq <= radiusSq + 0.01f;
    }
}
