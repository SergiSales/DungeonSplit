using System.Diagnostics;
using UnityEngine;
using System.Collections.Generic;


/// <summary>
/// Script de prueba para verificar el funcionamiento del generador BSP.
/// No genera gráficos, solo imprime información en consola.
/// 
/// </summary>
public class Test1 : MonoBehaviour
{

    public int dungeonWidth = 100;
    public int dungeonHeight = 100;
    public int minRoomSize = 10;
    public int maxRoomSize = 20;
    public int seed;
    void Start()
    {
        Stopwatch totalTimer = Stopwatch.StartNew();

        // Se instancia el generador BSP con un tamaño mínimo y una semilla
        seed = UnityEngine.Random.Range(0, 100000);

        BSPGenerator generator = new BSPGenerator(minRoomSize, maxRoomSize, seed);

        // Área total de la mazmorra (100x100 unidades)
        BSPNode root = generator.Generate(new IntRect(0, 0, dungeonWidth, dungeonHeight));
        List<Room> rooms = generator.CreateRooms(root);



        // Imprime la estructura del árbol BSP en la consola
        PrintNode(root, 0);
        
        totalTimer.Stop();
        UnityEngine.Debug.Log($"[Test1] Generated rooms: {rooms.Count}");
        UnityEngine.Debug.Log($"[Test1] Total generation time: {totalTimer.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// Recorre el árbol BSP e imprime información de cada nodo.
    /// </summary>
    /// <param name="node">Nodo actual</param>
    /// <param name="depth">Profundidad en el árbol (para indentación)</param>
    void PrintNode(BSPNode node, int depth)
    {
        // Indentación visual para representar jerarquía
        string indent = new string('-', depth * 2);

        UnityEngine.Debug.Log($"{indent}Área: {node.Area.width} x {node.Area.height}");

        // Si no es hoja, seguimos recorriendo los hijos
        if (!node.IsLeaf)
        {
            PrintNode(node.left, depth + 1);
            PrintNode(node.right, depth + 1);
        }
    }
}
