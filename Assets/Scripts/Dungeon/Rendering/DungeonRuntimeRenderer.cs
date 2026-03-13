using System.Collections.Generic;
using UnityEngine;

public class DungeonRuntimeRenderer : MonoBehaviour
{
    [Header("Dungeon Settings")]
    public int dungeonWidth = 100;
    public int dungeonHeight = 100;
    public int minRoomSize = 10;
    public int seed = 0;
    public bool useRandomSeed = true;

    [Header("Rendering Settings")]
    public GameObject floorPrefab;
    public float cellSize = 1f;
    public Vector3 cellOffset = new Vector3(0.5f, 0f, 0.5f);
    public int corridorWidth = 1;
    public float uvScale = 1f;

    private Transform generatedParent;
    private Material sharedMaterial;

    private void Start()
    {
        Generate();
    }

    public void Generate()
    {
        if (useRandomSeed)
        {
            seed = Random.Range(0, int.MaxValue);
        }

        if (floorPrefab == null)
        {
            Debug.LogError("DungeonRuntimeRenderer: floorPrefab no está asignado.");
            return;
        }

        MeshRenderer prefabRenderer = floorPrefab.GetComponent<MeshRenderer>();
        if (prefabRenderer == null || prefabRenderer.sharedMaterial == null)
        {
            Debug.LogError("DungeonRuntimeRenderer: floorPrefab no tiene MeshRenderer con material.");
            return;
        }
        sharedMaterial = prefabRenderer.sharedMaterial;

        ClearGenerated();

        BSPGenerator generator = new BSPGenerator(minRoomSize, seed);
        BSPNode root = generator.Generate(new IntRect(0, 0, dungeonWidth, dungeonHeight));
        List<Room> rooms = generator.CreateRooms(root);
        List<Corridor> corridors = generator.CreateCorridors(root);

        GameObject parent = new GameObject("mazmorra");
        generatedParent = parent.transform;
        generatedParent.SetParent(transform, false);

        Transform roomsParent = new GameObject("salas").transform;
        roomsParent.SetParent(generatedParent, false);

        Transform corridorsParent = new GameObject("pasillos").transform;
        corridorsParent.SetParent(generatedParent, false);

        for (int i = 0; i < rooms.Count; i++)
        {
            Room room = rooms[i];
            GameObject roomObj = new GameObject($"sala_{i}");
            roomObj.transform.SetParent(roomsParent, false);

            MeshFilter filter = roomObj.AddComponent<MeshFilter>();
            MeshRenderer renderer = roomObj.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = sharedMaterial;
            filter.sharedMesh = BuildRoomMesh(room);
        }

        for (int i = 0; i < corridors.Count; i++)
        {
            Corridor corridor = corridors[i];
            HashSet<Vector2Int> corridorCells = new HashSet<Vector2Int>();
            if (corridor.hasBend)
            {
                AddCorridorCells(corridorCells, corridor.start, corridor.bend);
                AddCorridorCells(corridorCells, corridor.bend, corridor.end);
            }
            else
            {
                AddCorridorCells(corridorCells, corridor.start, corridor.end);
            }

            GameObject corridorObj = new GameObject($"pasillo_{i}");
            corridorObj.transform.SetParent(corridorsParent, false);

            MeshFilter filter = corridorObj.AddComponent<MeshFilter>();
            MeshRenderer renderer = corridorObj.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = sharedMaterial;
            filter.sharedMesh = BuildMeshFromCells(corridorCells);
        }
    }

    private void ClearGenerated()
    {
        Transform existing = transform.Find("mazmorra");
        if (existing == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(existing.gameObject);
        }
        else
        {
            DestroyImmediate(existing.gameObject);
        }
    }

    private void AddCorridorCells(HashSet<Vector2Int> cells, Vector2Int start, Vector2Int end)
    {
        Vector2Int current = start;
        AddCellWithWidth(cells, current);

        while (current.x != end.x)
        {
            current.x += current.x < end.x ? 1 : -1;
            AddCellWithWidth(cells, current);
        }

        while (current.y != end.y)
        {
            current.y += current.y < end.y ? 1 : -1;
            AddCellWithWidth(cells, current);
        }
    }

    private void AddCellWithWidth(HashSet<Vector2Int> cells, Vector2Int cell)
    {
        int radius = Mathf.Max(0, corridorWidth - 1);
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                if (dx != 0 && dy != 0)
                {
                    continue;
                }

                cells.Add(new Vector2Int(cell.x + dx, cell.y + dy));
            }
        }
    }

    private Mesh BuildRoomMesh(Room room)
    {
        int cellsCount = room.bounds.width * room.bounds.height;
        List<Vector3> vertices = new List<Vector3>(cellsCount * 4);
        List<int> triangles = new List<int>(cellsCount * 6);
        List<Vector3> normals = new List<Vector3>(cellsCount * 4);
        List<Vector2> uvs = new List<Vector2>(cellsCount * 4);

        float half = cellSize * 0.5f;
        for (int x = room.bounds.x; x < room.bounds.xMax; x++)
        {
            for (int y = room.bounds.y; y < room.bounds.yMax; y++)
            {
                float cx = x * cellSize + cellOffset.x;
                float cz = y * cellSize + cellOffset.z;
                float x0 = cx - half;
                float x1 = cx + half;
                float z0 = cz - half;
                float z1 = cz + half;
                AddQuad(vertices, triangles, normals, uvs, x0, z0, x1, z1);
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "RoomMesh";
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateBounds();
        return mesh;
    }

    private Mesh BuildMeshFromCells(HashSet<Vector2Int> cells)
    {
        List<Vector3> vertices = new List<Vector3>(cells.Count * 4);
        List<int> triangles = new List<int>(cells.Count * 6);
        List<Vector3> normals = new List<Vector3>(cells.Count * 4);
        List<Vector2> uvs = new List<Vector2>(cells.Count * 4);

        float half = cellSize * 0.5f;
        foreach (Vector2Int cell in cells)
        {
            float cx = cell.x * cellSize + cellOffset.x;
            float cz = cell.y * cellSize + cellOffset.z;
            float x0 = cx - half;
            float x1 = cx + half;
            float z0 = cz - half;
            float z1 = cz + half;
            AddQuad(vertices, triangles, normals, uvs, x0, z0, x1, z1);
        }

        Mesh mesh = new Mesh();
        mesh.name = "CellMesh";
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateBounds();
        return mesh;
    }

    private void AddQuad(
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector3> normals,
        List<Vector2> uvs,
        float x0,
        float z0,
        float x1,
        float z1)
    {
        float scale = Mathf.Max(0.0001f, uvScale);
        float u0 = 0f;
        float u1 = 1f * scale;
        float v0 = 0f;
        float v1 = 1f * scale;

        int start = vertices.Count;
        vertices.Add(new Vector3(x0, 0f, z0));
        vertices.Add(new Vector3(x1, 0f, z0));
        vertices.Add(new Vector3(x1, 0f, z1));
        vertices.Add(new Vector3(x0, 0f, z1));

        triangles.Add(start + 0);
        triangles.Add(start + 2);
        triangles.Add(start + 1);
        triangles.Add(start + 0);
        triangles.Add(start + 3);
        triangles.Add(start + 2);

        normals.Add(Vector3.up);
        normals.Add(Vector3.up);
        normals.Add(Vector3.up);
        normals.Add(Vector3.up);

        uvs.Add(new Vector2(u0, v0));
        uvs.Add(new Vector2(u1, v0));
        uvs.Add(new Vector2(u1, v1));
        uvs.Add(new Vector2(u0, v1));
    }
}
