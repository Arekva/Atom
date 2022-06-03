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
    private BufferSubresource _debugMatricesSubresource;

    private ReadOnlyMesh<DebugLineVertex, u8> _linesMesh;

    private bool _isReady;

    private Space _space;

    private struct DebugLineVertex
    {
        public Vector3D<f32> Position;
    }


    public Grid(VoxelBody body)
    {
        _body = new WeakReference<VoxelBody>(target: body);
        _space = new(body.RotatedSpace, $"{body.Name} main grid space");
        
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

        u64 sizeof_vertex_sets = (u64)Unsafe.SizeOf<Matrix4X4<f32>>() * Graphics.MAX_FRAMES_COUNT * VOXEL_TERRAIN_COUNT;
        u64 sizeof_fragment_sets = (u64)Unsafe.SizeOf<Vector4D<f32>>() * VOXEL_TERRAIN_COUNT;
        
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
        
        _isReady = true;
    }

    public void SpawnTerrain()
    {
        if (_terrainSpawned) return;

        _drawer = new Drawer(TerrainDraw, TerrainBounds);
        Initialize();
        
        _body.TryGetTarget(out VoxelBody? body);
        if (body.Name == "Harbor")
        {
            SubdivideTo(GetTreeLocation(new Location(
                Vector3D<f64>.UnitY * body.Radius)));
                //Vector3D<f64>.UnitY * body.Radius)));
        }
        
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
        // make space for coordinates
        u128 location = one << (i32)(depth * DIMENSION);

        Vector3D<i64> POSITION_OFFSET = Vector3D<i64>.Zero; //-Vector3D<i64>.One * 1024;
        
        Vector3D<i64> local_position = (Vector3D<i64>)localPosition.Position;

        Vector3D<u64> grid_space_position = (Vector3D<u64>)(local_position + new Vector3D<i64>(Chunk.SCALES_LONG[0]) + POSITION_OFFSET);

        Vector3D<u64> chunk_coord = grid_space_position / (u64)Chunk.SIZE;
        

        for (i32 i = 1; i <= depth; i++)
        {
            i32 offset =  (i32)(depth-i);
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
            
            Location norm_pos = new (Chunk.POSITIONS[loc_loc]);
            norm_pos.Scale(Chunk.SIZES[i]);
            position += norm_pos;

            location >>= (i32)DIMENSION;
        }
        return position;
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
        if (!_isReady) return;
        
        _body.TryGetTarget(out VoxelBody? body);
        if (body!.Name != "Harbor") return;

        const u32 SHOW_AFTER = 0;

        u32 node_count = 0;
        
        using (MemoryMap<Vector4D<f32>> color_map = _debugFragmentSubresource.Segment.Map<Vector4D<f32>>())
        {
            Vector4D<f64> min_color = new(1.0D, 0.0D, 0.0D, 1.0D);
            Vector4D<f64> max_color = new(0.0D, 1.0D, 0.0D, 1.0D);
            
            foreach(Node<Chunk> node in Nodes)
            {
                if (!node.Equals(Root) && node.HasBranches) continue;
                
                u32 depth = node.Depth;
                if (depth != 0 && depth < SHOW_AFTER) continue;
                
                color_map[node_count] = node.Equals(Root) ?
                    new Vector4D<f32>(0.0F, 0.0F, 1.0F, 0.0F)
                    :
                    (Vector4D<f32>)Vector4D.Lerp(min_color, max_color, depth/(f64)MAX_SUBDIVISIONS);
                ++node_count;
            }
        }

        using (MemoryMap<Matrix4X4<f32>> matrices_map = _debugMatricesSubresource.Segment.Map<Matrix4X4<f32>>())
        {
            Location grid_location = body!.CelestialSpace.Location;
            Quaternion<f64> rotation = body.RotatedSpace.Rotation;

            u32 i = 0U;

            foreach(Node<Chunk> node in Nodes)
            {
                if (!node.Equals(Root) && node.HasBranches) continue;
                
                u32 depth = node.Depth;
                if (depth != 0 && depth < SHOW_AFTER) continue;
                
                f64 scale = Chunk.SCALES[depth];

                Location location = GetGridLocation(node.Location, depth);
                location.Rotate(rotation);

                location += grid_location;

                Vector3D<f32> rel_pos_cam = (Vector3D<f32>)(location - Camera.World!.Location).Position;

                matrices_map[i * Graphics.MAX_FRAMES_COUNT + frameIndex] = Matrix4X4.Multiply(Matrix4X4.Multiply(
                    /*Matrix4X4.CreateFromQuaternion((Quaternion<f32>)rotation),
                    Matrix4X4.CreateScale((f32)scale)), 
                    Matrix4X4.CreateTranslation(rel_pos_cam));*/
                    Matrix4X4.CreateFromQuaternion((Quaternion<f32>)rotation),
                    Matrix4X4.CreateScale((f32)scale)), 
                    Matrix4X4.CreateTranslation(rel_pos_cam));
                ++i;
            }
        }

        _debugMaterial.CmdBindMaterial(renderPass.CommandBuffer, resolution, frameIndex);
        
        _linesMesh.CmdBindBuffers(renderPass.CommandBuffer);
        
        renderPass.DrawIndexed(indexCount: _linesMesh.IndexCount, instanceCount: node_count);
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