using System.Runtime.CompilerServices;

using Silk.NET.Maths;

using Atom.Engine.Tree;



namespace Atom.Engine.Astro;



public partial class Chunk : IDisposable
{
    private readonly Voxel[]?            _voxels   ; // The actual voxel data of the chunk
    
    private readonly Vector3D<f64>       _position ; // Local position relative to the planet centre
    private readonly f64                 _scale    ; // Global scale
    private readonly u32                 _depth    ; // Depth of the owner node into the octree structure

    private          bool                _generated; // Has the chunk been generated (either from save file or generator)

    
    
    public ref Voxel this[u32 x, u32 y, u32 z] => ref _voxels![AMath.To1D(x, y, z, width: UNIT_COUNT, height: UNIT_COUNT)];
    
    public ref Voxel this[u32 i] => ref _voxels![i];
    
    
    
    public Chunk(in Node<Chunk> node)
    {
        _depth = node.Depth;
        _voxels = new Voxel[UNIT_COUNT];

        _scale = SCALES[_depth];
        _position = default;
        for (u32 i = 1; i < _depth + 1; --i)
        {
            _position += SCALES[i] * POSITIONS[node.BranchIndexAtDepth(i)];
        }
    }
    
    public void Generate(f64 radius)
    {
        f64 squared_radius = radius * radius;
        
        ref readonly Vector3D<f64> centre = ref _position;
        ref readonly f64 scale = ref _scale;

        Vector3D<f64> world_min = centre - Vector3D<f64>.One * scale;
        Vector3D<f64> world_max = centre + Vector3D<f64>.One * scale;
        
        const f64 VOXEL_MIN = 0.0D;
        const f64 VOXEL_MAX = UNITS;

        Vector3D<f64> voxel_min_pos = new(VOXEL_MIN, VOXEL_MIN, VOXEL_MIN);
        Vector3D<f64> voxel_max_pos = new(VOXEL_MAX, VOXEL_MAX, VOXEL_MAX);

        for (u32 i = 0; i < UNIT_COUNT; i++)
        {
            AMath.To3D(i, (u64)SIZE, (u64)SIZE, out Vector3D<u64> voxel_pos);

            Vector3D<f64> world_pos = AMath.Map((Vector3D<f64>)voxel_pos, voxel_min_pos, voxel_max_pos, world_min, world_max);

            f64 squared_distance_from_centre = Vector3D.Dot(world_pos, world_pos);

            if (squared_distance_from_centre <= squared_radius) // stupid sphere with radius of planet
            {
                _voxels![i].Element = 1;
                _voxels![i].Mass = 1.0F;
            }
            /*else
            {
                _voxels![i].Element = 0;
            }*/
        }
        
        _generated = true;
    }

    public void Delete() { }

    public void Dispose() => Delete();

    ~Chunk() => Dispose();

}