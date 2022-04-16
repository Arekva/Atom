using System.Diagnostics;
using System.Runtime.CompilerServices;
using Atom.Engine;
using Atom.Engine.Astro;
using Atom.Engine.Loaders;
using Atom.Engine.Shader;
using Atom.Engine.Vulkan;
using Atom.Game.Config;
using Silk.NET.Maths;

namespace Atom.Game;

class SystemStructure
{
    private string SystemName { get; }
    
    
}

class Subsystem
{
    
}


public class SpaceScene : AtomObject, IScene, IDrawer
{
    private readonly ClassicSkySphere _sky;

    private readonly ClassicPlayerController _controller;

    public SpaceScene()
    {
        _controller = new ClassicPlayerController();
        
        SystemConfig[] system_configs = Directory
            .GetFiles("assets/Space/", "*.system", SearchOption.AllDirectories)
            .Select(ConfigFile.LoadInto<SystemConfig>)
            .ToArray();

        PlanetConfig[] planet_configs = Directory
            .GetFiles("assets/Space/", "*.planet", SearchOption.AllDirectories)
            .Select(ConfigFile.LoadInto<PlanetConfig>)
            .ToArray();
        
        
        
        // ksp system
        
        /* root */ 
        using CelestialSystem kerbol_system = new( 
            location: Location.Origin,
            name    : "Kerbol"
        );
        /* main star */ 
        Star kerbol  = new(name: "Kerbol"    ,
            radius   : 261600000.0D          ,
            mass     : 1.75654591319326E+28D ,
            reference: kerbol_system
        );
        Planet moho  = new(name: "Moho"      ,
            radius   : 250000.0D             ,
            mass     : 2.52633139930162E+21D ,
            reference: kerbol
        );
        Planet eve   = new(name: "Eve"       ,
            radius   : 700000.0D             ,
            mass     : 1.2243980038014E+23D  ,
            reference: kerbol
        );
        Planet gilly = new(name: "Gilly"     ,
            radius   : 13000.0D              ,
            mass     : 1.24203632781093E+17D ,
            reference: eve
        );

        Log.Put(kerbol_system.View());


        Draw.AssignDrawer(this, 0);
    }
    
    protected override void Frame()
    {
        
    }

    protected override void PhysicsFrame() { /* todo */ }

    public override void Delete()
    {
        base.Delete();

        Draw.UnassignDrawer(this, 0);

        _controller      .Dispose(      );
    }

    public void CmdDraw(SlimCommandBuffer cmd, Vector2D<UInt32> extent, UInt32 cameraIndex, UInt32 frameIndex)
    {
    }
}