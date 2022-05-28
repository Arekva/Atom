using System.Diagnostics.CodeAnalysis;

namespace Atom.Engine.Tree;

public abstract class Octree
{
    public const u32 DIMENSION = 3;
    public const u32 BRANCH_COUNT = 2 * 2 * 2; // 2^DIMENSION
    public const u32 MAX_SUBDIVISIONS = 42U;
}

public class Octree<T> : Octree, IDisposable
{
    private Node<T> _root;

    public Node<T> Root
    {
        get => _root;
        protected set => _root = value;
    }

    public event Action<Node<T>> OnNodeCreated;
    public event Action<Node<T>, Node<T>[]> OnNodeSplit;
    

    public Octree() { }

    public bool TryFindNode(u128 location, out Node<T> node)
    {
        if (_root == null!) throw new Exception("Octree is not initialized");
        
        u128 one_u128 = new(high: 0, low: 1);
        for (i32 depth = (i32)MAX_SUBDIVISIONS; depth >= 0; --depth)
        {
            u128 loc = location >> (depth * 3);
            if (loc == one_u128)
            {
                // found location's desired depth
                // now travel tree in the other way to find the node

                node = _root;

                for (i32 n = depth-1; n >= 0; --n)
                {
                    if (!node.HasBranches) return false;
                    
                    u8 local_location = (u8)((u32)(location >> (n * 3)).Low & Node.LOCATION_MASK);
                    node = node[local_location];
                }

                return true;
            }
        }

        node = null;
        return false;
    }

    public void Initialize()
    {
        _root = new Node<T>();
        _root.OnCreate += OnNodeCreated;
        _root.OnSplit += OnNodeSplit;
        _root.TriggerOnCreate();
    }

    public virtual void Delete(bool doCollect = true)
    {
        _root = null!;

        if (doCollect)
        {
            GC.Collect(2);
        }
    }

    public void Dispose() => Delete();
    ~Octree() => Dispose();
}