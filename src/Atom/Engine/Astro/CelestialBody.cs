using System.Collections.Concurrent;
using Silk.NET.Maths;
using Atom.Engine.Vulkan;
using Atom.Game.Config;

namespace Atom.Engine.Astro;

public abstract class CelestialBody : AtomObject, ICelestialBody, IDrawer
{
    public static ConcurrentDictionary<string, ICelestialBody> CelestialBodies { get; } = new();


    public string ID { get; }
    public string? Description { get; }

    public bool            IsStatic  { get; }

    public ITrajectory?    Orbit     { get; set; }
    
    public ICelestialBody  Reference { get; }
    

    protected List<ICelestialBody> SatellitesList = new();
    public IEnumerable<ICelestialBody> Satellites => SatellitesList.AsEnumerable();


    public Space CelestialSpace { get; }
    
    public Space RotatedSpace { get; init; }
    


    // kg
    public f64 Mass { get; }

    // meter
    public f64 Radius { get; }

    public f64 Diameter => Radius * 2.0D;

    // m^3
    public f64 Volume => (Radius * Radius * Radius) * (4.0D/3.0D) * Math.PI;

    // kg / m^3
    public f64 Density => Mass / Volume;
    
    // m/s²
    public f64 SurfaceGravity => (Astrophysics.G * Mass) / (Radius * Radius);
    
    public f64 SurfaceG => SurfaceGravity / Astrophysics.EARTH_SURFACE_GRAVITY;

    public CelestialBody(PlanetConfig config, ICelestialBody reference)
    {
        ID = config.ID;
        
        Name = config.Name;

        Radius = config.Generation.GeneratorParameters.Radius;

        Mass = config.Mass / 1000.0D;

        Reference = reference;

        CelestialSpace = new Space(reference.CelestialSpace, $"{config.Name} celestial space");
        
        reference.AddSatellite(this);
        
        //Draw.AssignDrawer(this, cameraIndex: 0);
    }


    public void AddSatellite(ICelestialBody celestialBody)
    {
        SatellitesList.Add(celestialBody);
    }

    public void RemoveSatellite(ICelestialBody celestialBody)
    {
        SatellitesList.Remove(celestialBody);
    }
    
    public string View(i32 level = 0)
    {
        string tabs = level == 0 ? string.Empty : new(' ', level);

        string surface_g = $"{SurfaceG:F1}G";
        string info = $"{tabs} {Name} [{surface_g}]\n";

        return Satellites.Aggregate(info, (current, satellite) => current + satellite.View(level: level + 1));
    }

    public abstract void CmdDraw(SlimCommandBuffer cmd, Vector2D<u32> extent, u32 cameraIndex, u32 frameIndex);

    public override void Delete()
    {
        base.Delete();
        
        //Draw.UnassignDrawer(this, cameraIndex: 0);

        //VK.API.DeviceWaitIdle(VK.Device); // shit but lazy way to sync, for now.
    }
}