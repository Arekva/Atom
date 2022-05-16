namespace Atom.Engine.Tree;



public class Node<T>
{
    public const u32 LOCATION_MASK = 0b111;
    
    
    private T _data;

    private readonly u128 _location;
    
    private WeakReference<Node<T>>? _parent;
    
    private Node<T>[]? _branches;



    public ref T Data => ref _data;

    public u128 Location => _location;

    public u32 Depth => DepthInternal();

    public bool IsLeaf => LeafInternal();

    public bool HasBranches => _branches != null;


    public Node() // root node
    {
        _parent   = null;
        _location = 0b1; // end bit

        DepthInternal();
    }

    public Node(Node<T> parent, u32 branchIndex)
    {
        _parent = new WeakReference<Node<T>>(parent);
        
        // 0b000 = 0 = -X -Y -Z = left  bottom backward
        // 0b001 = 1 = -X -Y +Z = left  bottom forward
        // 0b010 = 2 = -X +Y -Z = left  up     backward
        // 0b011 = 3 = -X +Y +Z = left  up     forward
        // 0b100 = 4 = +X -Y -Z = right up     backward
        // 0b101 = 5 = +X -Y +Z = right up     forward
        // 0b110 = 6 = +X +Y -Z = right up     backward
        // 0b111 = 7 = +X +Y +Z = right up     forward
        
        _location = (parent._location << 3) | branchIndex;
    }

    public override bool Equals(object? obj) => obj is Node<T> node && node._location.Equals(_location);

    public override i32 GetHashCode() => _location.GetHashCode();

    /*public override String ToString()
    {
        
    }*/

    /*private u32 GetDepth()
    {
        
    }*/

    private u32 DepthInternal()
    {
        for (i32 i = 126, depth = 42; i >= 0; i -= 3, --depth)
        {
            u128 loc = _location >> i;
            if (loc == 1) return (u32)depth;
        }

        throw new Exception($"Location {_location} isn't valid.");
    }

    private bool LeafInternal() => (_location >> 126).Low == 1;

    public u32 BranchIndexAtDepth(u32 depth) => (u32)(_location >> (i32)((Octree<T>.MAX_SUBDIVISIONS - depth) * Octree<T>.DIMENSION)).Low & Node<T>.LOCATION_MASK;

    public void Split()
    {
        if (HasBranches)
        {
            Log.Warning("Cannot split node: it is already split.");
            return;
        }

        _branches = new Node<T>[Octree<T>.BRANCH_COUNT];
        for (u32 i = 0; i < Octree<T>.BRANCH_COUNT; i++)
        {
            _branches[i] = new Node<T>(parent: this, i);
        }
    }
}