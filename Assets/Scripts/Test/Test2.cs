using System;
using System.Diagnostics;
using UnityEngine;
using System.Collections.Generic;

public class Test2 : MonoBehaviour
{
    // Previsualizar el BSP en la escena usando Gizmos

    [Header("Dungeon Settings")]
    public int dungeonWidth = 100;
    public int dungeonHeight = 100;
    public int minRoomSize = 10;
    public int seed;

    private BSPNode root;

    void Start()
    {
        Stopwatch totalTimer = Stopwatch.StartNew();

        //Generar Mazmorra al iniciar escena
        seed = UnityEngine.Random.Range(0, 100000);

        BSPGenerator generator = new BSPGenerator(minRoomSize, seed);
        root = generator.Generate(new IntRect(0, 0, dungeonWidth, dungeonHeight));
        List<Room> rooms = generator.CreateRooms(root);
        
        

        totalTimer.Stop();
        UnityEngine.Debug.Log($"[Test2] Generated rooms: {rooms.Count}");
        UnityEngine.Debug.Log($"[Test2] Total generation time: {totalTimer.ElapsedMilliseconds}ms");
    }
    void OnDrawGizmos()
    {
        if (root == null)
        {
            return;
        }
        DrawNodeGizmos(root);
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
}
