using Atom.Engine;
using Atom.Engine.Astro;
using Atom.Engine.Loaders;
using Atom.Engine.Vulkan;
using Atom.Game.Config;
using Silk.NET.Maths;
using Image = Atom.Engine.Image;

namespace Atom.Game;

public class SpaceScene : AtomObject, IScene
{
    private readonly ClassicPlayerController _controller;

    private List<CelestialSystem> _systems;
    
    public SpaceScene()
    {
        _controller = new ClassicPlayerController();

        _controller.Location = new Location(new Vector3D<double>(0.0D, 0.0D, 13599840256));
        
        Dictionary<string, PlanetConfig> planet_configs = Directory
            .GetFiles("assets/Space/", "*.planet", SearchOption.AllDirectories)
            .Select(ConfigFile.LoadInto<PlanetConfig>)
            .ToDictionary(planet => planet.ID);

        _systems = CelestialSystem.CreateSystems(Directory
            .GetFiles("assets/Space/", "*.system", SearchOption.AllDirectories)
            .Select(ConfigFile.LoadInto<SystemConfig>),
            planet_configs
        ).ToList();
        
        Log.Warning("System loaded.");
        
        /*_systems = new List<CelestialSystem>();
        CelestialSystem system = new (ConfigFile.LoadInto<SystemConfig>("Assets/Space/Systems/Kerbol/Kerbol.system"));
        new VoxelBody(ConfigFile.LoadInto<PlanetConfig>("Assets/Space/Systems/Kerbol/MinmusTest.planet"), system);
        _systems.Add(system);*/
        
        MakeReady();
    }
    
    protected internal override void Frame() { }

    protected internal override void Render()
    {
        base.Render();

        if (!Graphics.IsRenderReady) return;

        //ViewportWindow.Instance.Viewport.Present(_displayResource);
    }

    protected internal override void PhysicsFrame() { /* todo */ }

    public override void Delete()
    {
        base.Delete();
        
        foreach (CelestialSystem system in _systems)
        {
            system.Delete();
        }
        
        
        _controller      .Dispose(      );
    }
}