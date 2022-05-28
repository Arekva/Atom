using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace Atom.Engine.Tree;

public abstract class Node
{
    public const u32 LOCATION_MASK = 0b111;
    
    protected static readonly char[] LOC_CHAR_MAP =
    {
        'A', // 000 = 0 = -X -Y -Z = left  down backward (A)
        'E', // 001 = 1 = -X -Y +Z = left  down forward  (E)
        'C', // 010 = 2 = -X +Y -Z = left  up   backward (C)
        'G', // 011 = 3 = -X +Y +Z = left  up   forward  (G)
        'B', // 100 = 4 = +X -Y -Z = right down backward (B)
        'F', // 101 = 5 = +X -Y +Z = right down forward  (F)
        'D', // 110 = 6 = +X +Y -Z = right up   backward (D)
        'H', // 111 = 7 = +X +Y +Z = right up   forward  (H)
    };

    protected static readonly u32[] CHAR_LOC_MAP =
    {
        0b000, // A 0 => 0
        0b100, // B 1 => 4
        0b010, // C 2 => 2
        0b110, // D 3 => 6
        0b001, // E 4 => 1
        0b101, // F 5 => 5
        0b011, // G 6 => 3
        0b111, // H 7 => 7
    };
}


public class Node<T> : Node
{
    // Structure data
    private             T                          _data        ;
    private             Node<T>[]?                 _branches    ;
    private readonly    u128                       _location    ;
    private readonly    WeakReference<Node<T>>?    _parent      ;


    
    public ref T Data => ref _data;

    public u128 Location => _location;

    public u32 Depth => DepthInternal();

    public bool IsLeaf => LeafInternal();

    public bool HasBranches => _branches != null;

    public bool HasParent => _parent != null;

    public u8 LocalLocation => (u8)(_location.Low & LOCATION_MASK);

    public u128 ParentLocation => _location >> (i32)Octree.DIMENSION;
    
    public Node<T> Parent
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)] get
        {
            // if parent is null, then user's fault not to have checked for HasParent before.
            // to be as fast as possible, NO NULL CHECK!!!!!!
            _parent!.TryGetTarget(out Node<T>? parent);
            return parent!;
        }
    }
    
    public Node<T> this[u32 branchIndex] => _branches![branchIndex];
    
    public Node<T> this[char branchName] => _branches![CHAR_LOC_MAP[branchName - 'A']];
    
    public event Action<Node<T>>? OnDelete;
    public event Action<Node<T>> OnCreate;

    public event Action<Node<T>, Node<T>[]>? OnSplit;
    
    public Node() // root node
    {
        _parent   = null;
        _location = 0b1; // end bit

        DepthInternal();
    }

    public Node(Node<T> parent, u32 branchIndex)
    {
        _parent = new WeakReference<Node<T>>(parent);
        
        _location = (parent._location << 3) | branchIndex;
    }

    internal void TriggerOnCreate()
    {
        OnCreate(this);
    }
    
    ~Node() => OnDelete?.Invoke(this);

    public override bool Equals(object? obj) => obj is Node<T> node && node._location.Equals(_location);

    public override i32 GetHashCode() => _location.GetHashCode();

    public override string ToString()
    {
        if (Location == 1) 
            return "Root";
        
        StringBuilder builder = new(capacity: (i32)Depth);
        for (u32 i = 1; i < Depth + 1; i++)
            builder.Append(LOC_CHAR_MAP[BranchIndexAtDepth(i)]);

        return builder.ToString();
    }
    public string ToString(bool doColor)
    {
        if (Location == 1) 
            return "Root";
        
        StringBuilder builder = new(capacity: (i32)Depth);
        for (u32 i = 1; i < Depth + 1; i++)
            builder.Append(LOC_CHAR_MAP[BranchIndexAtDepth(i)]);

        return builder.ToString();
    }

    private u32 DepthInternal()
    {
        u128 one_u128 = new(high: 0, low: 1);
        for (i32 depth = (i32)Octree.MAX_SUBDIVISIONS; depth >= 0; --depth)
        {
            u128 loc = _location >> (depth * 3);
            if (loc == one_u128) return (u32)depth;
        }

        throw new Exception($"Location {_location} isn't valid.");
    }

    private bool LeafInternal() => (_location >> 126).Low == 1;

    public u32 BranchIndexAtDepth(u32 depth) => (u32)(_location >> (i32)((Depth - depth) * Octree.DIMENSION)).Low & LOCATION_MASK;

    public void Split()
    {
        if (HasBranches)
        {
            Log.Warning("Cannot split node: it is already split.");
            return;
        }

        if (IsLeaf)
        {
            Log.Warning("Cannot split node: it is leaf.");
            return;
        }

        _branches = new Node<T>[Octree.BRANCH_COUNT];
        for (u32 i = 0; i < Octree.BRANCH_COUNT; i++)
        {
            _branches[i] = new Node<T>(parent: this, i);
        }
        
        OnSplit?.Invoke(this, _branches);
    }

    public void Merge() => _branches = null;

    public Node<T>[] GetNeighbours(Directions directions)
    {
        Node<T>? neighbor = GetNeighborSameOrGreaterSize(directions);
        return GetNeighborSmallerSize(neighbor, directions);
    }

