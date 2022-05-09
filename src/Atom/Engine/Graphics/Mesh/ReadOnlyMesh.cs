using System.Runtime.CompilerServices;
using Atom.Engine.Mesh;
using Atom.Engine.Vulkan;

namespace Atom.Engine;

public abstract class ReadOnlyMesh : AtomObject
{
    protected internal static readonly Dictionary<Type, IndexType> IndexTypes = new()
    {
        { typeof(u8 ), IndexType.u8_EXT },
        { typeof(u16), IndexType.u16    },
        { typeof(u32), IndexType.u32    },
    };

    public static ReadOnlyMesh<TIndex> Load<TIndex>(
        string path, BufferSubresource? targetResource = null,
        vk.Device? device = null, vk.PhysicalDevice? physicalDevice = null)
        where TIndex : unmanaged, IFormattable, IEquatable<TIndex>, IComparable<TIndex>
    {
        (GVertex[] vertices, TIndex[] indices, f64 bounding_sphere) = Wavefront.Load<TIndex>(path);

        using MeshWriter<TIndex> writer = new(
            vertexCount: (u64)vertices.Length, indexCount: (u64)indices.Length, bounding_sphere,
            device, physicalDevice
        );
        
        u64 vertex_data_size = (u64)Unsafe.SizeOf<GVertex>() * (u64)vertices.Length;
        u64 index_data_size  = (u64)Unsafe.SizeOf<TIndex>()  *  (u64)indices.Length;
        u64 data_size        = vertex_data_size + index_data_size;

        unsafe
        {
            fixed (GVertex* p_vertices = vertices) System.Buffer.MemoryCopy(
                source                : p_vertices               ,
                destination           : (GVertex*)writer.Vertices,
                destinationSizeInBytes: data_size                ,
                sourceBytesToCopy     : vertex_data_size
            );
            fixed (TIndex* p_indices = indices) System.Buffer.MemoryCopy(
                source                : p_indices              ,
                destination           : (TIndex*)writer.Indices,
                destinationSizeInBytes: data_size              ,
                sourceBytesToCopy     : index_data_size
            );
        }
        
        return new ReadOnlyMesh<TIndex>(writer, targetResource, device, physicalDevice);
    }
}

