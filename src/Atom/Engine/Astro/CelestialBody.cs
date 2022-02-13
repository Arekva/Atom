using Atom.Engine.Astro.Transvoxel;

namespace Atom.Engine.Astro;

public class CelestialBody : AtomObject, ICelestialBody
{
    public bool IsStatic { get; }

    public Orbit? Orbit { get; }
    
    
    public Space EquatorialSpace { get; }
    
    public Space RotatedSpace { get; }
    
    
    // kg
    public double Mass { get; }
    
    // meter
    public double Radius { get; }

    public double Diameter => Radius * 2.0D;

    // m^3
    public double Volume => (Radius * Radius * Radius) * (4.0D/3.0D) * Math.PI;

    // kg / m^3
    public double Density => Mass / Volume;
    
    // m/s²
    public double SurfaceGravity => (Astrophysics.G * Mass) / Radius * Radius;
    
    public double SurfaceG => SurfaceGravity / Astrophysics.EarthSurfaceGravity;
    


    public Grid Grid { get; }
    
    
    
    public CelestialBody(
        string name, 
        double radius, double mass
        /*ICelestialBody parent, Orbit? orbit = null*/)
    {
        Name = name;
        
        Radius = radius;
        Mass = mass;

        Grid = new Grid(5U);
        Grid.Init();
    }

    public void DebugTerrainAll()
    {
        
    }

    public override void Delete()
    {
        base.Delete();
    }
}