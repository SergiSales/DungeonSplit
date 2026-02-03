using System;
using UnityEngine;
using System.Collections.Generic;

public class Test3 : MonoBehaviour
{
    // Previsualizar el BSP en la escena usando Gizmos

    [Header("Dungeon Settings")]
    public int dungeonWidth = 100;
    public int dungeonHeight = 100;
    public int minRoomSize = 10;
    public int seed;

    private BSPNode root;
    private List<Room> rooms;

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
