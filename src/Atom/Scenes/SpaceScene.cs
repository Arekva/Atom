using Atom.Engine;
using Atom.Engine.Astro;
using Atom.Engine.Vulkan;
using Atom.Game.Config;
using Silk.NET.Input;
using Silk.NET.Maths;

namespace Atom.Game;

public class SpaceScene : AtomObject, IScene, IDrawer
{
    private readonly ClassicPlayerController _controller;

    //private List<CelestialSystem> _systems;

    public SpaceScene()
    {
        _controller = new ClassicPlayerController();

        
        /*Dictionary<string, PlanetConfig> planet_configs = Directory
            .GetFiles("assets/Space/", "*.planet", SearchOption.AllDirectories)
            .Select(ConfigFile.LoadInto<PlanetConfig>)
            .ToDictionary(planet => planet.ID);

        _systems = CelestialSystem.CreateSystems(Directory
            .GetFiles("assets/Space/", "*.system", SearchOption.AllDirectories)
            .Select(ConfigFile.LoadInto<SystemConfig>),
            planet_configs
        ).ToList();*/
        

        
        //Draw.AssignDrawer(this, 0);
    }
    
    protected internal override void Frame()
    {
        
    }

    protected internal override void Render()
    {
        base.Render();
        
        /*if (Keyboard.IsPressed(Key.K))
        {
            VoxelBody minmus = (_systems[1].Satellites
                .First().Satellites
                .First(s => s.Name == "Test Planet") as VoxelBody)!;
            
            Location loc = minmus.CelestialSpace.Location;

            _controller.Location = loc + Vector3D<f64>.UnitY * minmus.Radius;

        }*/
    }

    protected internal override void PhysicsFrame() { /* todo */ }

    public override void Delete()
    {
        base.Delete();

        //Draw.UnassignDrawer(this, 0);

        /*foreach (CelestialSystem system in _systems)
        {
            system.Delete();
        }*/
        
        
        _controller      .Dispose(      );
    }

    public void CmdDraw(SlimCommandBuffer cmd, Vector2D<UInt32> extent, UInt32 cameraIndex, UInt32 frameIndex)
    {
        /*foreach (CelestialSystem system in _systems)
        {
            foreach (ICelestialBody body in system.Satellites)
            {
                body.CmdDraw(cmd, extent, cameraIndex, frameIndex);
            }
        }*/
    }
}