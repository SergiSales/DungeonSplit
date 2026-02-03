using System.Security.AccessControl;
using System;
using System.Collections.Generic;
using UnityEngine;

public class BSPGenerator
{
    // Dividir recursivamente un área inicial en subáreas
    private int minSize; // Tamaño mínimo para dividir
    private System.Random rd;

    public int padding = 2; // Espacio para pasillos entre habitaciones

    public BSPGenerator(int minSize, int seed)
    {
        this.minSize = minSize;
        rd = new System.Random(seed);
    }

    public BSPNode Generate(IntRect rootArea)
    {
        //Root area es el área total de la mazmorra
        BSPNode root = new BSPNode(rootArea);
        SplitNode(root);
        return root; // Devuelve el nodo raiz del arbol
    }

    private void SplitNode(BSPNode node)
    {
        // Dividir recursivamente el nodo
        Debug.Log(node);
        if(node.Area.width < minSize * 2 && node.Area.height < minSize * 2){
            Debug.Log(node + " No se puede dividir más.");
            return; // Si el área es demasiado pequeña, no dividir más
        }
        bool splitHorizontal = rd.NextDouble() > 0.5; //Decidir si se divide horizontal o vertical
        
        if(node.Area.width > node.Area.height)
            splitHorizontal = false;
        else if(node.Area.height > node.Area.width)
            splitHorizontal = true;

        if (splitHorizontal)
        {
            // Dividir horizontalmente
            int splitY = rd.Next(minSize, node.Area.height - minSize);
            
            node.left = new BSPNode(new IntRect(node.Area.x, node.Area.y, node.Area.width, splitY));
            node.right = new BSPNode(new IntRect(node.Area.x, node.Area.y + splitY, node.Area.width, node.Area.height - splitY));
        }
        else
        {
            // Dividir verticalmente
            int splitX = rd.Next(minSize, node.Area.width - minSize);
            
            node.left = new BSPNode(new IntRect(node.Area.x, node.Area.y, splitX, node.Area.height));
            node.right = new BSPNode(new IntRect(node.Area.x + splitX, node.Area.y, node.Area.width - splitX, node.Area.height));
        }

        // Llamadas recursivas
        SplitNode(node.left);
        SplitNode(node.right);        
    }

    // Convertir hojas en habitaciones

    public List<Room> CreateRooms(BSPNode node)
    {
        List<Room> rooms = new List<Room>();
        CollectLeafRooms(node, rooms);
        return rooms;
    }

    public void CollectLeafRooms(BSPNode node, List<Room> rooms)
    {
        // Crear una sala por hoja, recorrer el arbol recursivamente
        if (node.IsLeaf)
        {
             // Espacio para pasillos
            IntRect roomBounds = new IntRect(
                node.Area.x + padding,
                node.Area.y + padding,
                node.Area.width - padding * 2,
                node.Area.height - padding * 2
            );
            rooms.Add(new Room(roomBounds));
        }
        else
        {
            CollectLeafRooms(node.left, rooms);
            CollectLeafRooms(node.right, rooms);
        }
    }

}