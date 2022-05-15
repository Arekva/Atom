using System.Runtime.CompilerServices;
using Atom.Engine.Shader;
using Atom.Game.Config;
using Silk.NET.Maths;
using Atom.Engine.Vulkan;
using Atom.Game;

using dds = Atom.Engine.Loaders.DDS;

namespace Atom.Engine.Astro;

public class VoxelBody : CelestialBody
{
    private static ReadOnlyMesh<u32> _mesh;

    private IRasterShader _shader;
    private RasterizedMaterial _material;
    
    /*private readonly Texture _sphereAlbedo;
    private readonly Texture _sphereMetallic;
    private readonly Texture _sphereRoughness;
    private readonly Texture _sphereNormal;
    private readonly Texture _sphereAO;
    private readonly Texture _sphereHeight;*/

    private readonly Texture _white ;
    private readonly Texture _black ;
    private readonly Texture _purple;


    private readonly SlimBuffer _transformsBuffer;
    private readonly VulkanMemory _transformsMemory;
    
    private readonly SlimBuffer _settingsBuffer;
    private readonly VulkanMemory _settingsMemory;

    private Drawer _drawer;

    private bool isReady;

    private VertexSettings _vertexSettings = new VertexSettings() { Height = 0.05F };

    private Drawer.MeshBounding _bound;

    public f64 BaseRotation { get; set; } = 0.0D;
    public f64 Rotation { get; private set; } = 0.0D;
    public f64 Day { get; set; } = 0.0D;

    public ReadOnlyMesh<u16> SimplifiedMesh { get; set; }




    struct VertexSettings
    {
        public float Height;
    }

    static VoxelBody()
    {
        _mesh = ReadOnlyMesh.Load<u32>("assets/Meshes/256_UVSphere.obj");
    }
    
        
    public VoxelBody(PlanetConfig config, ICelestialBody reference) : base(config, reference)
    {
        RotatedSpace = new Space(CelestialSpace, $"{config.Name} rotated space");
        RotatedSpace.LocalScale = new Vector3D<f64>(Radius);
        
        PlanetConfig.RotationConfig.InclinationConfig tilt = 
            config.Rotation?.Inclination ?? 
            new PlanetConfig.RotationConfig.InclinationConfig();

        Quaternion<f64> axial_tilt = Quaternion<f64>.CreateFromAxisAngle(Vector3D<f64>.UnitY, tilt.RightAscension)
                                   * Quaternion<f64>.CreateFromAxisAngle(Vector3D<f64>.UnitX, tilt.Obliquity);
        
        Day = config.Rotation?.Day ?? 0.0D;
        
        
        
        CelestialSpace.LocalRotation = axial_tilt;

        PlanetConfig.OrbitConfig orbit = config.Orbit;

        Orbit = orbit.SemiMajorAxis == 0.0D ? 
            new NoOrbit() :
            new RailOrbit(this, orbit);
        
        _shader = Shader.Shader.Load<IRasterShader>("Engine", "Standard"); 
        _material = new RasterizedMaterial(_shader);
        
        Span<Matrix4X4<f32>> transforms = stackalloc Matrix4X4<f32>[3];
        for (i32 i = 0; i < Graphics.MaxFramesCount; i++)
        {
            transforms[i] = Matrix4X4<f32>.Identity;
        }
        (_transformsBuffer, _transformsMemory) = transforms.CreateVulkanMemory(
            device: _material.Device,
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
            device: _material.Device,  
            usages: BufferUsageFlags.UniformBuffer,
            properties: MemoryPropertyFlags.HostVisible  |
                        MemoryPropertyFlags.HostCoherent |
                        MemoryPropertyFlags.DeviceLocal  );
        
        _material.WriteBuffer<IVertexModule>(
            name: "_instanceData",
            subresource: new BufferSubresource(
                buffer : _transformsBuffer,
                segment: _transformsMemory.Segment(0, (u64)Unsafe.SizeOf<Matrix4X4<f32>>() * Graphics.MaxFramesCount)
            )
        );
        /*_material.WriteBuffer<IVertexModule>(
            name: "_cameraMatrices",
            subresource: new BufferSubresource(
                buffer : CameraData.VPMatrices,
                segment: CameraData.Memory.Whole
            )
        );*/

        _material.WriteBuffer<IVertexModule>(
            name: "_settings",
            subresource: new BufferSubresource(
                buffer : _settingsBuffer,
                segment: _settingsMemory.Segment(0, (u64)Unsafe.SizeOf<VertexSettings>())
            ),
            descriptorType: vk.DescriptorType.UniformBuffer
        );
        
        const string WHITE = "assets/Images/white.dds";
        const string BLACK = "assets/Images/black.dds";
        const string WHITE_NORMAL = "assets/Images/white_normal.dds";

        u32 queue_family = 0;
        Span<u32> queue_families = queue_family.AsSpan();
        
        _white = new Texture(image: dds.Load(stream: File.OpenRead(WHITE), queue_families));
        _black = new Texture(image: dds.Load(stream: File.OpenRead(BLACK), queue_families));
        _purple = new Texture(image: dds.Load(stream: File.OpenRead(WHITE_NORMAL), queue_families));
        
        //_sphereAlbedo    = new Texture(image: dds.Load(stream: File.OpenRead(config.Texture?.Color ?? WHITE)));
        _material.WriteImage<IFragmentModule>(name: "_albedo", texture: _white);
        
        //_sphereNormal    = new Texture(image: dds.Load(stream: File.OpenRead(config.Texture?.Normal ?? WHITE_NORMAL)));
        _material.WriteImage<IFragmentModule>(name: "_normal", texture: _purple);
        
        //_sphereAO        = new Texture(image: dds.Load(stream: File.OpenRead(BLACK)));
        _material.WriteImage<IFragmentModule>(name: "_ambientOcclusion", texture: _black);
        
        //_sphereMetallic  = new Texture(image: dds.Load(stream: File.OpenRead(BLACK)));
        _material.WriteImage<IFragmentModule>(name: "_metalness", texture: _black);
        
        //_sphereRoughness = new Texture(image: dds.Load(stream: File.OpenRead(BLACK)));
        _material.WriteImage<IFragmentModule>(name: "_roughness", texture: _black);
        
        //_sphereHeight   = new Texture(image: dds.Load(stream: File.OpenRead(config.Texture?.Height ?? BLACK)));
        _material.WriteImage<IVertexModule>(name: "_height", texture: _black);
        
        _material.WriteBuffer<IVertexModule>(name: "_cameraMatrices", subresource: Camera.ShaderData);
        
        _bound = new Drawer.MeshBounding
        {
            CallIndex = 0,
            Bounding = Radius
        };

        _drawer = new Drawer(CmdDraw, GetMeshesBounds, Camera.World!);
        
        Log.Info(Name + " Loaded.");

        MakeReady();
    }

