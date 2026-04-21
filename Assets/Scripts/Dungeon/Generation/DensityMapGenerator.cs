using UnityEngine;

public class DensityMapGenerator
{
    private int dungeonWidth;
    private int dungeonHeight;
    private int seed;
    
    public DensityMapGenerator(int width, int height, int seed)
    {
        this.dungeonWidth = width;
        this.dungeonHeight = height;
        this.seed = seed;
    }
    
    public float[,] GeneratePerlinNoise(float perlinScale)
    {
        float[,] densityMap = new float[dungeonWidth, dungeonHeight];
        
        for (int x = 0; x < dungeonWidth; x++)
        {
            for (int y = 0; y < dungeonHeight; y++)
            {
                float noiseX = (x + seed * 0.1f) * perlinScale;
                float noiseY = (y + seed * 0.2f) * perlinScale;
                densityMap[x, y] = Mathf.PerlinNoise(noiseX, noiseY);
            }
        }
        
        return densityMap;
    }
    
    public void FilterRoomsByDensity(ref System.Collections.Generic.List<Room> rooms, float[,] densityMap, float densityThreshold)
    {
        System.Collections.Generic.List<Room> filteredRooms = new System.Collections.Generic.List<Room>();
        
        foreach (Room room in rooms)
        {
            float avgDensity = 0f;
            int count = 0;
            
            for (int x = room.bounds.x; x < room.bounds.x + room.bounds.width && x < dungeonWidth; x++)
            {
                for (int y = room.bounds.y; y < room.bounds.y + room.bounds.height && y < dungeonHeight; y++)
                {
                    avgDensity += densityMap[x, y];
                    count++;
                }
            }
            
            if (count > 0)
            {
                avgDensity /= count;
            }
            
            if (avgDensity >= densityThreshold)
            {
                filteredRooms.Add(room);
            }
        }
        
        rooms = filteredRooms;
    }
}
