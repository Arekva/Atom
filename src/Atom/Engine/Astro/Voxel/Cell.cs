using System.Runtime.CompilerServices;
using Atom.Engine.Generator;
using Silk.NET.Maths;

using Atom.Engine.Tree;



namespace Atom.Engine.Astro;



public partial class Cell : IDisposable
{
    private readonly Voxel[]?            _voxels   ; // The actual voxel data of the chunk
    private WeakReference<Node<Cell>>    _node     ;

    private          bool                _generated; // Has the chunk been generated (either from save file or generator)

    public ref Voxel this[u32 x, u32 y, u32 z] => ref _voxels![AMath.To1D(x, y, z, width: UNIT_COUNT, height: UNIT_COUNT)];
    
    public ref Voxel this[u32 i] => ref _voxels![i];



    public Voxel[]? Voxels => _voxels;
    
    
    
    public Cell(in Node<Cell> node)
    {
        //_depth = node.Depth;
        _voxels = new Voxel[UNIT_COUNT];

        _node = new WeakReference<Node<Cell>>(node);

        /*_scale = SCALES[_depth];
        _position = default;
        for (u32 i = 1; i < _depth + 1; --i)
        {
            _position += SCALES[i] * POSITIONS[node.BranchIndexAtDepth(i)];
        }*/
    }
    
    public void Generate(IGenerator generator)
    {
        _node.TryGetTarget(out Node<Cell> node);
        generator.Generate(node.Location, node.Depth, _voxels);

        _generated = true;
    }

    /*public static Vector3D<u64> GetCellCoordinates(u128 location)
    {
        
    }*/

    public void Delete() { }

    public void Dispose() => Delete();

    ~Cell() => Dispose();

}