using System.Runtime.CompilerServices;
using Atom.Engine.Shader;
using Atom.Engine.Tree;
using Atom.Game.Config;
using Silk.NET.Maths;
using Atom.Engine.Vulkan;
using Atom.Game;

using dds = Atom.Engine.Loaders.DDS;

namespace Atom.Engine.Astro;

public class VoxelBody : CelestialBody
{
    // rotation
    // axial tilt written in CelestialBody's rotated space
    private f64 _rotationPeriod; // day length
    private f64 _baseRotation  ; // base rotation angle
    private f64 _rotation      ; // current rotation angle
    
    
    
    public f64 RotationPeriod
    {
        get => _rotationPeriod;
        set => _rotationPeriod = value;
    }
    public f64 BaseRotation
    {
        get => _baseRotation;
        set => _baseRotation = value % AMath.TAU;
    }

    public f64 Rotation => _rotation;
    

    public ReadOnlyMesh<GVertex, u16> SimplifiedMesh { get; set; }

    public readonly Grid Grid;
    
    
    
    
    public VoxelBody(PlanetConfig config, ICelestialBody reference) : base(config, reference)
    {
        RotatedSpace = new Space(CelestialSpace, $"{config.Name} rotated space");
        RotatedSpace.LocalScale = new Vector3D<f64>(Radius);

        PlanetConfig.RotationConfig.InclinationConfig tilt = 
            config.Rotation?.Inclination ?? 
            new PlanetConfig.RotationConfig.InclinationConfig();

        Quaternion<f64> axial_tilt = Quaternion<f64>.CreateFromAxisAngle(Vector3D<f64>.UnitY, tilt.RightAscension)
                                   * Quaternion<f64>.CreateFromAxisAngle(Vector3D<f64>.UnitX, tilt.Obliquity     );
        
        _rotationPeriod = config.Rotation?.Day ?? 0.0D;
        
        CelestialSpace.LocalRotation = axial_tilt;

        PlanetConfig.OrbitConfig orbit = config.Orbit;

        Orbit = orbit.SemiMajorAxis == 0.0D ? 
            new NoOrbit() :
            new RailOrbit(this, orbit);
        
        // Grid = new Grid(this);
        // Grid.SpawnTerrain();
        
        Display(true);
        
        MakeReady();
    }
    
    public override void Delete()
    {
        base.Delete();

        // First recursively delete all the satellites
        foreach (ICelestialBody satellite in Satellites)
        {
            satellite.Dispose();
        }
        
        // TEMPORARY
        Display(state: false);
    }
    
    protected internal override void Frame()
    {
        base.Frame();

        f64 ut = Astrophysics.UniversalTime;
        
        // Update celestial space relative position to parent with orbit's coordinates. 
        Vector3D<f64> rel_pos = Orbit!.GetRelativePosition(ut);
        CelestialSpace.LocalPosition = rel_pos;

        // Update rotated space rotation on current one.
        _rotation = GetRotation(ut);
        RotatedSpace.LocalRotation = Quaternion<f64>.CreateFromAxisAngle(Vector3D<f64>.UnitY, _rotation);
    }

    public f64 GetCurrentRotation() => GetRotation(Astrophysics.UniversalTime);

    public f64 GetRotation(f64 universalTime)
    {
        // Rotation of the planet
        // If the planet's rotation is static, simply assign the base rotation.
        if (_rotationPeriod == 0.0D) return -_baseRotation;

        // Otherwise get current rotation at specified time (onto base rotation)
        f64 rad_per_sec = (1.0D / _rotationPeriod) * AMath.TAU;
        f64 rot_at_ut = _baseRotation + (rad_per_sec * universalTime);
        return -(rot_at_ut % AMath.TAU);
    }



    // TEMPORARY
    private static ReadOnlyMesh<GVertex, u32> _mesh;
    private static Texture _white ;
    private static Texture _black ;
    private static Texture _purple;
    private static Texture _se    ;
    private bool                       _displaying;
    private IRasterShader?             _shader;
    private RasterizedMaterial?        _material;
    private BufferSubresource?         _transformsSubresource;
    private BufferSubresource?         _settingsSubresource;
    private MemoryMap<Matrix4X4<f32>>? _transformMap;
    private Drawer?                    _drawer;
    private VertexSettings             _vertexSettings = new() { Height = 0.05F };
    private Drawer.MeshBounding _bound;
    struct VertexSettings
    {
        public float Height;
    }
    
    
    private void CmdDraw(Camera camera, CommandRecorder.RenderPassRecorder renderPass,
        ReadOnlySpan<Drawer.DrawRange> ranges)
    {
        if (_material == null! || IsDeleted || !_displaying) return;
        
        using (MemoryMap<VertexSettings> map = _settingsSubresource.Segment.Map<VertexSettings>())
        {
            Span<VertexSettings> frame_data = map.AsSpan(0, 1);
            frame_data[0] = _vertexSettings;
        }

        if (ClassicPlayerController.Singleton != null!)
        {
            Matrix4X4<f64> render_matrix = RotatedSpace.RelativeMatrix(Camera.World!.Location);

            Span<Matrix4X4<f32>> frame_data = _transformMap.AsSpan(0 * Graphics.MAX_FRAMES_COUNT + Graphics.FrameIndex, 1);
            frame_data[0] = (Matrix4X4<f32>)render_matrix;
        
            Vector3D<f64> rel_pos = (this.CelestialSpace.Location - Camera.World!.Location).Position;

            _bound.Position = rel_pos;
        }

        renderPass.BindMaterial(_material, camera);
        renderPass.DrawIndexed(_mesh);
    }

