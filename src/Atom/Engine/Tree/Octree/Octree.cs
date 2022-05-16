namespace Atom.Engine.Tree;

public class Octree<T>
{
    public const u32 DIMENSION = 3;
    public const u32 BRANCH_COUNT = 2 * 2 * 2; // 2^DIMENSION
    public const u32 MAX_SUBDIVISIONS = 42U;
    
    
    private Node<T> _root;

    public Octree()
    {
        _root = new Node<T>();
    }
}