using System.Collections.Generic;
using UnityEngine;

public class DungeonVisualization
{
    public void DrawNodeGizmos(BSPNode node)
    {
        Gizmos.color = Color.white;

        Vector3 center = new Vector3(node.Area.x + node.Area.width / 2f, 0f, node.Area.y + node.Area.height / 2f);
        Vector3 size = new Vector3(node.Area.width, 0f, node.Area.height);
        Gizmos.DrawWireCube(center, size);

        if (!node.IsLeaf)
        {
            DrawNodeGizmos(node.left);
            DrawNodeGizmos(node.right);
        }
    }

    public void DrawRoomsGizmos(List<Room> rooms)
    {
        if (rooms == null)
        {
            return;
        }

        Gizmos.color = Color.green;
        foreach (Room room in rooms)
        {
            Vector3 center = new Vector3(room.center.x, 0f, room.center.y);
            Vector3 size = new Vector3(room.bounds.width, 0.1f, room.bounds.height);
            Gizmos.DrawWireCube(center, size);
        }
    }

    public void DrawMSTGizmos(List<MSTEdge> mstEdges)
    {
        if (mstEdges == null || mstEdges.Count == 0)
        {
            return;
        }

        Gizmos.color = new Color(1f, 0f, 0f, 0.8f);
        foreach (MSTEdge edge in mstEdges)
        {
            Vector3 p1 = new Vector3(edge.p1.x, 0.03f, edge.p1.y);
            Vector3 p2 = new Vector3(edge.p2.x, 0.03f, edge.p2.y);
            Gizmos.DrawLine(p1, p2);
        }
    }

    public void DrawDelaunayGizmos(List<DelaunayTriangle> delaunayTriangles)
    {
        if (delaunayTriangles == null || delaunayTriangles.Count == 0)
        {
            return;
        }

        Gizmos.color = new Color(0.1f, 0.25f, 0.85f, 0.8f);
        foreach (DelaunayTriangle triangle in delaunayTriangles)
        {
            Vector3 p1 = new Vector3(triangle.p1.x, 0.02f, triangle.p1.y);
            Vector3 p2 = new Vector3(triangle.p2.x, 0.02f, triangle.p2.y);
            Vector3 p3 = new Vector3(triangle.p3.x, 0.02f, triangle.p3.y);

            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawLine(p2, p3);
            Gizmos.DrawLine(p3, p1);
        }
    }

    public void DrawCorridorGizmos(List<List<Vector2Int>> corridorPaths, bool showCorridorCells)
    {
        if (corridorPaths == null || corridorPaths.Count == 0)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        foreach (List<Vector2Int> path in corridorPaths)
        {
            if (showCorridorCells)
            {
                foreach (Vector2Int cell in path)
                {
                    Gizmos.DrawCube(new Vector3(cell.x, 0.05f, cell.y), new Vector3(0.7f, 0.1f, 0.7f));
                }
            }

            CorridorGenerator corridorGen = new CorridorGenerator();
            List<Vector2Int> previewPath = corridorGen.SimplifyPath(path);
            for (int i = 0; i < previewPath.Count - 1; i++)
            {
                Vector3 from = new Vector3(previewPath[i].x, 0.06f, previewPath[i].y);
                Vector3 to = new Vector3(previewPath[i + 1].x, 0.06f, previewPath[i + 1].y);
                Gizmos.DrawLine(from, to);
            }
        }
    }

    public void DrawDensityMapGizmos(float[,] densityMap, int dungeonWidth, int dungeonHeight, float densityThreshold)
    {
        if (densityMap == null)
        {
            return;
        }

        for (int x = 0; x < dungeonWidth; x++)
        {
            for (int y = 0; y < dungeonHeight; y++)
            {
                float density = densityMap[x, y];
                float alpha = density >= densityThreshold ? 0.3f : 0.1f;
                Color densityColor = Color.Lerp(new Color(1f, 0f, 0f, alpha), new Color(0f, 1f, 0f, alpha), density);
                Gizmos.color = densityColor;

                Vector3 pos = new Vector3(x, 0.01f, y);
                Gizmos.DrawCube(pos, Vector3.one * 0.8f);
            }
        }
    }
}
