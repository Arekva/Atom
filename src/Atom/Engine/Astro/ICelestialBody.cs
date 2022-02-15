namespace Atom.Engine.Astro;

public interface ICelestialBody
{
    public bool IsStatic { get; }
    
    public Orbit? Orbit { get; }
    
    
    
    public Space EquatorialSpace { get; }
    
    public Space RotatedSpace { get; }
    
    
    
    public string? Name { get; }
    
    
    // Total mass of the CB, in kg
    public double Mass { get; }
}