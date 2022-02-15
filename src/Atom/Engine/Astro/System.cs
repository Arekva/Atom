namespace Atom.Engine.Astro;

public class System : Thing, ICelestialBody
{
    public bool IsStatic => true;

    public Orbit? Orbit => null;
    
    
    
    public Space RotatedSpace => EquatorialSpace;
    
    public Space EquatorialSpace { get; }


    
    public double Mass { get; }
}