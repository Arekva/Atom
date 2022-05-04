using Atom.Engine;
using Atom.Engine.Loaders;
using Atom.Engine.Vulkan;
using Silk.NET.Maths;
using Image = Atom.Engine.Image;

namespace Atom.Game;

public class SpaceScene : AtomObject, IScene, IDrawer
{
    private readonly ClassicPlayerController _controller;

    //private List<CelestialSystem> _systems;

    private Viewport _viewport;

    private ImageSubresource _displayResource;

    public SpaceScene()
    {
        _controller = new ClassicPlayerController();


        u32 queues_fam = 0;
        Image image = DDS.Load(
            stream: File.Open("assets/Images/Planets/Colors/Kerbin.dds", FileMode.Open),
            queueFamilies: queues_fam.AsSpan(),
            layout: vk.ImageLayout.TransferSrcOptimal, 
            stage: PipelineStageFlags.Transfer,
            accessMask: vk.AccessFlags.AccessTransferReadBit,
            usages: ImageUsageFlags.TransferSource | ImageUsageFlags.Sampled);

        _displayResource = image.CreateSubresource();

        /*Dictionary<string, PlanetConfig> planet_configs = Directory
            .GetFiles("assets/Space/", "*.planet", SearchOption.AllDirectories)
            .Select(ConfigFile.LoadInto<PlanetConfig>)
            .ToDictionary(planet => planet.ID);

        _systems = CelestialSystem.CreateSystems(Directory
            .GetFiles("assets/Space/", "*.system", SearchOption.AllDirectories)
            .Select(ConfigFile.LoadInto<SystemConfig>),
            planet_configs
        ).ToList();*/

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
        
        _displayResource.Delete();
        _displayResource.Image.Delete();
        
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