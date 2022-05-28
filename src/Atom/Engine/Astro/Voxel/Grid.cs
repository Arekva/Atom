using System.Runtime.CompilerServices;
using Atom.Engine.GraphicsPipeline;
using Atom.Engine.Shader;
using Atom.Engine.Tree;
using Atom.Engine.Vulkan;
using Silk.NET.Maths;

namespace Atom.Engine.Astro;

public class Grid : Octree<Chunk>
{
    private BufferSubresource _voxelMeshMemory;
    
    private Queue<BufferSubresource> _toBorrowBuffers;
    
    
    
    private Drawer? _drawer;

    private Drawer _debugDrawer;

    private bool _terrainSpawned;

    private WeakReference<VoxelBody> _body;



    private RasterShader _debugShader;
    private RasterizedMaterial _debugMaterial;
    
    private BufferSubresource _debugSettingsSubresource;
    private BufferSubresource _debugFragmentSubresource;
    private BufferSubresource _debugVertexSubresource;

    private ReadOnlyMesh<DebugLineVertex, u8> _linesMesh;

    private bool _isReady;

    private struct DebugLineVertex
    {
        public Vector3D<f32> Position;
    }

    private struct VertexSettings
    {
        public Matrix4X4<f32> Model;
    }

    private struct FragmentSettings
    {
        public Vector3D<f32> Color;
    }


