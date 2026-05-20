using System.Collections.Generic;

public sealed class DungeonBuildResult
{
    public List<Room> Rooms { get; }
    public List<MSTEdge> MstEdges { get; }
    public int GeneratedRoomCount => Rooms.Count;

    public DungeonBuildResult(List<Room> rooms, List<MSTEdge> mstEdges)
    {
        Rooms = rooms ?? new List<Room>();
        MstEdges = mstEdges ?? new List<MSTEdge>();
    }
}
