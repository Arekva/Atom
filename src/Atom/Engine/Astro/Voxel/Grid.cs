using System.Runtime.CompilerServices;
using Atom.Engine.GraphicsPipeline;
using Atom.Engine.Shader;
using Atom.Engine.Tree;
using Atom.Engine.Vulkan;
using Silk.NET.Maths;

namespace Atom.Engine.Astro;

public class Grid : Octree<Cell>
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
    private BufferSubresource _debugMatricesSubresource;

    private MemoryMap<u8> _settingsMap;

    private ReadOnlyMesh<DebugLineVertex, u8> _linesMesh;

    private bool _isReady;

    private Space _space;

    private struct DebugLineVertex
    {
        public Vector3D<f32> Position;
    }

    
    const u64 VOXEL_TERRAIN_COUNT = 1024;
    const u64 DEBUG_CHUNK_DISPLAY_COUNT = 1<<16;

    public Grid(VoxelBody body)
    {
        _body = new WeakReference<VoxelBody>(target: body);
        _space = new(body.RotatedSpace, $"{body.Name} main grid space");
        
        u64 VOXEL_MESH_SIZE = Cell.MAX_VERTEX_COUNT * (u64)Unsafe.SizeOf<GVertex>() + Cell.MAX_INDEX_COUNT * sizeof(u16);
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
        _debugMaterial.DepthStencil.DepthCompareOp = vk.CompareOp.GreaterOrEqual;

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

        u64 sizeof_vertex_sets = (u64)Unsafe.SizeOf<Matrix4X4<f32>>() * Graphics.MAX_FRAMES_COUNT * DEBUG_CHUNK_DISPLAY_COUNT;
        u64 sizeof_fragment_sets = (u64)Unsafe.SizeOf<Vector4D<f32>>() * DEBUG_CHUNK_DISPLAY_COUNT;
        
        _debugSettingsSubresource = VK.CreateMemory<u8>(
            count     : sizeof_vertex_sets + sizeof_fragment_sets, 
            usages    : BufferUsageFlags.StorageBuffer, 
            properties: MemoryPropertyFlags.HostVisible  | 
                        MemoryPropertyFlags.HostCoherent | 
                        MemoryPropertyFlags.DeviceLocal  );

        
        
        _debugMaterial.WriteBuffer<IVertexModule>("_cameraMatrices", Camera.ShaderData);
        
        _debugMatricesSubresource = _debugSettingsSubresource.Subresource(start: 0UL, length: sizeof_vertex_sets);
        _debugMaterial.WriteBuffer<IVertexModule>("_instanceData", _debugMatricesSubresource);
        
        _debugFragmentSubresource = _debugSettingsSubresource.Subresource(start: sizeof_vertex_sets, length: sizeof_fragment_sets);
        _debugMaterial.WriteBuffer<IFragmentModule>("_instanceData", _debugFragmentSubresource);

        _settingsMap = _debugSettingsSubresource.Segment.Map();
        
        _isReady = true;
    }

    public void SpawnTerrain()
    {
        if (_terrainSpawned) return;

        _drawer = new Drawer(TerrainDraw, TerrainBounds);
        Initialize();
        
        _body.TryGetTarget(out VoxelBody? body);
        
        u128 loc = GetTreeLocation(new Location(Vector3D<f64>.UnitY * body.Radius));
        SubdivideToSmooth(loc);

        Parallel.ForEach(Nodes, node =>
        {
            if (!node.HasBranches)
            {
                Log.Info($"Spawning chunk {node}");
                Cell cell = node.Data = new Cell(node);
                cell.Generate(body.Radius);
            }
        });
        
        _terrainSpawned = true;
    }

    public void DispawnTerrain()
    {
        if (_terrainSpawned) return;
        
        Root = null!; // GC should be automatically collecting this.
        _drawer!.Delete();
        
        
        _terrainSpawned = false;
    }

    public u128 GetTreeLocationWorld(Location worldLocation, u32 depth = MAX_SUBDIVISIONS)
    {
        _body.TryGetTarget(out VoxelBody? body);
        Location relative_location = worldLocation - body!.CelestialSpace.Location;
        return GetTreeLocation(relative_location, depth);
    }

    public u128 GetTreeLocation(Location localPosition, u32 depth = MAX_SUBDIVISIONS)
    {
        u128 one = new(0, 1);
        if (depth == 0) return one;
        
        u128 location = one << (i32)(depth * DIMENSION);
        Vector3D<i64> local_position = (Vector3D<i64>)localPosition.Position;
        Vector3D<u64> grid_space_position = (Vector3D<u64>)(local_position + new Vector3D<i64>(Cell.SCALES_LONG[0]));
        Vector3D<u64> chunk_coord = grid_space_position / (u64)Cell.SIZE;

        for (i32 i = 1; i <= depth; i++)
        {
            i32 offset = (i32)(depth-i);
            u8 local_location = 0b000;
            
            local_location |= (u8)(((chunk_coord.X >> offset) & 0b1) << 2); // x
            local_location |= (u8)(((chunk_coord.Y >> offset) & 0b1) << 1); // y
            local_location |= (u8)(((chunk_coord.Z >> offset) & 0b1) << 0); // z
            
            location |= (new u128(0, local_location) << (i32)((depth-i) * DIMENSION));
        }

        return location;
    }

    public Location GetGridLocation(u128 location, u32 depth)
    {
        if (depth == 0) return Location.Origin;
        
        Location position = Location.Origin;

        for (u32 i = depth; i > 0; i--)
        {
            u8 loc_loc = (u8)(location.Low & 0b111);
            
            Location norm_pos = new (Cell.POSITIONS[loc_loc]);
            norm_pos.Scale(Cell.SIZES[i]);
            position += norm_pos;

            location >>= (i32)DIMENSION;
        }
        return position;
    }

    private void CreateVoxel(Node<Cell> node)
    {
        //node.Data = new Chunk(node, this);
    }

    private void TerrainDraw(Camera camera, CommandRecorder.RenderPassRecorder renderPass,
        ReadOnlySpan<Drawer.DrawRange> ranges)
    {
        
    }

    private ReadOnlySpan<Drawer.MeshBounding> TerrainBounds()
    {
        return ReadOnlySpan<Drawer.MeshBounding>.Empty;
    }
    
    private void DebugDraw(Camera camera, CommandRecorder.RenderPassRecorder renderPass,
        ReadOnlySpan<Drawer.DrawRange> ranges)
    {
        //return;
        if (!_isReady) return;
        
        _body.TryGetTarget(out VoxelBody? body);

        const u32 SHOW_AFTER = 20;
        
        i32 node_count = 0;
        
        Span<u8> color_map_raw = _settingsMap.AsSpan(
            _debugFragmentSubresource.Segment.Offset,
            _debugFragmentSubresource.Segment.Size
        );
        Span<u8> vertex_map_raw = _settingsMap.AsSpan(
            _debugMatricesSubresource.Segment.Offset,
            _debugMatricesSubresource.Segment.Size
        );
        Span<Vector4D<f32>> color_map; 
        Span<Matrix4X4<f32>> vertex_map;

        unsafe
        {
            fixed (u8* color_map_ptr = color_map_raw)
                color_map = new(color_map_ptr, (i32)_debugFragmentSubresource.Segment.Size/sizeof(Vector4D<f32>));
            
            fixed (u8* vertex_map_ptr = vertex_map_raw)
                vertex_map = new(vertex_map_ptr, (i32)_debugMatricesSubresource.Segment.Size/sizeof(Matrix4X4<f32>));
        }
        


        Vector4D<f64> min_color = new(1.0D, 0.0D, 0.0D, 1.0D);
        Vector4D<f64> max_color = new(0.0D, 1.0D, 0.0D, 1.0D);
        
        Location grid_location = body!.CelestialSpace.Location;
        Quaternion<f64> rotation = body.RotatedSpace.Rotation;
        
        foreach(Node<Cell> node in Nodes)
        {
            if (node == null!) continue;
            if (!node.Equals(Root) && node.HasBranches) continue;
            
            // FRAGMENT
            u32 depth = node.Depth;
            if (depth != 0 && depth < SHOW_AFTER) continue;
            
            color_map[node_count] = node.Equals(Root) ?
                new Vector4D<f32>(0.0F, 0.0F, 1.0F, 0.0F)
                :
                (Vector4D<f32>)Vector4D.Lerp(min_color, max_color, depth/(f64)MAX_SUBDIVISIONS);

            // VERTEX

            f64 scale = Cell.SCALES[depth];
            
            Location location = GetGridLocation(node.Location, depth);
            location.Rotate(rotation);
            
            location += grid_location;
            
            Vector3D<f32> rel_pos_cam = (Vector3D<f32>)(location - Camera.World!.Location).Position;
            
            vertex_map[node_count * (i32)Graphics.MAX_FRAMES_COUNT + (i32)renderPass.FrameIndex] = Matrix4X4.Multiply(Matrix4X4.Multiply(
                    Matrix4X4.CreateFromQuaternion((Quaternion<f32>)rotation),
                    Matrix4X4.CreateScale((f32)scale)), 
                    Matrix4X4.CreateTranslation(rel_pos_cam));

            ++node_count;
        }
        
        _debugMaterial.CmdBindMaterial(renderPass.CommandBuffer, renderPass.Resolution, renderPass.FrameIndex);
        
        renderPass.DrawIndexed(_linesMesh, instanceCount: (u32)node_count);
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
        
        _settingsMap.Dispose();

        _debugDrawer.Delete();
        _debugMaterial.Delete();
        _debugShader.Delete();
        
        _linesMesh.Delete();
        _debugSettingsSubresource.Delete();

        base.Delete(doCollect);
    }
}