using Silk.NET.Maths;

using Atom.Engine.Tree;



namespace Atom.Engine.Astro;



public partial class Cell
{
    public const u32 UNITS = 32;
    
    public const u32 UNIT_COUNT = UNITS * UNITS * UNITS;

    public const u32 SIZE_POW = 4; // 2^4: 16
    public const f64 SIZE = Units.METRE * (1<<(i32)SIZE_POW);
    
    public const f64 UNIT_SIZE = SIZE / UNITS;

    public const f64 VOLUME = UNIT_SIZE * UNIT_SIZE * UNIT_SIZE;

    public const f64 ISOSURFACE = 0.0D;

    public const u64 MAX_VERTEX_COUNT = 1 << 14;

    public const u64 MAX_INDEX_COUNT  = 1 << 12;
    

    
    // world-sized scales for chunks and n subdivision
    public static readonly f64[] SCALES;
    public static readonly i64[] SCALES_LONG;
    public static readonly f64[] SIZES;
    public static readonly i64[] SIZES_LONG;

    public static readonly Vector3D<f64>[] POSITIONS =
    {
        new (-0.5D, -0.5D, -0.5D), //'A'
        new (-0.5D, -0.5D,  0.5D), //'E'
        new (-0.5D,  0.5D, -0.5D), //'C'
        new (-0.5D,  0.5D,  0.5D), //'G'
        new ( 0.5D, -0.5D, -0.5D), //'B'
        new ( 0.5D, -0.5D,  0.5D), //'F'
        new ( 0.5D,  0.5D, -0.5D), //'D'
        new ( 0.5D,  0.5D,  0.5D), //'H'
    };
    
    public static readonly Quaternion<f64> ROTATION = Quaternion<f64>.Identity; // Local rotation from planet centre.

    
    static Cell()
    {
        // MAX_SUBDIVISIONS + 1 because we also count the root node.
        const u32 SCALE_COUNT = Octree.MAX_SUBDIVISIONS + 1;
        SCALES = new f64[SCALE_COUNT];
        SIZES = new f64[SCALE_COUNT];
        SCALES_LONG = new i64[SCALE_COUNT];
        SIZES_LONG = new i64[SCALE_COUNT];
        for (i32 i = 0; i < SCALE_COUNT; i++)
        {
            // 2^(43-1) => max size
            SIZES_LONG[i] = 1L << (i32)(SCALE_COUNT - i - 1 + SIZE_POW);
            SIZES[i] = SIZES_LONG[i];

            SCALES_LONG[i] = 1L << (i32)(SCALE_COUNT - i - 2 + SIZE_POW);
            SCALES[i] = SCALES_LONG[i];
        }
    }
}