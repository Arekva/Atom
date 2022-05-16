using System.Runtime.CompilerServices;
using Silk.NET.Maths;

namespace Atom.Engine.Tree;

[Flags] public enum Faces : byte
{
    //               +- +- +-
    //               ZZ YY XX
    None        = 0b_00_00_00,
    Left        = 0b_00_00_01,
    Right       = 0b_00_00_10,
    Down        = 0b_00_01_00,
    Up          = 0b_00_10_00,
    Backward    = 0b_01_00_00,
    Forward     = 0b_10_00_00,
    All         = Left|Right|Down|Up|Backward|Forward,
}


public static class FacesExtension
{
    public static Vector3D<i32> GetNormal(this Faces faces)
    {
        Vector3D<i32> result = Vector3D<i32>.Zero;

        if (faces.HasFlag(Faces.Left    )) result += -Vector3D<i32>.UnitX;
        if (faces.HasFlag(Faces.Right   )) result +=  Vector3D<i32>.UnitX;
        if (faces.HasFlag(Faces.Down    )) result += -Vector3D<i32>.UnitY;
        if (faces.HasFlag(Faces.Up      )) result +=  Vector3D<i32>.UnitY;
        if (faces.HasFlag(Faces.Backward)) result += -Vector3D<i32>.UnitZ;
        if (faces.HasFlag(Faces.Forward )) result +=  Vector3D<i32>.UnitZ;
        
        return result;
    }
    
    public static Faces GetPositionToFacesNormalized(Vector3D<f64> pos)
    {
        Faces flags = Faces.None;

        flags |= pos.X < 0.0 ? Faces.Left     : Faces.Right;
        flags |= pos.Y < 0.0 ? Faces.Down     : Faces.Up;
        flags |= pos.Z < 0.0 ? Faces.Backward : Faces.Forward;

        return flags;
    }


    private static readonly Dictionary<Faces, char> _faceIndexMap = new()
    {
        { Faces.All                                , 'X' }, // All  [Invalid]
        { Faces.None                               , 'X' }, // None [Invalid]
        { Faces.Left  | Faces.Up   | Faces.Backward, 'C' }, // left  up   backward (C)
        { Faces.Left  | Faces.Up   | Faces.Forward , 'G' }, // left  up   forward  (G)
        { Faces.Left  | Faces.Down | Faces.Backward, 'A' }, // left  down backward (A)
        { Faces.Left  | Faces.Down | Faces.Forward , 'E' }, // left  down forward  (E)
        { Faces.Right | Faces.Up   | Faces.Backward, 'D' }, // right up   backward (D)
        { Faces.Right | Faces.Up   | Faces.Forward , 'H' }, // right up   forward  (H)
        { Faces.Right | Faces.Down | Faces.Backward, 'B' }, // right down backward (B)
        { Faces.Right | Faces.Down | Faces.Forward , 'F' }, // right down forward  (F)
    };
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static char ToIndex(this Faces faces) => _faceIndexMap[faces];

    public static Vector3D<f64> GetDirectionCubical(this Faces faces)
    {
        if (faces is Faces.None or Faces.All)
        {
            return default;
        }
        
        Vector3D<f64> dir = Vector3D<f64>.Zero;

        dir.X = faces.HasFlag(Faces.Left    ) ? -1.0D : 1.0D;
        dir.Y = faces.HasFlag(Faces.Down    ) ? -1.0D : 1.0D;
        dir.Z = faces.HasFlag(Faces.Backward) ? -1.0D : 1.0D;

        return dir;
    }

    public static Vector3D<f64> GetDirectionSpherical(this Faces faces) => Vector3D.Normalize(GetDirectionCubical(faces));

    public static IEnumerable<Faces> GetFlags(this Faces input) => Faces.GetValues(input.GetType()).Cast<Faces>().Where(value => input.HasFlag(value));
}