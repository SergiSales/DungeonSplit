public class BSPNode
{
    //Nodo del BSP Tree, se usa en BSPGenerator
    public IntRect Area;
    public BSPNode left;
    public BSPNode right;

    
    public bool IsLeaf => left == null && right == null;

    public BSPNode(IntRect area)
    {
        Area = area;
    }
}