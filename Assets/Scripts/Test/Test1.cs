using UnityEngine;

/// <summary>
/// Script de prueba para verificar el funcionamiento del generador BSP.
/// No genera gráficos, solo imprime información en consola.
/// 
/// ----------------- Prueba hecha por chatGPT -----------------
/// </summary>
public class Test1 : MonoBehaviour
{

    public int dungeonWidth = 100;
    public int dungeonHeight = 100;
    public int minRoomSize = 10;
    public int seed;
    void Start()
    {
        // Se instancia el generador BSP con un tamaño mínimo y una semilla
        seed = UnityEngine.Random.Range(0, 100000);
        BSPGenerator generator = new BSPGenerator(minRoomSize, seed);

        // Área total de la mazmorra (100x100 unidades)
        BSPNode root = generator.Generate(new IntRect(0, 0, dungeonWidth, dungeonHeight));

        // Imprime la estructura del árbol BSP en la consola
        PrintNode(root, 0);
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

        Debug.Log($"{indent}Área: {node.Area.width} x {node.Area.height}");

        // Si no es hoja, seguimos recorriendo los hijos
        if (!node.IsLeaf)
        {
            PrintNode(node.left, depth + 1);
            PrintNode(node.right, depth + 1);
        }
    }
}