public class ReadOnlyMesh<TIndex> : ReadOnlyMesh
    where TIndex : unmanaged, IFormattable, IEquatable<TIndex>, IComparable<TIndex>
{
    /* Type validation   */
    static ReadOnlyMesh()
    {
        if (!IndexTypes.TryGetValue(typeof(TIndex), out IndexType index_type))
            throw new ArgumentException(
                $"Index type type must be {typeof(u8)}, {typeof(u16)} or {typeof(u32)}, " +
                $"but input type is {typeof(TIndex)} and therefore is invalid.");

        IndexType = index_type;
    }


    /* Handles           */
    private readonly vk.Device         _device        ;
    private readonly vk.PhysicalDevice _physicalDevice;
    private readonly SlimBuffer        _buffer        ; // vertex + index
    private readonly VulkanMemory      _memory        ;

    /* Memory accessors  */
    private readonly bool              _isMemoryOwned ; // should the mesh destroy the buffer + memory on Dispose?
    private          BufferSubresource _vertexResource;
    private          BufferSubresource _indexResource ;


    /* API               */
    public static IndexType IndexType      { get; }
    public        u32       VertexCount    { get; }
    public        u32       IndexCount     { get; }
    
    public        f64       BoundingSphere { get; }


    /* Constructors      */
    private ReadOnlyMesh(
        bool isMemoryOwned,
        ReadOnlySpan<GVertex> vertices, ReadOnlySpan<TIndex> indices, f64 boundingSphere,
        vk.Device? device = null, vk.PhysicalDevice? physicalDevice = null)
    {
        _isMemoryOwned = isMemoryOwned;

        _device         = device         ?? VK.Device            ;
        _physicalDevice = physicalDevice ?? VK.GPU.PhysicalDevice;

        if (vertices.IsEmpty) throw new ArgumentException("Vertices must be set.", nameof(vertices));

        VertexCount    = (u32)vertices.Length;
        IndexCount     = (u32) indices.Length;

        BoundingSphere = boundingSphere;
    }

    public unsafe ReadOnlyMesh(
        MeshWriter<TIndex> writer, BufferSubresource? targetResource = null, 
        vk.Device? device = null, vk.PhysicalDevice? physicalDevice = null)
    : this (
        isMemoryOwned : targetResource == null, 
        vertices      : new ReadOnlySpan<GVertex>((void*)writer.Vertices, (i32)writer.VertexCount), 
        indices       : new ReadOnlySpan<TIndex> ((void*)writer.Indices , (i32)writer.IndexCount ),
        boundingSphere: writer.BoundingSphere)
    {
        u64 vertex_data_size = (u64)Unsafe.SizeOf<GVertex>() * writer.VertexCount;
        u64 index_data_size  = (u64)Unsafe.SizeOf<TIndex>()  *  writer.IndexCount;
        u64 data_size        = vertex_data_size + index_data_size;
        
        u32 queue_family_index = 0;
        
        u64 vertex_offset = 0UL;
        u64 index_offset  = 0UL;
        if (!_isMemoryOwned) // just append to target resource
        {
            _buffer = targetResource!.Buffer;
            _buffer.GetMemoryRequirements(_device, out vk.MemoryRequirements buffer_requirements);

            u64 base_offset    = targetResource.Segment.Offset;
            u64 data_alignment = buffer_requirements.Alignment;
            
            vertex_offset      = base_offset;
            index_offset       = AMath.Align(vertex_offset + vertex_data_size, data_alignment);
        }
        else
        {
            Span<u32> queue_families = queue_family_index.AsSpan();
            
            const BufferUsageFlags FINAL_USAGE = BufferUsageFlags.VertexBuffer       |
                                                 BufferUsageFlags.IndexBuffer        |
                                                 BufferUsageFlags.TransferDestination;
            const vk.SharingMode FINAL_SHARING = vk.SharingMode.Exclusive;

            SlimBuffer dummy_buffer = new(
                device            : _device         ,
                size              : data_size       ,
                usage             : FINAL_USAGE     ,
                sharingMode       : FINAL_SHARING   ,
                queueFamilyIndices: queue_families
            );
            dummy_buffer.GetMemoryRequirements(_device, out vk.MemoryRequirements buffer_requirements);
            dummy_buffer.Destroy(_device);

            u64 data_alignment = buffer_requirements.Alignment;

            const u64 VERTEX_OFFSET = 0UL;
            index_offset            = AMath.Align(VERTEX_OFFSET + vertex_data_size, data_alignment);

            u64 buffer_data_size    = AMath.Align(index_offset + index_data_size, data_alignment);

            _buffer = new SlimBuffer(
                device            : _device         ,
                size              : buffer_data_size,
                usage             : FINAL_USAGE     ,
                sharingMode       : FINAL_SHARING   ,
                queueFamilyIndices: queue_families
            );

            u32 memory_type = _physicalDevice.FindMemoryType(
                typeFilter: buffer_requirements.MemoryTypeBits,
                properties: MemoryPropertyFlags.DeviceLocal
            );

            u64 memory_size = buffer_requirements.Size;

            _memory = new VulkanMemory(
                device         : _device    ,
                size           : memory_size,
                memoryTypeIndex: memory_type
            );

            _buffer.BindMemory(_memory.Whole);
        }
        
        CreateSubresources(
            vertexOffset: vertex_offset   ,
            vertexSize  : vertex_data_size,
            indexOffset : index_offset    ,
            indexSize   : index_data_size
        );

        SlimCommandPool pool = new(device: _device, queueFamilyIndex: queue_family_index);

        pool.AllocateCommandBuffer(device: _device, level: CommandBufferLevel.Primary, out SlimCommandBuffer cmd);
        
        vk.CommandBufferBeginInfo cmd_begin_info = new(
            flags: vk.CommandBufferUsageFlags.CommandBufferUsageOneTimeSubmitBit
        );

        VK.API.BeginCommandBuffer(cmd, in cmd_begin_info);
        {
            SlimBuffer destination_buffer = _buffer;

            vk.BufferCopy* copies = stackalloc vk.BufferCopy[2];
                
            copies[0] = new vk.BufferCopy(size: vertex_data_size, srcOffset: writer.VertexOffset, dstOffset: vertex_offset);
            copies[1] = new vk.BufferCopy(size: index_data_size , srcOffset: writer.IndexOffset,  dstOffset: index_offset );
            
            VK.API.CmdCopyBuffer(
                commandBuffer: cmd               ,
                srcBuffer    : writer.Buffer     ,
                dstBuffer    : destination_buffer,
                regionCount  : 2U, copies
            );
        }
        VK.API.EndCommandBuffer(cmd);

        SlimFence fence = new(device: _device, signaled: false);
        vk.SubmitInfo submit = new(commandBufferCount: 1U, pCommandBuffers: (vk.CommandBuffer*)&cmd);

        using (MutexLock<vk.Queue> queue = VK.Queue.Lock())
        {
            VK.API.QueueSubmit(
                queue      : queue.Data   ,
                submitCount: 1U, in submit,
                fence      : fence.Handle
            );
            fence.Wait(_device);
        }
        

        

        fence.         Destroy(_device);
        pool.          Destroy(_device);

        MakeReady();
    }


    public ReadOnlyMesh(
        ReadOnlySpan<GVertex> vertices, ReadOnlySpan<TIndex> indices, f64 boundingSphere, BufferSubresource targetResource,
        vk.Device? device = null, vk.PhysicalDevice? physicalDevice = null)
        : this(isMemoryOwned: true, vertices, indices, boundingSphere, device, physicalDevice)
    {
        _isMemoryOwned = false;

        _buffer = targetResource.Buffer;

        u64 vertex_data_size = (u64)Unsafe.SizeOf<GVertex>() * (u64)vertices.Length;
        u64 index_data_size  = (u64)Unsafe.SizeOf<TIndex>()  * (u64) indices.Length;

        _buffer.GetMemoryRequirements(_device, out vk.MemoryRequirements buffer_requirements);

        u64 base_offset = targetResource.Segment.Offset;

        u64 data_alignment = buffer_requirements.Alignment;

        u64 vertex_offset    = base_offset;
        u64 index_offset     = AMath.Align(vertex_offset + vertex_data_size, data_alignment);
        
        u64 buffer_data_size = AMath.Align(index_offset + index_data_size, data_alignment);

        CreateSubresources(
            vertexOffset: vertex_offset   ,
            vertexSize  : vertex_data_size,
            indexOffset : index_offset    ,
            indexSize   : index_data_size
        );
        
        CopyToDeviceBuffer(
            dataSize      : buffer_data_size,
            indexOffset   : index_offset    ,
            vertices      : vertices        ,
            indices       : indices         ,
            targetResource: targetResource
        );

        MakeReady();
    }

    public ReadOnlyMesh(
        ReadOnlySpan<GVertex> vertices, ReadOnlySpan<TIndex> indices, f64 boundingSphere,
        vk.Device? device = null, vk.PhysicalDevice? physicalDevice = null)
        : this(isMemoryOwned: true, vertices, indices, boundingSphere, device, physicalDevice)
    {
        u64 vertex_data_size = (u64)Unsafe.SizeOf<GVertex>() * (u64)vertices.Length;
        u64 index_data_size  = (u64)Unsafe.SizeOf<TIndex>()  * (u64) indices.Length;

        u64 data_size = vertex_data_size + index_data_size;

        const BufferUsageFlags FINAL_USAGE = BufferUsageFlags.VertexBuffer       |
                                             BufferUsageFlags.IndexBuffer        |
                                             BufferUsageFlags.TransferDestination;
        const vk.SharingMode FINAL_SHARING = vk.SharingMode.Exclusive;

        u32 main_queue_family_index = 0;
        Span<u32> queue_families = main_queue_family_index.AsSpan();

        SlimBuffer dummy_buffer = new(
            device            : _device         ,
            size              : data_size       ,
            usage             : FINAL_USAGE     ,
            sharingMode       : FINAL_SHARING   ,
            queueFamilyIndices: queue_families
        );

        dummy_buffer.GetMemoryRequirements(_device, out vk.MemoryRequirements buffer_requirements);

        dummy_buffer.Destroy(_device);

        u64 data_alignment = buffer_requirements.Alignment;

        const u64 VERTEX_OFFSET = 0UL;
        u64 index_offset        = AMath.Align(VERTEX_OFFSET + vertex_data_size, data_alignment);

        u64 buffer_data_size = AMath.Align(index_offset + index_data_size, data_alignment);

        _buffer = new SlimBuffer(
            device            : _device         ,
            size              : buffer_data_size,
            usage             : FINAL_USAGE     ,
            sharingMode       : FINAL_SHARING   ,
            queueFamilyIndices: queue_families
        );

        u32 memory_type = _physicalDevice.FindMemoryType(
            typeFilter: buffer_requirements.MemoryTypeBits,
            properties: MemoryPropertyFlags.DeviceLocal
        );

        u64 memory_size = buffer_requirements.Size;

        _memory = new VulkanMemory(
            device         : _device    ,
            size           : memory_size,
            memoryTypeIndex: memory_type
        );

        _buffer.BindMemory(_memory.Whole);

        CreateSubresources(
            vertexOffset: VERTEX_OFFSET   ,
            vertexSize  : vertex_data_size,
            indexOffset : index_offset    ,
            indexSize   : index_data_size
        );

        BufferSubresource whole_subresource = new(buffer: _buffer, segment: _memory.Whole);

        CopyToDeviceBuffer(
            dataSize      : memory_size      ,
            indexOffset   : index_offset     ,
            vertices      : vertices         ,
            indices       : indices          ,
            targetResource: whole_subresource
        );

        MakeReady();
    }

    private void CreateSubresources(
        u64 vertexOffset, u64 vertexSize,
        u64 indexOffset, u64 indexSize)
    {
        _vertexResource = new BufferSubresource(
            buffer : _buffer        ,
            segment: _memory.Segment(
            offset : vertexOffset   ,
            size   : vertexSize
        ));
        _indexResource = new BufferSubresource(
            buffer : _buffer        ,
            segment: _memory.Segment(
            offset : indexOffset    ,
            size   : indexSize
        ));
    }

    private void CopyToDeviceBuffer(
        u64 dataSize, u64 indexOffset,
        ReadOnlySpan<GVertex> vertices, ReadOnlySpan<TIndex> indices,
        BufferSubresource targetResource)
    {
        u64 vertex_data_size = (u64)Unsafe.SizeOf<GVertex>() * (u64)vertices.Length;
        u64 index_data_size =  (u64)Unsafe.SizeOf<TIndex>()  * (u64) indices.Length;

        u32 queue_family_index = 0;

        SlimBuffer staging_buffer = new(
            device            : _device                        ,
            size              : dataSize                       ,
            usage             : BufferUsageFlags.TransferSource,
            sharingMode       : vk.SharingMode.Exclusive       ,
            queueFamilyIndices: queue_family_index.AsSpan()
        );

        staging_buffer.GetMemoryRequirements(device: _device, out vk.MemoryRequirements staging_requirements);
        u64 staging_size = staging_requirements.Size;

        const MemoryPropertyFlags STAGING_MEMORY_FLAGS = MemoryPropertyFlags.HostVisible |
                                                         MemoryPropertyFlags.HostCoherent;

        u32 staging_memory_type = _physicalDevice.FindMemoryType(
            typeFilter: staging_requirements.MemoryTypeBits,
            properties: STAGING_MEMORY_FLAGS
        );

        using VulkanMemory staging_memory = new(
            device         : _device            ,
            size           : staging_size       ,
            memoryTypeIndex: staging_memory_type
        );

        staging_buffer.BindMemory(memory: staging_memory.Whole);

        using (MemoryMap<byte> map = staging_memory.Map())
        {
            unsafe
            {
                byte* p_map = map;

                fixed (GVertex* p_vertices = vertices) System.Buffer.MemoryCopy(
                    source                : p_vertices      ,
                    destination           : p_map           ,
                    destinationSizeInBytes: dataSize        ,
                    sourceBytesToCopy     : vertex_data_size
                );

                fixed (TIndex* p_indices = indices) System.Buffer.MemoryCopy(
                    source                : p_indices          ,
                    destination           : p_map + indexOffset,
                    destinationSizeInBytes: dataSize           ,
                    sourceBytesToCopy     : index_data_size
                );
            }
        }

        SlimCommandPool pool = new(device: _device, queueFamilyIndex: queue_family_index);

        pool.AllocateCommandBuffer(device: _device, level: CommandBufferLevel.Primary, out SlimCommandBuffer cmd);
        unsafe // todo: safe wrapper
        {
            vk.CommandBufferBeginInfo cmd_begin_info = new(
                flags: vk.CommandBufferUsageFlags.CommandBufferUsageOneTimeSubmitBit
            );

            VK.API.BeginCommandBuffer(cmd, in cmd_begin_info);
            {
                u64 destination_offset = targetResource.Segment.Offset;
                SlimBuffer destination_buffer = targetResource.Buffer;

                vk.BufferCopy copy = new(size: dataSize, dstOffset: destination_offset);
                VK.API.CmdCopyBuffer(
                    commandBuffer: cmd               ,
                    srcBuffer    : staging_buffer    ,
                    dstBuffer    : destination_buffer,
                    regionCount  : 1U, in copy
                );
            }
            VK.API.EndCommandBuffer(cmd);
        }

        SlimFence fence = new(device: _device, signaled: false);
        unsafe
        {
            vk.SubmitInfo submit = new(commandBufferCount: 1U, pCommandBuffers: (vk.CommandBuffer*)&cmd);

            using (MutexLock<vk.Queue> queue = VK.Queue.Lock())
            {
                VK.API.QueueSubmit(
                    queue      : queue.Data   ,
                    submitCount: 1U, in submit,
                    fence      : fence.Handle
                );
            }
            fence.Wait(_device);
        }

        

        fence.         Destroy(_device);
        pool.          Destroy(_device);
        staging_buffer.Destroy(_device);
    }
    
    public override void Delete()
    {
        base.Delete();

        if (_isMemoryOwned)
        {
            _buffer.Destroy(_device);
            _memory.Dispose(       );
        }
    }
    
    public void CmdBindBuffers(SlimCommandBuffer cmd)
    {
        VK.API.CmdBindVertexBuffers(
            cmd, 
            firstBinding: 0U                            ,
            bindingCount: 1U                            , 
            pBuffers    : in _buffer.Handle             ,
            pOffsets    : _vertexResource.Segment.Offset
        );
        VK.API.CmdBindIndexBuffer(
            cmd                                , 
            buffer: _buffer.Handle               ,
            offset: _indexResource.Segment.Offset, 
            indexType: IndexType.ToVk()
        );
    }
}