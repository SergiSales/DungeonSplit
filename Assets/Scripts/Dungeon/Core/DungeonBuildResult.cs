using System.Collections.Generic;
using UnityEngine;


public class DungeonBuildResult : MonoBehaviour
{
    public List<Room> Rooms { get; set; }
    public List<MSTEdge> MstEdges { get; set; }
    public DungeonBuildResult(List<Room> rooms, List<MSTEdge> mstEdges)
    {
        Rooms = rooms ?? new List<Room>();
        MstEdges = mstEdges ?? new List<MSTEdge>();
    }
}