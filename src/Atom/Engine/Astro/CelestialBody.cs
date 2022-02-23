using Atom.Engine.Astro.Transvoxel;
using Atom.Engine.Global;
using Atom.Engine.Shader;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using SPIRVCross;

namespace Atom.Engine.Astro;

public class CelestialBody : AtomObject, ICelestialBody, IDrawer
{
    public bool IsStatic { get; }

    public Orbit? Orbit { get; }
    
    
    public Space EquatorialSpace { get; }
    
    public Space RotatedSpace { get; }
    
    
    // kg
    public double Mass { get; }
    
    // meter
    public double Radius { get; }

    public double Diameter => Radius * 2.0D;

    // m^3
    public double Volume => (Radius * Radius * Radius) * (4.0D/3.0D) * Math.PI;

    // kg / m^3
    public double Density => Mass / Volume;
    
    // m/s²
    public double SurfaceGravity => (Astrophysics.G * Mass) / Radius * Radius;
    
    public double SurfaceG => SurfaceGravity / Astrophysics.EarthSurfaceGravity;
    


    public Grid Grid { get; }

    private IRasterizedMaterial DebugTerrainMaterial;

    private ulong _vertexBufferOffset, _indexBufferOffset, _instanceDataOffset;
    private uint _indexCount;
    private ulong _vertexSize;
    
    private SlimBuffer _meshBuffer;
    private DeviceMemory _meshMemory;
    
    
    
    public CelestialBody(
        string name, 
        double radius, double mass,
        IRasterShader debugTerrainShader,
        ICelestialBody parent/*, Orbit? orbit = null*/)
    {
        Name = name;
        
        Radius = radius;
        Mass = mass;

        EquatorialSpace = new Space(parent.EquatorialSpace);

        Grid = new Grid(5U);
        Grid.Init();

        Grid.Generator = (x, y, z) =>
        {
            return x * x + y * y + z * z - 1.0F;
            //return Math.Max(Math.Max(x, y), z);
        };
        
        DebugTerrainMaterial = new RasterizedMaterial(debugTerrainShader);

        DebugTerrainAll();
        
        Draw.AssignDrawer(this, cameraIndex: 0);
    }

    public unsafe void DebugTerrainAll()
    {
        Cell cell = Grid.Cells.First();
        cell.FillData();
        (GVertex[] vert, uint[] indices) verts = cell.Visit(true);
        
        //(GVertex[] vert, uint[] indices) verts = WavefrontLoader.ImportFile<uint>("Assets/Meshes/SuzanneAxis.obj");

        /*(GVertex[] vert, uint[] indices) verts = new (
            new GVertex[3]
            {
                new GVertex() { Position = new (-1.0F,-3.0F, 0.0F) },
                new GVertex() { Position = new (-1.0F, 1.0F, 0.0F) },
                new GVertex() { Position = new ( 3.0F, 1.0F, 0.0F) },
            },
            new uint[3]
            {
                0, 1, 2
            }
        );*/
        
        _indexCount = (uint)verts.indices.Length;

        ulong vert_size = _vertexSize = (ulong)verts.vert.Length * (ulong)sizeof(GVertex);
        ulong indices_size = (ulong)_indexCount * sizeof(uint);
        ulong instanced_size = (ulong)sizeof(Matrix4X4<float>);

        _vertexBufferOffset = 0UL;
        _indexBufferOffset = AMath.Align(_vertexBufferOffset + vert_size, 0x10);
        _instanceDataOffset = AMath.Align(_indexBufferOffset + indices_size, 0x10);
        
        //AMath.()

        ulong buffer_size = _instanceDataOffset + instanced_size;

        uint queue_fam = 0U;

        Device device = DebugTerrainMaterial.Device;
        
        _meshBuffer = new SlimBuffer(device, 
            buffer_size, 
            usage: BufferUsageFlags.VertexBuffer        | 
                   BufferUsageFlags.IndexBuffer         | 
                   BufferUsageFlags.StorageBuffer       |
                   BufferUsageFlags.TransferDestination ,
            sharingMode: SharingMode.Exclusive, queue_fam.AsSpan()
        );
        _meshBuffer.GetMemoryRequirements(device, out MemoryRequirements reqs);
        
        _meshMemory = new DeviceMemory(
            device: device,
            size: reqs.Size, 
            VK.GPU.PhysicalDevice.FindMemoryType(
            typeFilter: reqs.MemoryTypeBits, MemoryPropertyFlags.HostVisible | MemoryPropertyFlags.HostCoherent)
        );
        _meshBuffer.BindMemory(_meshMemory.Whole);

        /*DeviceMemory staging_mesh_memory = new(
            device: device,
            size: reqs.Size, 
            VK.GPU.PhysicalDevice.FindMemoryType(
                typeFilter: reqs.MemoryTypeBits, MemoryPropertyFlags.HostVisible | MemoryPropertyFlags.HostCoherent
            )
        );*/

        Matrix4X4<float> model_matrix = Matrix4X4.Multiply(Matrix4X4.Multiply(Matrix4X4.Multiply(
            Matrix4X4.CreateFromQuaternion(Quaternion<float>.CreateFromYawPitchRoll(0.0F, 0.0F, 0.0F)),
            Matrix4X4.CreateScale(Vector3D<float>.One * (float)Radius)),
            Matrix4X4.CreateTranslation(Vector3D<float>.Zero)), 
            Matrix4X4<float>.Identity);

        using (MemoryMap<byte> map = _meshMemory.Map(_meshMemory.Whole))
        {
            fixed (GVertex* p_verts = verts.vert)
            {
                System.Buffer.MemoryCopy(p_verts, (byte*)map.Handle + _vertexBufferOffset, reqs.Size, vert_size);
            }
            fixed (uint* p_indices = verts.indices)
            {
                System.Buffer.MemoryCopy(p_indices, (byte*)map.Handle + _indexBufferOffset, reqs.Size, indices_size);
            }
            {
                System.Buffer.MemoryCopy(&model_matrix, (byte*)map.Handle + _instanceDataOffset, reqs.Size, instanced_size);
            }
        }
        
        /*SlimBuffer staging_buffer = new(device, 
            buffer_size,
            BufferUsageFlags.TransferSource ,
            sharingMode: SharingMode.Exclusive, queue_fam.AsSpan()
        );
        {
            staging_buffer.BindMemory(staging_mesh_memory.Whole);
            SlimCommandPool pool = new(device, queue_fam);
            {
                pool.AllocateCommandBuffer(device, CommandBufferLevel.Primary, out SlimCommandBuffer cmd);
                CommandBufferBeginInfo begin_info = new(
                    flags: CommandBufferUsageFlags.CommandBufferUsageOneTimeSubmitBit
                );
                VK.API.BeginCommandBuffer(cmd, in begin_info);
                {
                    BufferCopy copy = new BufferCopy(default, default, buffer_size);
                    VK.API.CmdCopyBuffer(cmd, staging_buffer, _meshBuffer, 1U, in copy);
                }
                VK.API.EndCommandBuffer(cmd);

                SlimFence fence = new (device, signaled: false);
                {
                    SubmitInfo submit_info = new(
                        commandBufferCount: 1U,
                        pCommandBuffers: (Silk.NET.Vulkan.CommandBuffer*)&cmd
                    );
                    VK.API.DeviceWaitIdle(VK.Device);
                    VK.API.QueueSubmit(VK.Queue, 1U, in submit_info, fence);
                    fence.Wait(device);
                }
                fence.Destroy(device);
            }
            pool.Destroy(device);
        }
        staging_buffer.Destroy(device);
        staging_mesh_memory.Dispose();*/
    }

