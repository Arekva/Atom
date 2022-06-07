using Atom.Engine;
using Atom.Engine.Astro;
using Atom.Engine.Loaders;
using Atom.Engine.Vulkan;
using Atom.Game.Config;
using Atom.Game.VoxelTest;
using Silk.NET.Maths;
using Image = Atom.Engine.Image;

namespace Atom.Game;

public class VoxelTestScene : AtomObject, IScene
{
    private Camera _camera;

    private SkySphere _sky;

    private CameraRot _rotation;

    public VoxelTestScene()
    {
        _camera = Camera.World = new Camera(identifier: "default_world_viewport");
        _camera.Perspective.Near = 0.001D;

        _sky = new SkySphere(color: new Vector4D<f64>(135/255.0D, 206/255.0D, 235/255.0D, 20000.0D));

        _rotation = new CameraRot();
        
        MakeReady();
    }

    protected internal override void Render()
    {
        base.Render();

        
    }

    protected internal override void PhysicsFrame() { /* todo */ }

    public override void Delete()
    {
        base.Delete();
        
        _rotation.Delete();
        
        _camera.Delete();
        _sky.Delete();
    }
}