using System.Diagnostics.CodeAnalysis;

namespace Atom.Engine.Tree;

public abstract class Octree
{
    public const u32 DIMENSION = 3;
    public const u32 BRANCH_COUNT = 2 * 2 * 2; // 2^DIMENSION
    public const u32 MAX_SUBDIVISIONS = 42U; 
    //todo: fix world => grid position to increase that.
    // everything is fine but that, for now.
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


    public IEnumerable<Node<T>> Nodes
    {
        get
        {
            if (_root == null!) yield break;

            yield return _root;
            foreach (Node<T> sub_node in _root.SubNodes)
            {
                yield return sub_node;
            }
        }
    }



    public Octree() { }

    public bool TryFindNode(ReadOnlySpan<char> code, out Node<T> node)
    {
        u128 location = new(0, 1);
        for (i32 i = 0; i < code.Length; i++)
        {
            ref readonly char branch_code = ref code[i];
            ref readonly u8 branch = ref Node.CHAR_LOC_MAP[branch_code - 'A'];
            location = ((location << (i32)DIMENSION) | branch );
        }

        return TryFindNode(location, out node);
    }
    
    
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
                    
                    u8 local_location = (u8)((u32)(location >> (n * (i32)DIMENSION)).Low & Node.LOCATION_MASK);
                    node = node[local_location];
                }

                return true;
            }
        }

        throw new Exception("Invalid location submitted.");
    }

    public Node<T> SubdivideTo(u128 location)
    {
        // get base node to subdivide from
        bool found_node = TryFindNode(location, out Node<T> node);
        if (found_node) return node; // no need to subdivide anything

        u32 from_depth = node.Depth;

        u128 one = new(0UL, 1UL);

        location >>= (i32)(from_depth * DIMENSION);

        u32 current_depth = from_depth;
        u128 current_location = location;
        while (current_location != one)
        {
            if (node.IsLeaf) break; // safety check

            u128 next_location = location >> (i32)(((MAX_SUBDIVISIONS - 1) - current_depth) * DIMENSION);
            u8 next_loc = (u8)(next_location.Low & 0b111);
            node = node.Subdivide()![next_loc];

            ++current_depth;
            current_location = next_location;
        }

        return node;
    }

    public void Initialize()
    {
        _root = new Node<T>();
        _root.OnCreate += OnNodeCreated;
        _root.OnSubdivide += OnNodeSplit;
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