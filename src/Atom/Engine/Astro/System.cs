namespace Atom.Engine.Astro;

public class System : Thing, ICelestialBody
{
    public bool IsStatic { get; } = true;

    public Orbit? Orbit { get; } = null;
    
    
    
    public Space RotatedSpace => EquatorialSpace;
    
    public Space EquatorialSpace { get; }


    
    public double Mass { get; }
}