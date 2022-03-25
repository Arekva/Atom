using System.Diagnostics;
using System.Runtime.CompilerServices;
using Atom.Engine;
using Atom.Engine.Loaders;
using Atom.Engine.Shader;
using Atom.Engine.Vulkan;
using Silk.NET.Maths;

namespace Atom.Game;

public class ClassicScene : AtomObject, IScene, IDrawer
{
    private readonly ClassicSkySphere _sky;

    private readonly ClassicPlayerController _controller;

    private readonly SlimBuffer _transformsBuffer;
    private readonly VulkanMemory _transformsMemory;

    private readonly ReadOnlyMesh<u32> _ground;

    private readonly RasterizedMaterial _groundMaterial;

    private readonly Texture _groundTexture;

    private vk.Device Device => _groundMaterial.Device;

    public ClassicScene()
    {
        _sky = new ClassicSkySphere();

        _controller = new ClassicPlayerController();

        _ground = ReadOnlyMesh.Load<u32>("Assets/Meshes/Cube.obj");

        IRasterShader shader = Shader.Load<IRasterShader>("Engine", "Standard");

        _groundMaterial = new RasterizedMaterial(shader);

        Span<Matrix4X4<f32>> transforms = stackalloc Matrix4X4<f32>[3];
        for (i32 i = 0; i < Graphics.MaxFramesCount; i++)
        {
            transforms[i] = Matrix4X4<f32>.Identity;
        }
        (_transformsBuffer, _transformsMemory) = transforms.CreateVulkanMemory(
            device: _groundMaterial.Device,  
            properties: MemoryPropertyFlags.HostVisible  |
                        MemoryPropertyFlags.HostCoherent |
                        MemoryPropertyFlags.DeviceLocal  );
        
        _groundMaterial.WriteBuffer<IVertexModule>(
            name: "_instanceData",
            subresource: new BufferSubresource(
                buffer : _transformsBuffer,
                segment: _transformsMemory.Segment(0, (u64)Unsafe.SizeOf<Matrix4X4<f32>>() * Graphics.MaxFramesCount)
            )
        );
        _groundMaterial.WriteBuffer<IVertexModule>(
            name: "_cameraMatrices",
            subresource: new BufferSubresource(
                buffer : CameraData.VPMatrices,
                segment: CameraData.Memory.Whole
            )
        );
        
        _groundTexture = new Texture(DDS.Load(File.OpenRead("assets/Images/photo.dds")));
        
        _groundMaterial.WriteImage<IFragmentModule>(
            name   : "_albedo"     ,
            texture: _groundTexture
        );

        Draw.AssignDrawer(this, 0);
    }

    private f64 _rotation = 0.0D;

    protected override void Frame()
    {
        if (_transformsMemory == null!) return;
        
        _rotation += Time.DeltaTime * 45.0D;
        using (MemoryMap<Matrix4X4<f32>> map = _transformsMemory.Map<Matrix4X4<f32>>())
        {
            Span<Matrix4X4<f32>> frame_data = map.AsSpan(Graphics.FrameIndex, 1);
            frame_data[0] = Matrix4X4.CreateFromYawPitchRoll((f32)(_rotation * AMath.DegToRad), 0.0F, 0.0F) *
                            Matrix4X4.CreateTranslation(0.0F, 0.0F, 0.0F) * 
                            Matrix4X4<f32>.Identity;
        }
    }

    protected override void PhysicsFrame() { /* todo */ }

    public override void Delete()
    {
        base.Delete();

        Draw.UnassignDrawer(this, 0);

        _controller      .Dispose(      );
                
        _ground          .Dispose(      );
        _groundMaterial  .Dispose(      );
        _groundTexture   .Dispose(      );

        _transformsBuffer.Destroy(Device);
        _transformsMemory.Dispose(      );
    }

    public void CmdDraw(SlimCommandBuffer cmd, Vector2D<UInt32> extent, UInt32 cameraIndex, UInt32 frameIndex)
    {
        _groundMaterial.CmdBindMaterial(cmd, extent, cameraIndex, frameIndex);
        _ground.CmdBindBuffers(cmd);
        VK.API.CmdDrawIndexed(cmd, _ground.IndexCount, 1U, 0U, 0, 0U);
    }
}