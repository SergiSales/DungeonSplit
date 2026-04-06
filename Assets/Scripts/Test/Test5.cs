using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Test5 : MonoBehaviour
{
    [Header("Dungeon Settings")]
    public int dungeonWidth = 100;
    public int dungeonHeight = 100;
    public int minRoomSize = 10;
    public int seed;

    [Header("MST Settings")]
    public bool useMST = true;
    public bool showDelaunay = true;

    private BSPNode root;
    private List<Room> rooms;
    private List<MSTEdge> mstEdges;
    private List<MSTEdge> delaunayEdges;
    private List<DelaunayTriangle> delaunayTriangles;

    // Struct para aristas del MST
    private struct MSTEdge
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

    // Struct para triángulos Delaunay
    private struct DelaunayTriangle
    {
        public Vector2Int p1, p2, p3;
        public DelaunayTriangle(Vector2Int a, Vector2Int b, Vector2Int c)
        {
            p1 = a;
            p2 = b;
            p3 = c;
        }
    }

    void Start()
    {
        //Generar Mazmorra al iniciar escena
        seed = UnityEngine.Random.Range(0, 100000);
        Debug.Log("Starting generator");
        BSPGenerator generator = new BSPGenerator(minRoomSize, seed);
        Debug.Log("Generator Finished");
        Debug.Log("Generating root node");
        root = generator.Generate(new IntRect(0, 0, dungeonWidth, dungeonHeight));
        rooms = generator.CreateRooms(root);

        // Generar Delaunay + MST si está habilitado
        if (useMST && rooms.Count > 1)
        {
            delaunayTriangles = new List<DelaunayTriangle>();
            delaunayEdges = new List<MSTEdge>();
            mstEdges = new List<MSTEdge>();

            // Generar Delaunay Triangulation
            GenerateDelaunayTriangulation();
            Debug.Log("Delaunay Triangulation Generated. Triangles: " + delaunayTriangles.Count);

            // Aplicar Prim sobre las aristas de Delaunay
            GenerateMinimumSpanningTree();
            Debug.Log("MST Generated. Edges: " + mstEdges.Count);
        }
    }
    void OnDrawGizmos()
    {
        if (root == null)
        {
            return;
        }
        DrawNodeGizmos(root);
        DrawRoomsGizmos(root);

        if (useMST && delaunayTriangles != null && showDelaunay)
        {
            DrawDelaunayGizmos();
        }

        if (useMST && mstEdges != null)
        {
            DrawMSTGizmos();
        }
    }

    void DrawNodeGizmos(BSPNode node)
    {
        //Dibujar recursivamente los nodos del BSP
        Gizmos.color = Color.white;

        Vector3 center = new Vector3(node.Area.x + node.Area.width / 2f, 0, node.Area.y + node.Area.height / 2f);
        Vector3 size = new Vector3(node.Area.width, 0, node.Area.height);

        Gizmos.DrawWireCube(center, size);

        if (!node.IsLeaf)
        {
            DrawNodeGizmos(node.left);
            DrawNodeGizmos(node.right);
        }
    }

    void DrawRoomsGizmos(BSPNode node)
    {
        Gizmos.color = Color.green;
        foreach (var room in rooms)
        {
            Vector3 center = new Vector3(room.center.x, 0, room.center.y);
            Vector3 size = new Vector3(room.bounds.width, 0.1f, room.bounds.height);
            Gizmos.DrawWireCube(center, size);
        }
    }

    void GenerateMinimumSpanningTree()
    {
        if (rooms.Count < 2)
            return;

        // Aplicar Prim usando las aristas de Delaunay
        mstEdges = PrimAlgorithmFromDelaunay(delaunayEdges, rooms);
    }

    void GenerateDelaunayTriangulation()
    {
        // Obtener lista de centros de habitaciones
        List<Vector2Int> points = rooms.Select(r => r.center).ToList();

        // Generar triangulación Delaunay
        delaunayTriangles = BowyerWatsonTriangulation(points);

        // Extraer aristas únicas del Delaunay
        HashSet<(Vector2Int, Vector2Int)> uniqueEdges = new HashSet<(Vector2Int, Vector2Int)>();
        foreach (var triangle in delaunayTriangles)
        {
            AddEdgeNormalized(uniqueEdges, triangle.p1, triangle.p2);
            AddEdgeNormalized(uniqueEdges, triangle.p2, triangle.p3);
            AddEdgeNormalized(uniqueEdges, triangle.p3, triangle.p1);
        }

        // Convertir a lista de MSTEdge
        delaunayEdges = new List<MSTEdge>();
        foreach (var edge in uniqueEdges)
        {
            delaunayEdges.Add(new MSTEdge(edge.Item1, edge.Item2));
        }

        // Ordenar por distancia para optimizar
        delaunayEdges = delaunayEdges.OrderBy(e => e.distance).ToList();
    }

    void AddEdgeNormalized(HashSet<(Vector2Int, Vector2Int)> edges, Vector2Int p1, Vector2Int p2)
    {
        if (p1.x > p2.x || (p1.x == p2.x && p1.y > p2.y))
        {
            var temp = p1;
            p1 = p2;
            p2 = temp;
        }
        edges.Add((p1, p2));
    }

    List<MSTEdge> PrimAlgorithmFromDelaunay(List<MSTEdge> edges, List<Room> roomList)
    {
        List<MSTEdge> mst = new List<MSTEdge>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        List<Vector2Int> points = roomList.Select(r => r.center).ToList();

        if (points.Count == 0)
            return mst;

        // Comenzar con el primer punto
        visited.Add(points[0]);

        // Mientras no hayamos visitado todos los puntos
        while (visited.Count < points.Count)
        {
            float minDistance = float.MaxValue;
            MSTEdge bestEdge = new MSTEdge(Vector2Int.zero, Vector2Int.zero);
            bool foundEdge = false;

            // Buscar la arista más corta que conecte un nodo visitado con uno no visitado
            foreach (var edge in edges)
            {
                bool p1Visited = visited.Contains(edge.p1);
                bool p2Visited = visited.Contains(edge.p2);

                // Si conecta un visitado con uno no visitado
                if ((p1Visited && !p2Visited) || (!p1Visited && p2Visited))
                {
                    if (edge.distance < minDistance)
                    {
                        minDistance = edge.distance;
                        bestEdge = edge;
                        foundEdge = true;
                    }
                }
            }

            // Añadir la arista más corta al MST
            if (foundEdge)
            {
                mst.Add(bestEdge);
                visited.Add(visited.Contains(bestEdge.p1) ? bestEdge.p2 : bestEdge.p1);
            }
            else
            {
                break;
            }
        }

        return mst;
    }

    List<DelaunayTriangle> BowyerWatsonTriangulation(List<Vector2Int> points)
    {
        List<DelaunayTriangle> triangles = new List<DelaunayTriangle>();

        if (points.Count < 3)
            return triangles;

        // Crear supertriángulo que contiene todos los puntos
        Vector2Int minPoint = new Vector2Int(int.MaxValue, int.MaxValue);
        Vector2Int maxPoint = new Vector2Int(int.MinValue, int.MinValue);

        foreach (var p in points)
        {
            minPoint = new Vector2Int(Mathf.Min(minPoint.x, p.x), Mathf.Min(minPoint.y, p.y));
            maxPoint = new Vector2Int(Mathf.Max(maxPoint.x, p.x), Mathf.Max(maxPoint.y, p.y));
        }

        int deltaMax = Mathf.Max(maxPoint.x - minPoint.x, maxPoint.y - minPoint.y);
        Vector2Int mid = new Vector2Int((minPoint.x + maxPoint.x) / 2, (minPoint.y + maxPoint.y) / 2);

        Vector2Int p1 = new Vector2Int(mid.x, mid.y + 2 * deltaMax);
        Vector2Int p2 = new Vector2Int(mid.x - 2 * deltaMax, mid.y - deltaMax);
        Vector2Int p3 = new Vector2Int(mid.x + 2 * deltaMax, mid.y - deltaMax);

        triangles.Add(new DelaunayTriangle(p1, p2, p3));

        // Algoritmo Bowyer-Watson
        foreach (var point in points)
        {
            List<DelaunayTriangle> badTriangles = new List<DelaunayTriangle>();

            // Encontrar triángulos cuya circunferencia contiene el punto
            foreach (var triangle in triangles)
            {
                if (IsPointInCircumcircle(point, triangle))
                {
                    badTriangles.Add(triangle);
                }
            }

            // Polígono de la frontera
            HashSet<(Vector2Int, Vector2Int)> polygon = new HashSet<(Vector2Int, Vector2Int)>();

            foreach (var triangle in badTriangles)
            {
                AddEdgeToPolygon(polygon, triangle.p1, triangle.p2);
                AddEdgeToPolygon(polygon, triangle.p2, triangle.p3);
                AddEdgeToPolygon(polygon, triangle.p3, triangle.p1);
            }

            // Remover triángulos malos
            foreach (var triangle in badTriangles)
            {
                triangles.Remove(triangle);
            }

            // Crear nuevos triángulos
            foreach (var edge in polygon)
            {
                triangles.Add(new DelaunayTriangle(edge.Item1, edge.Item2, point));
            }
        }

        // Remover triángulos que comparten vértices con el supertriángulo
        triangles.RemoveAll(t =>
            (t.p1 == p1 || t.p1 == p2 || t.p1 == p3) ||
            (t.p2 == p1 || t.p2 == p2 || t.p2 == p3) ||
            (t.p3 == p1 || t.p3 == p2 || t.p3 == p3)
        );

        return triangles;
    }

    void AddEdgeToPolygon(HashSet<(Vector2Int, Vector2Int)> polygon, Vector2Int p1, Vector2Int p2)
    {
        var edge = (p1, p2);
        var reverseEdge = (p2, p1);

        if (polygon.Contains(reverseEdge))
        {
            polygon.Remove(reverseEdge);
        }
        else
        {
            polygon.Add(edge);
        }
    }

    bool IsPointInCircumcircle(Vector2Int point, DelaunayTriangle triangle)
    {
        float ax = triangle.p1.x, ay = triangle.p1.y;
        float bx = triangle.p2.x, by = triangle.p2.y;
        float cx = triangle.p3.x, cy = triangle.p3.y;
        float px = point.x, py = point.y;

        float D = 2 * (ax * (by - cy) + bx * (cy - ay) + cx * (ay - by));

        if (Mathf.Abs(D) < 0.0001f)
            return false;

        float ux = ((ax * ax + ay * ay) * (by - cy) + (bx * bx + by * by) * (cy - ay) + (cx * cx + cy * cy) * (ay - by)) / D;
        float uy = ((ax * ax + ay * ay) * (cx - bx) + (bx * bx + by * by) * (ax - cx) + (cx * cx + cy * cy) * (bx - ax)) / D;

        float radiusSq = (ax - ux) * (ax - ux) + (ay - uy) * (ay - uy);
        float distSq = (px - ux) * (px - ux) + (py - uy) * (py - uy);

        return distSq <= radiusSq + 0.01f;
    }

    void DrawMSTGizmos()
    {
        if (mstEdges == null || mstEdges.Count == 0)
            return;

        Gizmos.color = new Color(1f, 0f, 0f, 0.8f); // Rojo
        foreach (var edge in mstEdges)
        {
            Vector3 p1 = new Vector3(edge.p1.x, 0.03f, edge.p1.y);
            Vector3 p2 = new Vector3(edge.p2.x, 0.03f, edge.p2.y);
            Gizmos.DrawLine(p1, p2);
        }
    }

    void DrawDelaunayGizmos()
    {
        if (delaunayTriangles == null || delaunayTriangles.Count == 0)
            return;

        Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.2f); // Azul semi-transparente
        foreach (var triangle in delaunayTriangles)
        {
            Vector3 p1 = new Vector3(triangle.p1.x, 0.02f, triangle.p1.y);
            Vector3 p2 = new Vector3(triangle.p2.x, 0.02f, triangle.p2.y);
            Vector3 p3 = new Vector3(triangle.p3.x, 0.02f, triangle.p3.y);

            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawLine(p2, p3);
            Gizmos.DrawLine(p3, p1);
        }
    }
}
