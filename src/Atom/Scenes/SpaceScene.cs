using Atom.Engine;
using Atom.Engine.Astro;
using Atom.Engine.Vulkan;
using Atom.Game.Config;
using Silk.NET.Input;
using Silk.NET.Maths;

namespace Atom.Game;

class SystemStructure
{
    private string SystemName { get; }
    
    
}

class BodyInheritence
{
    public string ID { get; set; }
    
    public List<BodyInheritence> Inheritence { get; set; }
}


public class SpaceScene : AtomObject, IScene, IDrawer
{
    private readonly ClassicSkySphere _sky;

    private readonly ClassicPlayerController _controller;

    private List<CelestialSystem> _systems;

    public SpaceScene()
    {
        _controller = new ClassicPlayerController();

        
        Dictionary<string, PlanetConfig> planet_configs = Directory
            .GetFiles("assets/Space/", "*.planet", SearchOption.AllDirectories)
            .Select(ConfigFile.LoadInto<PlanetConfig>)
            .ToDictionary(planet => planet.ID);

        _systems = CelestialSystem.CreateSystems(Directory
            .GetFiles("assets/Space/", "*.system", SearchOption.AllDirectories)
            .Select(ConfigFile.LoadInto<SystemConfig>),
            planet_configs
        ).ToList();
        

        Draw.AssignDrawer(this, 0);
    }
    
    protected override void Frame()
    {
        
    }

    protected override void Render()
    {
        base.Render();
        
        Log.Info(Astrophysics.UniversalTime);
        
        if (Keyboard.IsPressed(Key.K))
        {
            VoxelBody minmus = (_systems[0].Satellites
                .First().Satellites
                .First(s => s.Name == "Kerbin") as VoxelBody)!;
            
            Location loc = minmus.CelestialSpace.Location;

            _controller.Location = loc + (Vector3D<f64>.UnitY + Vector3D<f64>.UnitZ) * minmus.Radius * 4.0D;
        }
    }

    protected override void PhysicsFrame() { /* todo */ }

    public override void Delete()
    {
        base.Delete();

        Draw.UnassignDrawer(this, 0);

        foreach (CelestialSystem system in _systems)
        {
            system.Delete();
        }
        
        
        _controller      .Dispose(      );
    }

    public void CmdDraw(SlimCommandBuffer cmd, Vector2D<UInt32> extent, UInt32 cameraIndex, UInt32 frameIndex)
    {
        foreach (CelestialSystem system in _systems)
        {
            foreach (ICelestialBody body in system.Satellites)
            {
                body.CmdDraw(cmd, extent, cameraIndex, frameIndex);
            }
        }
    }
}