    public Grid(VoxelBody body)
    {
        _body = new WeakReference<VoxelBody>(target: body);
        
        const u64 VOXEL_TERRAIN_COUNT = 1024;
        u64 VOXEL_MESH_SIZE = Chunk.MAX_VERTEX_COUNT * (u64)Unsafe.SizeOf<GVertex>() + Chunk.MAX_INDEX_COUNT * sizeof(u16);
        u64 VOXEL_MESH_TOTAL_SIZE = VOXEL_TERRAIN_COUNT * VOXEL_MESH_SIZE;

        _voxelMeshMemory = VK.CreateMemory<u8>(
            count     : VOXEL_MESH_TOTAL_SIZE, 
            usages    : BufferUsageFlags.IndexBuffer | BufferUsageFlags.VertexBuffer | BufferUsageFlags.TransferDestination, 
            properties: MemoryPropertyFlags.DeviceLocal
        );

        _toBorrowBuffers = new Queue<BufferSubresource>();
        for (u64 i = 0; i < VOXEL_TERRAIN_COUNT; i++)
        {
            BufferSubresource chunk_subresource = _voxelMeshMemory.Subresource(start: VOXEL_MESH_SIZE * i, length: VOXEL_MESH_SIZE);
            _toBorrowBuffers.Enqueue(chunk_subresource);
        }

        OnNodeCreated += CreateVoxel;

        _debugDrawer = new Drawer(DebugDraw, bounds: null, subpass: 2U);
        
        _debugShader = Shader.Shader.Load<RasterShader>("Atom.Space", "GridDebug");
        
        _debugMaterial = new RasterizedMaterial(_debugShader, createPipeline: false, subpass: 2U);
        

        // oh yes big "thicc" config
        _debugMaterial.Rasterizer = new Rasterizer(Rasterizer.Default);
        _debugMaterial.Rasterizer.CullMode = vk.CullModeFlags.CullModeFrontAndBack;
        _debugMaterial.Rasterizer.LineWidth = 4.0F; // todo: enable extension for line width.
        _debugMaterial.Rasterizer.PolygonMode = vk.PolygonMode.Line;

        _debugMaterial.Topology = new Topology(Topology.Default);
        _debugMaterial.Topology.Primitive = vk.PrimitiveTopology.LineList;

        _debugMaterial.ColorBlending = new ColorBlending(ColorBlending.Default);
        _debugMaterial.ColorBlending.Attachments = new[] { new vk.PipelineColorBlendAttachmentState { ColorWriteMask = ColorBlending.ALL_FLAGS }, };

        _debugMaterial.DepthStencil = new DepthStencil(DepthStencil.Default);
        _debugMaterial.DepthStencil.DepthWriteEnable = false;
        _debugMaterial.DepthStencil.DoDepthTest = true;

        /*_debugMaterial.Multisampling = new(Multisampling.Default);
        _debugMaterial.Multisampling.Count = vk.SampleCountFlags.SampleCount8Bit;*/

        _debugMaterial.CastShadows = false;
        
        _debugMaterial.CreatePipeline();

        using MeshWriter<DebugLineVertex, u8> mesh_writer = new(vertexCount: 8, indexCount: 24);

        // bounding box vertices
        Span<DebugLineVertex> vertices = mesh_writer.Vertices.AsSpan<DebugLineVertex>(count: 8);
        vertices[0].Position = new Vector3D<f32>(-1.0F, -1.0F, -1.0F);
        vertices[1].Position = new Vector3D<f32>( 1.0F, -1.0F, -1.0F);
        vertices[2].Position = new Vector3D<f32>(-1.0F,  1.0F, -1.0F);
        vertices[3].Position = new Vector3D<f32>( 1.0F,  1.0F, -1.0F);
        vertices[4].Position = new Vector3D<f32>(-1.0F, -1.0F,  1.0F);
        vertices[5].Position = new Vector3D<f32>( 1.0F, -1.0F,  1.0F);
        vertices[6].Position = new Vector3D<f32>(-1.0F,  1.0F,  1.0F);
        vertices[7].Position = new Vector3D<f32>( 1.0F,  1.0F,  1.0F);
        
        // bounding box indices
        Span<u8> indices = mesh_writer.Indices.AsSpan<u8>(count: 24);
        for (u8 i = 0; i < 8; ++i)                // 8 - X axis
        {
            indices[i] = i;
        }
        indices[08] = 0; indices[09] = 2; // Y
        indices[10] = 1; indices[11] = 3; // Y
        indices[12] = 4; indices[13] = 6; // Y
        indices[14] = 5; indices[15] = 7; // Y
        for (u8 i = 16, v = 0; i < 24; i+=2, ++v)  // 8 - Z axis
        {
            indices[i + 0] = v;
            indices[i + 1] = (u8)(v + 4);
        }

        _linesMesh = new ReadOnlyMesh<DebugLineVertex, u8>(mesh_writer);

        _debugSettingsSubresource = VK.CreateMemory<u8>(
            count     : (u64)(Unsafe.SizeOf<VertexSettings>() + Unsafe.SizeOf<FragmentSettings>()), 
            usages    : BufferUsageFlags.UniformBuffer, 
            properties: MemoryPropertyFlags.HostVisible  | 
                        MemoryPropertyFlags.HostCoherent | 
                        MemoryPropertyFlags.DeviceLocal  );

        u64 sizeof_vertex_sets = (u64)Unsafe.SizeOf<VertexSettings>();
        u64 sizeof_fragment_sets = (u64)Unsafe.SizeOf<FragmentSettings>();
        
        _debugMaterial.WriteBuffer<IVertexModule>("_cameraMatrices", Camera.ShaderData);
        
        _debugVertexSubresource = _debugSettingsSubresource.Subresource(start: 0UL, length: sizeof_vertex_sets);
        _debugMaterial.WriteBuffer<IVertexModule>("_vertexSettings", _debugVertexSubresource, vk.DescriptorType.UniformBuffer);
        
        _debugFragmentSubresource = _debugSettingsSubresource.Subresource(start: sizeof_vertex_sets, length: sizeof_fragment_sets);
        _debugMaterial.WriteBuffer<IFragmentModule>("_fragmentSettings", _debugFragmentSubresource, vk.DescriptorType.UniformBuffer);
        
        _isReady = true;
    }

    public void SpawnTerrain()
    {
        if (_terrainSpawned) return;

        _drawer = new Drawer(TerrainDraw, TerrainBounds);
        Initialize();

        _terrainSpawned = true;
    }

    public void DispawnTerrain()
    {
        if (_terrainSpawned) return;
        
        Root = null!; // GC should be automatically collecting this.
        _drawer!.Delete();
        
        
        _terrainSpawned = false;
    }