    protected internal override void Frame()
    {
        base.Frame();

        f64 ut = Astrophysics.UniversalTime;
        
        Vector3D<f64> rel_pos = Orbit!.GetRelativePosition(ut);
        CelestialSpace.LocalPosition = rel_pos;


        if (Day == 0.0D)
        {
            Rotation = -(BaseRotation % AMath.TAU);
        }
        else
        {
            f64 rad_per_sec = (1.0D / Day) * AMath.TAU;
            f64 rot_at_ut = BaseRotation + (rad_per_sec * ut);
            Rotation = -(rot_at_ut % AMath.TAU);
        }
        
        RotatedSpace.LocalRotation = Quaternion<f64>.CreateFromAxisAngle(Vector3D<Double>.UnitY, Rotation);
        
        using (MemoryMap<VertexSettings> map = _settingsMemory.Map<VertexSettings>())
        {
            Span<VertexSettings> frame_data = map.AsSpan(0, 1);
            frame_data[0] = _vertexSettings;
        }
    }

    protected internal override void Render()
    {
        base.Render();
        
        if (ClassicPlayerController.Singleton == null!) return;
        
        //ref Matrix4X4<f64> render_matrix = ref CelestialSpace[Camera.World!.Index, Graphics.FrameIndex];
        Matrix4X4<f64> render_matrix = RotatedSpace.RelativeMatrix(Camera.World!.Location);

        using (MemoryMap<Matrix4X4<f32>> map = _transformsMemory.Map<Matrix4X4<f32>>())
        {
            Span<Matrix4X4<f32>> frame_data = map.AsSpan(Graphics.FrameIndex, 1);
            frame_data[0] = (Matrix4X4<f32>)render_matrix;
        }
        
        Vector3D<f64> rel_pos = (this.CelestialSpace.Location - Camera.World!.Location).Position;

        _bound.Position = rel_pos;
    }

    private void CmdDraw(Camera camera, CommandRecorder.RenderPassRecorder renderPass,
        ReadOnlySpan<Drawer.DrawRange> ranges,
        Vector2D<u32> resolution, u32 frameIndex)
        //SlimCommandBuffer cmd, Vector2D<UInt32> extent, UInt32 cameraIndex, UInt32 frameIndex
        //)
    {
        if (_material == null!) return;
        
        _material.CmdBindMaterial(renderPass.CommandBuffer, resolution, camera.Index, frameIndex);
        _mesh.CmdBindBuffers(renderPass.CommandBuffer);
        VK.API.CmdDrawIndexed(renderPass.CommandBuffer, _mesh.IndexCount, 1, 0, 0, 0);
    }

    private ReadOnlySpan<Drawer.MeshBounding> GetMeshesBounds()
    {
        return _bound.AsSpan();
    }

    public override void Delete()
    {
        base.Delete();

        VK.API.DeviceWaitIdle(VK.Device); // bruh todo: remove that and do proper synchronisation

        foreach (ICelestialBody satellite in Satellites)
        {
            satellite.Dispose();
        }
        
        _drawer.Dispose();          
        
        _material.Delete();
        _shader.Dispose();
        _mesh.Delete();
        
        _white.Dispose();
        _black.Dispose();
        _purple.Dispose();
        
        _settingsBuffer.Destroy(VK.Device);
        _settingsMemory.Delete();
        
        _transformsBuffer.Destroy(VK.Device);
        _transformsMemory.Delete();

        /*_sphereAlbedo.Dispose();
        _sphereMetallic.Dispose();
        _sphereRoughness.Dispose();
        _sphereNormal.Dispose();
        _sphereAO.Dispose();
        _sphereHeight.Dispose();*/
    }
}