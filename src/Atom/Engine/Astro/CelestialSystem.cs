namespace Atom.Engine.Astro;

public class CelestialSystem : Thing, ICelestialBody
{
    public bool            IsStatic  => true;

    public ITrajectory?    Orbit     => null;

    public ICelestialBody? Reference => null;

    private List<ICelestialBody> _satellites;
    public IEnumerable<ICelestialBody> Satellites => _satellites;
    
    public Space RotatedSpace => CelestialSpace; // a system by itself doesn't rotate.
    
    public Space CelestialSpace { get; }

    public f64 Mass => 0.0D; // todo: total system mass

    public CelestialSystem(Location location, string? name = "Star System") : base(location, name)
    {
        _satellites = new List<ICelestialBody>(capacity: 7); // no more than 7 stars per system... i hope?
        CelestialSpace = new Space(this, name + " space");
    }

    public string View(i32 level = 0)
    {
        string info = $"Celestial System [|#FFC800,{Name}|]\n";

        return Satellites.Aggregate(info, (current, satellite) => current + satellite.View(level: level + 1));
    }
    
    public void AddSatellite(ICelestialBody celestialBody)
    {
        _satellites.Add(celestialBody);
    }

    public void RemoveSatellite(ICelestialBody celestialBody)
    {
        _satellites.Remove(celestialBody);
    }
}