    public unsafe void CmdDraw(SlimCommandBuffer cmd, Vector2D<uint> extent, uint cameraIndex, uint frameIndex)
    {
        Log.Trace($"Draw {Name} (Camera {cameraIndex}) [{frameIndex}]");
        
        DescriptorBufferInfo instance_buffer_info = new(
            _meshBuffer,
            offset: _instanceDataOffset,
            range: (ulong)sizeof(Matrix4X4<float>)
        );
        DescriptorBufferInfo camera_buffer_info = new(
            CameraData.VPMatrices,
            offset: 0,
            range: CameraData.MaxCameraCount * (ulong)sizeof(CameraVP)
        );
        
        // update instance data
        Span<WriteDescriptorSet> write_sets = stackalloc WriteDescriptorSet[2];
        write_sets[0] = /* write instance data buffer */ new WriteDescriptorSet(
            dstSet: DebugTerrainMaterial.DescriptorSets[frameIndex][ShaderStageFlags.Vertex],
            dstBinding: 0,
            dstArrayElement: 0,
            descriptorCount: 1,
            descriptorType: DescriptorType.StorageBuffer,
            pBufferInfo: &instance_buffer_info
        );
        write_sets[1] = /* write camera data buffer */ new WriteDescriptorSet(
            dstSet: DebugTerrainMaterial.DescriptorSets[frameIndex][ShaderStageFlags.Vertex],
            dstBinding: 1,
            dstArrayElement: 0,
            descriptorCount: 1,
            descriptorType: DescriptorType.StorageBuffer,
            pBufferInfo: &camera_buffer_info
        );

        
        DebugTerrainMaterial.CmdBindMaterial(cmd, extent, cameraIndex, frameIndex);
        
        VK.API.CmdBindVertexBuffers(cmd, 0U, 1U, _meshBuffer, 0U);
        VK.API.CmdBindIndexBuffer(cmd, _meshBuffer, _vertexSize, IndexType.Uint32);
        
        VK.API.UpdateDescriptorSets(DebugTerrainMaterial.Device, 
            2U, write_sets, 
            0U, ReadOnlySpan<CopyDescriptorSet>.Empty);

        VK.API.CmdDrawIndexed(cmd, _indexCount, 1U, 0U, 0, 0U);
    }

    public override void Delete()
    {
        base.Delete();
        
        Draw.UnassignDrawer(this, cameraIndex: 0);

        VK.API.DeviceWaitIdle(VK.Device); // shit but lazy way to sync, for now.
        
        _meshBuffer.Destroy(VK.Device);
        
        _meshMemory.Dispose();

        DebugTerrainMaterial.Dispose();
    }
}