using System;
using System.Collections.Generic;
using UnityEngine;

public class BSPGenerator
{
    // Dividir recursivamente un área inicial en subáreas
    private int minSize; // Tamaño mínimo para dividir
    private System.Random rd;

    public int padding = 4; // Espacio para pasillos entre habitaciones

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
        if(node.Area.width <= minSize * 2 && node.Area.height <= minSize * 2){
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
            int innerWidth = node.Area.width - padding * 2;
            int innerHeight = node.Area.height - padding * 2;

            int roomX = node.Area.x + padding;
            int roomY = node.Area.y + padding;
            int roomWidth = innerWidth;
            int roomHeight = innerHeight;

            if (innerWidth >= minSize && innerHeight >= minSize)
            {
                roomWidth = rd.Next(minSize, innerWidth + 1);
                roomHeight = rd.Next(minSize, innerHeight + 1);

                int maxX = node.Area.x + padding + (innerWidth - roomWidth);
                int maxY = node.Area.y + padding + (innerHeight - roomHeight);

                roomX = rd.Next(node.Area.x + padding, maxX + 1);
                roomY = rd.Next(node.Area.y + padding, maxY + 1);
            }

            IntRect roomBounds = new IntRect(roomX, roomY, roomWidth, roomHeight);
            Room room = new Room(roomBounds);
            node.Room = room;
            rooms.Add(room);
        }
        else
        {
            CollectLeafRooms(node.left, rooms);
            CollectLeafRooms(node.right, rooms);
        }
    }



// Conectar habitaciones recursivamente, creando pasillos entre ellas
    private Room ConnectRooms(BSPNode node, List<Corridor> corridors)
    {
        if (node == null)
        {
            return null;
        }

        if (node.IsLeaf)
        {
            return node.Room;
        }

        Room leftRoom = ConnectRooms(node.left, corridors);
        Room rightRoom = ConnectRooms(node.right, corridors);

        if (leftRoom != null && rightRoom != null)
        {
            corridors.Add(CreateCorridor(leftRoom.center, rightRoom.center));
        }

        node.Room = rd.NextDouble() < 0.5 ? leftRoom : rightRoom;
        return node.Room;
    }

// Crear el pasillo indicado
    private Corridor CreateCorridor(Vector2Int start, Vector2Int end)
    {
        if (start.x == end.x || start.y == end.y)
        {
            return new Corridor(start, end);
        }

        bool horizontalFirst = rd.NextDouble() < 0.5;
        Vector2Int bend = horizontalFirst
            ? new Vector2Int(end.x, start.y)
            : new Vector2Int(start.x, end.y);

        return new Corridor(start, end, bend);
    }

}
