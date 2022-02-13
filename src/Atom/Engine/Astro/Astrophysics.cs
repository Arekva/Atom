namespace Atom.Engine;

public class Astrophysics
{
    public const double G = 6.6742867E-11D;
    
    
    public const double EarthRadius      = 6371008.8D * Units.Metre;
    public const double EarthMass = 5.9722E+24D * Units.Kilogram;
    public const double EarthSurfaceGravity = (G * EarthMass) / (EarthRadius * EarthRadius);
}