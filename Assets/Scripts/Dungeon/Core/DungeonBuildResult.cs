using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DungeonBuildResult : MonoBehaviour
{
    public List<Room> Rooms { get; set; } = new List<Room>();
    public List<MSTEdge> MstEdges { get; set; } = new List<MSTEdge>();
    public int GeneratedRoomCount => Rooms.Count;
}
