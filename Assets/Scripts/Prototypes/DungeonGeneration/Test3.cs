using System;
using System.Diagnostics;
using UnityEngine;
using System.Collections.Generic;

public class Test3 : MonoBehaviour
{
    // Previsualizar el BSP en la escena usando Gizmos

    [Header("Dungeon Settings")]
    public int dungeonWidth = 100;
    public int dungeonHeight = 100;
    public int minRoomSize = 10;
    public int maxRoomSize = 20;
    public int seed;

    private BSPNode root;
    private List<Room> rooms;

    void Start()
    {
        Stopwatch totalTimer = Stopwatch.StartNew();

        //Generar Mazmorra al iniciar escena
        seed = UnityEngine.Random.Range(0, 100000);

        BSPGenerator generator = new BSPGenerator(minRoomSize, maxRoomSize, seed);
        root = generator.Generate(new IntRect(0, 0, dungeonWidth, dungeonHeight));
        rooms = generator.CreateRooms(root);

        

        totalTimer.Stop();
        UnityEngine.Debug.Log($"[Test3] Generated rooms: {rooms.Count}");
        UnityEngine.Debug.Log($"[Test3] Total generation time: {totalTimer.ElapsedMilliseconds}ms");
    }
    void OnDrawGizmos()
    {
        if (root == null)
        {
            return;
        }
        DrawNodeGizmos(root);
        DrawRoomsGizmos(root);
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

}
