using System.Numerics;
using System.Runtime.Intrinsics;
using Silk.NET.Maths;

namespace Atom.Engine;

public struct Location : IFormattable, IEquatable<Location>, IComparable<Location>
{
    // we need to keep everything under reasonable distance so the precisions
    // stays around the millimetre
    public const f64 SECTOR_SIZE        = (1UL<<39) * Units.METRE;
    public const f64 SECTOR_WIDTH       = SECTOR_SIZE            ;
    public const f64 SECTOR_HEIGHT      = SECTOR_SIZE            ;
    public const f64 SECTOR_DEPTH       = SECTOR_SIZE            ;
    public const f64 SECTOR_WIDTH_HALF  = SECTOR_WIDTH / 2.0D    ;
    public const f64 SECTOR_HEIGHT_HALF = SECTOR_HEIGHT/ 2.0D    ;
    public const f64 SECTOR_DEPTH_HALF  = SECTOR_DEPTH / 2.0D    ;
    
    public static readonly Vector3D<f64> SectorScale  = new (SECTOR_WIDTH, SECTOR_HEIGHT, SECTOR_DEPTH);
    public static readonly Vector3D<f64> SectorCentre = SectorScale / 2.0D                                  ;
    public static readonly Vector3D<f64> SectorHalf   = SectorScale / 2.0D                                  ;

    public static readonly Location Origin = new(coordinates: new Vector3D<f64>(0.0, 0.0, 0.0));
    
    
    public Vector3D<f64> Coordinates;
    public Vector3D<i64> Sector;

    /// <summary> The global universe coordinates. Avoid using this for precise calculations. </summary>
    public Vector3D<f64> Position => new Vector3D<f64>(Sector.X, Sector.Y, Sector.Z) * SectorScale + Coordinates;

    public Location(Vector3D<f64> coordinates, Vector3D<long> sector)
    {
        Coordinates = coordinates;
        Sector = sector;
        ManageOverflow();
    }

    public Location(Vector3D<f64> coordinates)
    {
        Coordinates = coordinates;
        Sector = default;
        ManageOverflow();
    }

    public void SetCoordinatesSafe(Vector3D<f64> coordinates)
    {
        Coordinates = coordinates;
        ManageOverflow();
    }

    public void Scale(f64 scale)
    {
        Sector *= (i64)scale;
        Coordinates *= scale;
        ManageOverflow();
    }

    public void Rotate(Quaternion<f64> rotation)
    {
        Vector3D<f64> position = Position;
        
        f64 length = position.Length;
        Vector3D<f64> dir = position / length;

        Vector3D<f64> new_dir = rotation.Multiply(dir);
        Vector3D<f64> new_position = new_dir * length;

        this = new Location(new_position);
    }

    private void ManageOverflow()
    {
        ref Vector3D<f64> c = ref Coordinates;
        ref Vector3D<i64> s = ref Sector;
        

        i64 s_overflow_x = (i64)(c.X / SECTOR_WIDTH);
        i64 s_overflow_y = (i64)(c.Y / SECTOR_HEIGHT);
        i64 s_overflow_z = (i64)(c.Z / SECTOR_DEPTH);

        c.X -= s_overflow_x * SECTOR_WIDTH ;
        c.Y -= s_overflow_y * SECTOR_HEIGHT;
        c.Z -= s_overflow_z * SECTOR_DEPTH ;
        
        s.X += s_overflow_x;
        s.Y += s_overflow_y;
        s.Z += s_overflow_z;
    }

    public override string ToString() => ToString(null, null);

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        ref readonly Vector3D<f64> c = ref Coordinates;
        ref readonly Vector3D<i64> s = ref Sector;
        return $"[{s.X.ToString(format, formatProvider)}; {s.Y.ToString(format, formatProvider)}; {s.Z.ToString(format, formatProvider)}]" +
               $" <{c.X.ToString(format, formatProvider)}; {c.Y.ToString(format, formatProvider)}; {c.Z.ToString(format, formatProvider)}>";
    }

    public bool Equals(Location other) => other.Sector == Sector && other.Coordinates == Coordinates;

    public int CompareTo(Location other)
    {
        double other_length = other.Position.LengthSquared;
        double this_length = Position.LengthSquared;
        return other_length > this_length ? -1 : other_length < this_length ? 1 : 0;
    }

    public static Location operator +(Location a, Location b)
    {
        Vector3D<double> coords = a.Coordinates + b.Coordinates;
        Vector3D<long> sector = a.Sector + b.Sector;

        return new Location(coords, sector);
    }
    
    public static Location operator +(Location a, Vector3D<double> b)
    {
        Vector3D<double> coords = a.Coordinates + b;
        Vector3D<long> sector = a.Sector;

        return new Location(coords, sector);
    }
    
    public static Location operator -(Location a, Location b)
    {
        Vector3D<double> coords = a.Coordinates - b.Coordinates;
        Vector3D<long> sector = a.Sector - b.Sector;

        return new Location(coords, sector);
    }
    
    public static Location operator *(Location a, Location b)
    {
        // todo: check if this is mathematically true
        Vector3D<double> coords = a.Coordinates * b.Coordinates;
        Vector3D<long> sector = a.Sector * b.Sector;

        return new Location(coords, sector);
    }
    public static Location operator /(Location a, Location b)
    {
        // todo: check if this is mathematically true
        Vector3D<double> coords = a.Coordinates / b.Coordinates;
        Vector3D<long> sector = default;
        if (b.Sector.X == 0.0D || b.Sector.Y == 0.0D || b.Sector.Z == 0.0D)
        {
            sector = a.Sector;
        }
        else sector = a.Sector / b.Sector;

        return new Location(coords, sector);
    }
    
    public static bool operator ==(Location a, Location b) => a.Coordinates == b.Coordinates && a.Sector == b.Sector;
    public static bool operator !=(Location a, Location b) => a.Coordinates != b.Coordinates || a.Sector != b.Sector;
}