namespace Atom.Engine;

public interface ICelestialBody
{
    public Space EquatorialSpace { get; }
    
    public Space RotatedSpace { get; }

    
    // Total mass of the CB, in kg
    public double Mass { get; }
}