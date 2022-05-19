namespace Atom.Engine.Astro;

public struct Voxel
{
    public u16 Element    ; // Index 
    public f16 Quantity   ;
    // public f32 Temperature; // Kelvin
    // public f32 Pressure   ; // Pascal
}

public class VoxelChunk
{
    public const u32 WIDTH  = 32;
    public const u32 HEIGHT = 32;
    public const u32 DEPTH  = 32;

    public const u32 VOLUME = WIDTH * HEIGHT * DEPTH;


    
    private Voxel[] _voxels;



    public ref Voxel this[u32 x, u32 y, u32 z] => ref _voxels[AMath.To1D(x, y, z, WIDTH, HEIGHT)];
    

    
    public VoxelChunk() => _voxels = new Voxel[VOLUME];
    
    
    
    
}