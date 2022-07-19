using Atom.Engine;
using Atom.Engine.Astro;
using Atom.Engine.Generator;
using Atom.Engine.Loaders;
using Atom.Engine.Tree;
using Atom.Engine.Vulkan;
using Atom.Game.Config;
using Silk.NET.Maths;
using Image = Atom.Engine.Image;

namespace Atom.Game;

public class SpaceScene : AtomObject, IScene
{
    private readonly ClassicPlayerController _controller;

    private CelestialSystem _system;

    private VoxelBody _playerBody;
    
    public SpaceScene()
    {
        _controller = new ClassicPlayerController();

        _controller.Location = new Location(new Vector3D<f64>(0.0D, 0.0D, 0.0D));

        _system = new CelestialSystem(
            location    : Location.Origin, 
            id          : "Atom.Dev"     , 
            name        : "Dev"          , 
            description : "Dev is a system with a surprising amount of very simple or very broken bodies, some has to get to work."
        );

        PlanetConfig smoothie_config = new()
        {
            ID = "Atom.Dev.Smoothie",
            Name = "Smoothie",
            Description = "Smoothie is a planet without any surface asperity",
            SurfaceG = Astrophysics.EARTH_SURFACE_GRAVITY,
            Orbit = new ()
            {
                Parent = "Atom.Dev"
            },
            
            Rotation = new()
            {
                Day = 300000.0D, // 5 mins
                Inclination = new() { }
            },

            Generation = new ()
            {
                GeneratorPath = "HARDCODED",
                GeneratorParameters = new Atom.Game.Config.Generator()
                {
                    Radius = Units.KILOMETRE
                }
            },
        };
        VoxelBody smoothie_body = new(smoothie_config, _system);

        Log.Warning("System loaded.");
        
        _playerBody = smoothie_body;

        Smooth smooth_generator = new () { Radius = Units.KILOMETRE };
        
        Log.Warning("Generating & spawning terrain.");
        //smoothie_body.Grid.SpawnTerrain(smooth_generator);


        /*Dictionary<string, PlanetConfig> planet_configs = Directory
            .GetFiles(path: "assets/Space/", searchPattern: "*.planet", SearchOption.AllDirectories)
            .Select(ConfigFile.LoadInto<PlanetConfig>)
            .ToDictionary(planet => planet.ID);

        _systems = CelestialSystem.CreateSystems(Directory
            .GetFiles("assets/Space/", "*.system", SearchOption.AllDirectories)
            .Select(ConfigFile.LoadInto<SystemConfig>),
            planet_configs
        ).ToList();*/

       /* _playerBody = (VoxelBody)
            _systems.First(s => s.Name == "Kerbol System")
            .Satellites.First(p => p.Name == "Kerbol")
            .Satellites.First(p => p.Name == "Kerbin");*/
        
        MakeReady();
    }

    protected internal override void Render()
    {
        _controller.Teleport(_playerBody);
    }

    protected internal override void PhysicsFrame() { /* todo */ }

    public override void Delete()
    {
        base.Delete();
        
        _system.Delete();

        /*foreach (CelestialSystem system in _systems)
        {
            system.Delete();
        }*/
        
        
        _controller.Dispose();
    }
}