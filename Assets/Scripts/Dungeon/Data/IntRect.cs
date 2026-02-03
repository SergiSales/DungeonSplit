public struct IntRect
{
    //Esto sirve para representar regiones del mapa
    public int x, y, width, height;
    public IntRect(int x, int y, int width, int height)
    {
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
    }

    public int xMax => x + width;
    public int yMax => y + height;
}