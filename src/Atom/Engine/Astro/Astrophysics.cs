namespace Atom.Engine;

public class Astrophysics
{
    public const f64 G = 6.6742867E-11D;
    
    
    public const f64 EARTH_RADIUS          = 6371008.8D  * Units.METRE;
    public const f64 EARTH_MASS            = 5.9722E+24D * Units.KILOGRAM;
    public const f64 EARTH_SURFACE_GRAVITY = (G * EARTH_MASS) / ((EARTH_RADIUS * EARTH_RADIUS) * Units.KILOMETRE);
}