    public u128 GetLocation(Vector3D<f64> localPosition, u32 depth = 42)
    {
        u128 location = new(0, 1);
        if (depth == 0) return location;

        Vector3D<f64> previous_high     = new(Chunk.SCALES[0]) ;
        Vector3D<f64> previous_low      = -1.0D * previous_high;
        Vector3D<f64> previous_position = Vector3D<f64>.Zero   ;

        for (i32 i = 1; i <= depth; i++)
        {
            Vector3D<f64> current_scale = new(Chunk.SCALES[i]);

            Vector3D<f64> low  = previous_position - current_scale;
            Vector3D<f64> high = previous_position + current_scale;

            Vector3D<f64> depth_local_position = AMath.Map(localPosition, previous_low, previous_high, low, high);

            u8 local_location = 0b000;
            
            if (depth_local_position.X >= 0.0D)
            {
                local_location |= 0b100;
                previous_low.X += current_scale.X;
            }
            else previous_high.X -= current_scale.X;
            
            if (depth_local_position.Y >= 0.0D)
            {
                local_location |= 0b010;
                previous_low.Y += current_scale.Y;
            }
            else previous_high.Y -= current_scale.Y;
            
            if (depth_local_position.Z >= 0.0D)
            {
                local_location |= 0b001;
                previous_low.Z += current_scale.Z;
            }
            else previous_high.Z -= current_scale.Z;

            location = (location << 3) | local_location;
        }

        return location;
    }

    private void CreateVoxel(Node<Chunk> node)
    {
        //node.Data = new Chunk(node, this);
    }

    private void TerrainDraw(Camera camera, CommandRecorder.RenderPassRecorder renderPass,
        ReadOnlySpan<Drawer.DrawRange> ranges,
        Vector2D<u32> resolution, u32 frameIndex)
    {
        
    }

    private ReadOnlySpan<Drawer.MeshBounding> TerrainBounds()
    {
        return ReadOnlySpan<Drawer.MeshBounding>.Empty;
    }
    
    private void DebugDraw(Camera camera, CommandRecorder.RenderPassRecorder renderPass,
        ReadOnlySpan<Drawer.DrawRange> ranges,
        Vector2D<u32> resolution, u32 frameIndex)
    {
#if DEBUG
        if (!_isReady) return;

        using (MemoryMap<VertexSettings> vertex_settings_map = _debugVertexSubresource.Segment.Map<VertexSettings>())
        {
            _body.TryGetTarget(out VoxelBody body);

            Vector3D<f32> rel_pos_cam = (Vector3D<f32>)(body!.CelestialSpace.Location - Camera.World!.Location).Position;

            vertex_settings_map.AsSpan()[0].Model = 
                Matrix4X4.Multiply(Matrix4X4.CreateScale((f32)Chunk.SCALES[0]), Matrix4X4.Multiply(Matrix4X4.CreateTranslation(rel_pos_cam), Matrix4X4<f32>.Identity)); // write red for uniform buffer
        }
        using (MemoryMap<FragmentSettings> fragment_settings_map = _debugFragmentSubresource.Segment.Map<FragmentSettings>())
        {
            fragment_settings_map.AsSpan()[0].Color = new Vector3D<f32>(1.0F, 0.0F, 0.0F); // write red for uniform buffer
        }
        
        _debugMaterial.CmdBindMaterial(renderPass.CommandBuffer, resolution, frameIndex);
        
        _linesMesh.CmdBindBuffers(renderPass.CommandBuffer);

        renderPass.DrawIndexed(indexCount: _linesMesh.IndexCount);
#endif
    }


    private ReadOnlySpan<Drawer.MeshBounding> DebugBounds()
    {
        return ReadOnlySpan<Drawer.MeshBounding>.Empty; // don't care about 
    }

    internal BufferSubresource BorrowMeshMemory()
    {
        if (!_toBorrowBuffers.TryDequeue(out BufferSubresource? chunk_subresource))
        {
            throw new Exception("No more meshes can be borrowed for this grid.");
        }
        return chunk_subresource;
    }

    internal void RetrieveMeshMemory(BufferSubresource subresource) => _toBorrowBuffers.Enqueue(subresource);

    public override void Delete(bool doCollect = true)
    {
        _isReady = false;
        
        DispawnTerrain();
        _voxelMeshMemory.Delete();
        
        _debugDrawer.Delete();
        _debugMaterial.Delete();
        _debugShader.Delete();
        
        _linesMesh.Delete();
        _debugSettingsSubresource.Delete();

        base.Delete(doCollect);
    }
}