    private ReadOnlySpan<Drawer.MeshBounding> GetMeshesBounds()
    {
        return _bound.AsSpan();
    }
    
    public void Display(bool state)
    {
        if (state && !_displaying)
        {
            _shader = Shader.Shader.Load<IRasterShader>("Engine", "Standard"); 
            _material = new RasterizedMaterial(_shader);
            
            // default every transformation to identity
            Span<Matrix4X4<f32>> transforms = stackalloc Matrix4X4<f32>[(i32)Graphics.MAX_FRAMES_COUNT];
            Span<VertexSettings> vertex_settings = stackalloc VertexSettings[(i32)Graphics.MAX_FRAMES_COUNT];
    
            for (i32 i = 0; i < Graphics.MAX_FRAMES_COUNT; i++)
            {
                transforms[i] = Matrix4X4<f32>.Identity;
                vertex_settings[i] = default; // in case memory isn't nulled.
            }
            
            _transformsSubresource = transforms.CreateVulkanMemory(
                device: _material.Device              ,
                usages: BufferUsageFlags.StorageBuffer,
                type  : MemoryType.DeviceLocalShared
            );
    
            _settingsSubresource = vertex_settings.CreateVulkanMemory(
                device: _material.Device              ,  
                usages: BufferUsageFlags.UniformBuffer,
                type  : MemoryType.DeviceLocalShared
            );
            
            u64 transform_length = (u64)Unsafe.SizeOf<Matrix4X4<f32>>() * Graphics.MAX_FRAMES_COUNT;
            
            _material.WriteBuffer<IVertexModule>(
                name        : "_instanceData",
                subresource : _transformsSubresource.Subresource(start: 0UL, length: transform_length) 
            );
    
            _material.WriteBuffer<IVertexModule>(
                name          : "_settings",
                subresource   : _settingsSubresource.Subresource(start: 0UL, length: (u64)Unsafe.SizeOf<VertexSettings>()),
                descriptorType: vk.DescriptorType.UniformBuffer
            );
    
            _transformMap = _transformsSubresource.Segment.Map<Matrix4X4<f32>>();
            
            _material.WriteImage<IFragmentModule>(name: "_albedo", texture: _se);
            _material.WriteImage<IFragmentModule>(name: "_normal", texture: _purple);
            _material.WriteImage<IFragmentModule>(name: "_ambientOcclusion", texture: _black);
            _material.WriteImage<IFragmentModule>(name: "_metalness", texture: _black);
            _material.WriteImage<IFragmentModule>(name: "_roughness", texture: _black);
            _material.WriteImage<IVertexModule>(name: "_height", texture: _black);
            _material.WriteBuffer<IVertexModule>(name: "_cameraMatrices", subresource: Camera.ShaderData);
            
            _bound = new Drawer.MeshBounding
            {
                CallIndex = 0,
                Bounding = Radius
            };
    
            _drawer = new Drawer(CmdDraw, GetMeshesBounds, Camera.World!);
        }
        else if (!state && _displaying)
        {
            _drawer.Dispose();          
        
            _material.Delete();
            _shader.Dispose();

            _settingsSubresource.Delete();
            _transformMap.Dispose();
            _transformsSubresource.Delete();
        }

        _displaying = state;
    }

    public static void Initialize()
    {
        _mesh   = ReadOnlyMesh.Load<u32>("assets/Meshes/SimpleSphere.obj");
        _white  = new Texture(image: dds.Load(stream: File.OpenRead(path: "assets/Images/white.dds"       )));
        _black  = new Texture(image: dds.Load(stream: File.OpenRead(path: "assets/Images/black.dds"       )));
        _purple = new Texture(image: dds.Load(stream: File.OpenRead(path: "assets/Images/white_normal.dds")));
        _se = new Texture(image: dds.Load(stream: File.OpenRead(path: "assets/Images/thumbnail.dds")));
    }

    public static void Cleanup()
    {
        _mesh.Delete();
        _white.Dispose();
        _black.Dispose();
        _purple.Dispose();
        _se.Delete();
    }
}