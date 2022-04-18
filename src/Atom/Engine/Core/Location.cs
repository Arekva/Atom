using Silk.NET.Maths;

namespace Atom.Engine;

public struct Location : IFormattable, IEquatable<Location>, IComparable<Location>
{
    // we need to keep everything under reasonable distance so the precisions
    // stays around the millimetre
    public const f64 SectorSize       = 60.0D * Units.ASTRONOMICAL_UNIT;
    public const f64 SectorWidth      = SectorSize                     ;
    public const f64 SectorHeight     = SectorSize                     ;
    public const f64 SectorDepth      = SectorSize                     ;
    public const f64 SectorWidthHalf  = SectorWidth / 2.0D             ;
    public const f64 SectorHeightHalf = SectorHeight/ 2.0D             ;
    public const f64 SectorDepthHalf  = SectorDepth / 2.0D             ;
    
    public static readonly Vector3D<f64> SectorScale  = new (SectorWidth, SectorHeight, SectorDepth);
    public static readonly Vector3D<f64> SectorCentre = SectorScale / 2.0D                               ;
    public static readonly Vector3D<f64> SectorHalf   = SectorScale / 2.0D                               ;

    public static readonly Location Origin = new(coordinates: new Vector3D<Double>(0.0, 0.0, 0.0));
    
    
    public Vector3D<f64> Coordinates;
    public Vector3D<i64> Sector;

    /// <summary> The global universe coordinates. Avoid using this for precise calculations. </summary>
    public Vector3D<f64> Position => new Vector3D<f64>(Sector.X, Sector.Y, Sector.Z) * SectorScale
                                                 + Coordinates;

    public Location(Vector3D<double> coordinates, Vector3D<long> sector)
    {
        Coordinates = coordinates;
        Sector = sector;
        ManageOverflow();
    }

    public Location(Vector3D<double> coordinates)
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

    private void ManageOverflow()
    {
        ref Vector3D<f64> c = ref Coordinates;
        ref Vector3D<i64> s = ref Sector;

        f64 c_overflow_x = c.X % SectorWidthHalf;
        f64 c_overflow_y = c.Y % SectorHeightHalf;
        f64 c_overflow_z = c.Z % SectorDepthHalf;
        
        i64 s_overflow_x = (i64)(c.X / SectorWidthHalf);
        i64 s_overflow_y = (i64)(c.Y / SectorHeightHalf);
        i64 s_overflow_z = (i64)(c.Z / SectorDepthHalf);

        if (s_overflow_x != 0) c.X = -Math.Sign(s_overflow_x) * SectorWidthHalf + c_overflow_x;
        if (s_overflow_y != 0) c.Y = -Math.Sign(s_overflow_y) * SectorHeightHalf + c_overflow_y;
        if (s_overflow_z != 0) c.Z = -Math.Sign(s_overflow_z) * SectorDepthHalf + c_overflow_z;

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
        Vector3D<long> sector = a.Sector / b.Sector;

        return new Location(coords, sector);
    }
    
    public static bool operator ==(Location a, Location b) => a.Coordinates == b.Coordinates && a.Sector == b.Sector;
    public static bool operator !=(Location a, Location b) => a.Coordinates != b.Coordinates || a.Sector != b.Sector;
}