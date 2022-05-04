using Atom.Engine.Vulkan;
using Atom.Game.Config;
using Silk.NET.Maths;

namespace Atom.Engine.Astro;

public class CelestialSystem : Thing, ICelestialBody
{
    public string ID { get; }
    public string? Description { get; }
    
    public bool            IsStatic  => true;

    public ITrajectory?    Orbit     => null;

    public ICelestialBody? Reference => null;

    private List<ICelestialBody> _satellites;
    public IEnumerable<ICelestialBody> Satellites => _satellites;
    
    public Space RotatedSpace => CelestialSpace; // a system by itself doesn't rotate.
    
    public Space CelestialSpace { get; }

    public f64 Mass => Satellites.Sum(s => s.Mass);

    public CelestialSystem(Location location, string id, string? name = "Star System", string? description = null) : base(location, name)
    {
        ID = id;
        Description = description;
        _satellites = new List<ICelestialBody>(capacity: 7); // no more than 7 stars per system... i hope?
        CelestialSpace = new Space(this, name + " space");

        MakeReady();
    }

    public CelestialSystem(SystemConfig config) : base(config.Location, config.Name)
    {
        ID = config.ID;
        Description = config.Description;
        
        _satellites = new List<ICelestialBody>(capacity: 7);
        CelestialSpace = new Space(this, config.Name + " space");
        
        MakeReady();
    }

    public CelestialSystem(SystemConfig config, Dictionary<string, PlanetConfig> bodies) : this(config)
    {
        // todo: load an entire system
        
        MakeReady();
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

    public void CmdDraw(SlimCommandBuffer cmd, Vector2D<UInt32> extent, UInt32 cameraIndex, UInt32 frameIndex)
    {
        // a system isn't drawn by itself.
    }

    public static IEnumerable<CelestialSystem> CreateSystems(IEnumerable<SystemConfig> systemsConfigs, Dictionary<string, PlanetConfig> planetConfigs)
    {
        Dictionary<string, ICelestialBody> planets = new(capacity: 2048);

        return systemsConfigs.Select(sys =>
        {
            CelestialSystem system = new (sys);
            foreach ((string id, PlanetConfig planet_cfg) in planetConfigs)
            {
                system.CreateBody(planet_cfg, planetConfigs, planets, out _);
            }
            return system;
        });
    }

    private bool CreateBody(PlanetConfig cfg, Dictionary<string, PlanetConfig> configs, Dictionary<string, ICelestialBody> currentBodies, out ICelestialBody? celestialBody)
    {
        string system_id = ID;
        string parent_id = cfg.Orbit.Parent;
        string planet_id = cfg.ID;


        ICelestialBody create_body(ICelestialBody parent)
        {
            ICelestialBody cb = new VoxelBody(cfg, parent);
            
            currentBodies.Add(cfg.ID, cb);

            return cb;
        }
        
        

        if (currentBodies.ContainsKey(planet_id))
        {   // this body already has been created
            celestialBody = currentBodies[planet_id];
            return true;
        }
        if (system_id == parent_id)
        {
            // this celestial body is owned by this system and is children of root, add.
            celestialBody = create_body(this);
            return true;
        }
        if (!configs.ContainsKey(parent_id))
        {
            // this body has no existing parent or parent is a system, skip.
            celestialBody = null;
            return false;
        }


        if (currentBodies.ContainsKey(parent_id))
        {   // simply spawn body and refers it to its parent

            ICelestialBody parent = currentBodies[parent_id];
            
            celestialBody = create_body(parent);

            return true;
        }
        else 
        {   // the parent isn't present in the list, and therefore hasn't been created or doesn't
            // exit in the system. recursively try to create the parents, if only one parent in the list couldn't be 
            // created then this planet does not belong to this system.
            if (CreateBody(configs[parent_id], configs, currentBodies, out ICelestialBody? parent))
            {
                celestialBody = create_body(parent!);
                return true;
            }
            else
            {   // this body does not belong to this system.
                celestialBody = null;
                return false;
            }
        }
    }
}