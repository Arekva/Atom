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
    
    private readonly SlimBuffer _settingsBuffer;
    private readonly VulkanMemory _settingsMemory;

    private readonly ReadOnlyMesh<u32> _sphere;

    private readonly RasterizedMaterial _sphereMaterial;

    private readonly Texture _sphereAlbedo;
    private readonly Texture _sphereMetallic;
    private readonly Texture _sphereRoughness;
    private readonly Texture _sphereNormal;
    private readonly Texture _sphereAO;
    private readonly Texture _sphereHeight;

    private vk.Device Device => _sphereMaterial.Device;

    private struct VertexSettings
    {
        public f32 Height;
    }

    public ClassicScene()
    {
        _sky = new ClassicSkySphere();

        _controller = new ClassicPlayerController();

        _sphere = ReadOnlyMesh.Load<u32>("Assets/Meshes/256_UVSphere.obj");

        IRasterShader shader = Shader.Load<IRasterShader>("Engine", "Standard");

        _sphereMaterial = new RasterizedMaterial(shader);

        Span<Matrix4X4<f32>> transforms = stackalloc Matrix4X4<f32>[3];
        for (i32 i = 0; i < Graphics.MaxFramesCount; i++)
        {
            transforms[i] = Matrix4X4<f32>.Identity;
        }
        (_transformsBuffer, _transformsMemory) = transforms.CreateVulkanMemory(
            device: _sphereMaterial.Device,
            usages: BufferUsageFlags.StorageBuffer,
            properties: MemoryPropertyFlags.HostVisible  |
                        MemoryPropertyFlags.HostCoherent |
                        MemoryPropertyFlags.DeviceLocal  );

        Span<VertexSettings> vertex_settings = stackalloc VertexSettings[3];
        for (i32 i = 0; i < Graphics.MaxFramesCount; i++)
        {
            vertex_settings[i] = new VertexSettings
            {
                Height = 0.05F
            };
        }
        (_settingsBuffer, _settingsMemory) = vertex_settings.CreateVulkanMemory(
            device: _sphereMaterial.Device,  
            usages: BufferUsageFlags.UniformBuffer,
            properties: MemoryPropertyFlags.HostVisible  |
                        MemoryPropertyFlags.HostCoherent |
                        MemoryPropertyFlags.DeviceLocal  );
        
        _sphereMaterial.WriteBuffer<IVertexModule>(
            name: "_instanceData",
            subresource: new BufferSubresource(
                buffer : _transformsBuffer,
                segment: _transformsMemory.Segment(0, (u64)Unsafe.SizeOf<Matrix4X4<f32>>() * Graphics.MaxFramesCount)
            )
        );
        _sphereMaterial.WriteBuffer<IVertexModule>(
            name: "_cameraMatrices",
            subresource: new BufferSubresource(
                buffer : CameraData.VPMatrices,
                segment: CameraData.Memory.Whole
            )
        );
        
        

        
        _sphereMaterial.WriteBuffer<IVertexModule>(
            name: "_settings",
            subresource: new BufferSubresource(
                buffer : _settingsBuffer,
                segment: _settingsMemory.Segment(0, (u64)Unsafe.SizeOf<VertexSettings>())
            ),
            descriptorType: vk.DescriptorType.UniformBuffer
        );
        
        _sphereAlbedo    = new Texture(image: DDS.Load(stream: File.OpenRead("assets/Images/brick/albedo.dds")));
        _sphereMaterial.WriteImage<IFragmentModule>(name: "_albedo", texture: _sphereAlbedo);
        
        _sphereNormal    = new Texture(image: DDS.Load(stream: File.OpenRead("assets/Images/brick/normal.dds")));
        _sphereMaterial.WriteImage<IFragmentModule>(name: "_normal", texture: _sphereNormal);
        
        _sphereAO        = new Texture(image: DDS.Load(stream: File.OpenRead("assets/Images/do_not_push/rust/ao.dds")));
        //_groundMaterial.WriteImage<IFragmentModule>(name: "_ambientOcclusion", texture: _groundAO);
        
        _sphereMetallic  = new Texture(image: DDS.Load(stream: File.OpenRead("assets/Images/do_not_push/rust/metallic.dds")));
        _sphereMaterial.WriteImage<IFragmentModule>(name: "_metalness", texture: _sphereMetallic);
        
        _sphereRoughness = new Texture(image: DDS.Load(stream: File.OpenRead("assets/Images/do_not_push/rust/roughness.dds")));
        _sphereMaterial.WriteImage<IFragmentModule>(name: "_roughness", texture: _sphereRoughness);
        
        _sphereHeight   = new Texture(image: DDS.Load(stream: File.OpenRead("assets/Images/brick/height.dds")));
        _sphereMaterial.WriteImage<IVertexModule>(name: "_height", texture: _sphereHeight);

        Draw.AssignDrawer(this, 0);
    }

    private f64 _rotation = 0.0D;
    
    VertexSettings _vertexSettings = new ()
    {
        Height = 0.05F
    };

    protected override void Frame()
    {
        if (_transformsMemory == null!) return;

        _rotation += Time.DeltaTime * 60.0/360D;
        using (MemoryMap<Matrix4X4<f32>> map = _transformsMemory.Map<Matrix4X4<f32>>())
        {
            Span<Matrix4X4<f32>> frame_data = map.AsSpan(Graphics.FrameIndex, 1);
            frame_data[0] = Matrix4X4.CreateFromYawPitchRoll((f32)(_rotation * AMath.DegToRad), 0.0F, 0.0F) *
                            Matrix4X4.CreateTranslation(0.0F, 0.0F, 0.0F) * 
                            Matrix4X4<f32>.Identity;
        }

        using (MemoryMap<VertexSettings> map = _settingsMemory.Map<VertexSettings>())
        {
            Span<VertexSettings> frame_data = map.AsSpan(0, 1);
            frame_data[0] = _vertexSettings;
        }
    }

    protected override void PhysicsFrame() { /* todo */ }

    public override void Delete()
    {
        base.Delete();

        Draw.UnassignDrawer(this, 0);

        _controller      .Dispose(      );
                
        _sphere          .Dispose(      );
        _sphereMaterial  .Dispose(      );
        
        _sphereAlbedo.Dispose();
        _sphereMetallic.Dispose();
        _sphereRoughness.Dispose();
        _sphereNormal.Dispose();
        _sphereAO.Dispose();
        _sphereHeight.Dispose();

        _transformsBuffer.Destroy(Device);
        _transformsMemory.Dispose(      );
        
        _settingsBuffer.Destroy(Device);
        _settingsMemory.Dispose(      );
    }

    public void CmdDraw(SlimCommandBuffer cmd, Vector2D<UInt32> extent, UInt32 cameraIndex, UInt32 frameIndex)
    {
        _sphereMaterial.CmdBindMaterial(cmd, extent, cameraIndex, frameIndex);
        _sphere.CmdBindBuffers(cmd);
        VK.API.CmdDrawIndexed(cmd, _sphere.IndexCount, 1U, 0U, 0, 0U);
    }
}