#region Neighbours
    [SuppressMessage("ReSharper", "AccessToModifiedClosure")]
    private Node<T>? GetNeighborSameOrGreaterSize(Directions direction)
    {
        if (Depth == 0)
        {
            return null;
        }

        if (direction is Directions.None or Directions.All)
        {
            throw new ArgumentException("A non trivial direction must be set.", nameof(direction));
        }

        byte locloc = LocalLocation;

        byte mask = 0b000;
        
        Node<T>? negative_subroutine()
        {
            if ((locloc & mask) == mask) return Parent._branches![locloc & ~mask];
            
            Node<T>? node = Parent!.GetNeighborSameOrGreaterSize(direction);
            if (node is null || !node.HasBranches) return node;
            
            return node._branches![locloc | mask];
        }

        Node<T>? positive_subroutine()
        {
            if ((locloc & mask) == 0) return Parent!._branches![locloc | mask];
            
            Node<T>? node = Parent!.GetNeighborSameOrGreaterSize(direction);
            if (node is null || !node.HasBranches) return node;
            
            return node._branches![locloc & ~mask];
        }

        Func<Node<T>?> routine;

        switch (direction)
        {
            case Directions.Left:
                routine = negative_subroutine;
                mask = 0b100;
                break;
            case Directions.Right:
                routine = positive_subroutine;
                mask = 0b100;
                break;
            case Directions.Down:
                routine = negative_subroutine;
                mask = 0b010;
                break;
            case Directions.Up:
                routine = positive_subroutine;
                mask = 0b010;
                break;
            case Directions.Backward:
                routine = negative_subroutine;
                mask = 0b001;
                break;
            case Directions.Forward:
                routine = positive_subroutine;
                mask = 0b001;
                break;
            default:
                throw new Exception($"Invalid direction type ({direction})");
        }

        return routine();
    }
    private Node<T>[] GetNeighborSmallerSize(Node<T>? neighbor, Directions direction)
    {
        if (neighbor is null) return Array.Empty<Node<T>>();
        
        List<Node<T>> candidates = new(27) { neighbor };
        List<Node<T>> neighbors = new(27);

        Action<List<Node<T>>> candidates_adder = null!;

        if (direction.HasFlag(Directions.Left    )) candidates_adder = cands => {
            for (i32 z = 0; z < 2; z++)
            for (i32 y = 0; y < 2; y++)
                cands.Add(cands[0]._branches![0b100 | y << 1 | z]);
        }; else if (direction.HasFlag(Directions.Right   )) candidates_adder = cands => {
            for (i32 z = 0; z < 2; z++)
            for (i32 y = 0; y < 2; y++)
                cands.Add(cands[0]._branches![y << 1 | z]);
        }; 
        else if (direction.HasFlag(Directions.Down    )) candidates_adder = cands => {
            for (i32 z = 0; z < 2; z++)
            for (i32 x = 0; x < 2; x++)
                cands.Add(cands[0]._branches![x << 2 | 0b010 | z]);
        }; else if (direction.HasFlag(Directions.Up      )) candidates_adder = cands => {
            for (i32 z = 0; z < 2; z++)
            for (i32 x = 0; x < 2; x++)
                cands.Add(cands[0]._branches![x << 2 | z]);
        }; else if (direction.HasFlag(Directions.Backward)) candidates_adder = cands => {
            for (i32 y = 0; y < 2; y++)
            for (i32 x = 0; x < 2; x++)
                cands.Add(cands[0]._branches![x << 2 | y << 1 | 0b001]);
        }; else if (direction.HasFlag(Directions.Forward )) candidates_adder = cands => {
            for (i32 y = 0; y < 2; y++)
            for (i32 x = 0; x < 2; x++)
                cands.Add(cands[0]._branches![x << 2 | y << 1]);
        };

        while (candidates.Count > 0)
        {
            Node<T> candidate = candidates[0];
            if (!candidate.HasBranches) neighbors.Add(candidate);
            else candidates_adder(candidates);
            
            candidates.Remove(candidate);
        }

        return neighbors.ToArray();
    }
#endregion

}