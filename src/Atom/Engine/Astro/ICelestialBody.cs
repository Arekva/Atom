namespace Atom.Engine.Astro;

public interface ICelestialBody : IDisposable
{
    public bool IsStatic { get; }
    
    public ITrajectory? Orbit { get; }
    
    public ICelestialBody? Reference { get; }
    
    public IEnumerable<ICelestialBody> Satellites { get; }


    public Space CelestialSpace { get; }
    
    public Space RotatedSpace { get; }
    
    
    public string? Name { get; }
    
    public string ID { get; }
    
    public string? Description { get; }
    
    
    // Total mass of the CB, in kg
    public f64 Mass { get; }

    public void AddSatellite(ICelestialBody celestialBody);
    public void RemoveSatellite(ICelestialBody celestialBody);

    public string View(i32 level = 0);
}