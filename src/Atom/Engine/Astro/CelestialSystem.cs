namespace Atom.Engine.Astro;

public class CelestialSystem : Thing, ICelestialBody
{
    public bool IsStatic => true;

    public Orbit? Orbit => null;
    
    
    
    public Space RotatedSpace => EquatorialSpace;
    
    public Space EquatorialSpace { get; }


    
    public double Mass { get; }


    public CelestialSystem(Location location, string? name = "Star System") : base(location, name)
    {
        EquatorialSpace = new Space(this, name + " space");
    }
}