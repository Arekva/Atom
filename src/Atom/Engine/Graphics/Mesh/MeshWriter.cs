using System.Runtime.CompilerServices;
using Atom.Engine.Vulkan;
using Silk.NET.Vulkan;
using BufferUsageFlags = Atom.Engine.Vulkan.BufferUsageFlags;
using MemoryPropertyFlags = Atom.Engine.Vulkan.MemoryPropertyFlags;

namespace Atom.Engine;

public class MeshWriter<TVertex, TIndex> : IDisposable 
    where TVertex : unmanaged
    where TIndex  : unmanaged, IFormattable, IEquatable<TIndex>, IComparable<TIndex>
{
    private readonly vk.Device         _device        ;
    private readonly vk.PhysicalDevice _physicalDevice;
    private readonly VulkanMemory      _memory        ;
    private readonly MemoryMap<u8>     _map           ;
    
    private readonly u64               _vertexSize    ;
    private readonly u64               _indicesSize   ;
    private readonly u64               _indicesOffset ;
    
    
    
    public readonly nint      Vertices;
    public readonly u64    VertexCount;
    public readonly u64   VertexOffset;
    
    public readonly nint       Indices;
    public readonly u64     IndexCount;
    public readonly u64    IndexOffset;

    public readonly f64 BoundingSphere;
    
    public readonly SlimBuffer  Buffer;


    public MeshWriter(
        u64 vertexCount, u64 indexCount, f64 boundingSphere = 1.0D,
        vk.Device? device = null, vk.PhysicalDevice? physicalDevice = null)
    {
        _device         = device         ?? VK.Device            ;
        _physicalDevice = physicalDevice ?? VK.GPU.PhysicalDevice;

        VertexCount = vertexCount;
        IndexCount  =  indexCount;

        BoundingSphere = boundingSphere;
        
        u64 vertex_data_size = _vertexSize  = (u64)Unsafe.SizeOf<TVertex>() * vertexCount;
        u64 index_data_size  = _indicesSize = (u64)Unsafe.SizeOf<TIndex>()  *  indexCount;
        u64 data_size        = vertex_data_size + index_data_size;

        _indicesOffset = vertex_data_size;
        
        u32 queue_family_index = 0;
        
        Buffer = new SlimBuffer(
            device            : _device                        ,
            size              : data_size                      ,
            usage             : BufferUsageFlags.TransferSource,
            sharingMode       : vk.SharingMode.Exclusive       ,
            queueFamilyIndices: queue_family_index.AsSpan()
        );
        
        Buffer.GetMemoryRequirements(device: _device, out vk.MemoryRequirements requirements);
        u64 buffer_size = requirements.Size;
        
        const MemoryPropertyFlags STAGING_MEMORY_FLAGS = MemoryPropertyFlags.HostVisible |
                                                         MemoryPropertyFlags.HostCoherent;
        
        u32 staging_memory_type = _physicalDevice.FindMemoryType(
            typeFilter: requirements.MemoryTypeBits,
            properties: STAGING_MEMORY_FLAGS
        );

        _memory = new VulkanMemory(
            device         : _device            ,
            size           : buffer_size        ,
            memoryTypeIndex: staging_memory_type
        );

        Buffer.BindMemory(_memory.Whole);

        _map = _memory.Map<u8>(start: 0UL, length: data_size);
        
        Vertices = _map.Handle;
        VertexOffset = 0UL;
        
        Indices  = _map.Handle + (nint)vertex_data_size;
        IndexOffset = vertex_data_size;
    }

    public void Dispose()
    {
        _map   .Dispose(       );
        Buffer .Destroy(_device);
        _memory.Dispose(       );

        GC.SuppressFinalize(this);
    }

    ~MeshWriter() => Dispose();
}

public class MeshWriter<TIndex> : MeshWriter<GVertex, TIndex>
    where TIndex : unmanaged, IFormattable, IEquatable<TIndex>, IComparable<TIndex>
{
    public MeshWriter(u64 vertexCount, u64 indexCount, f64 boundingSphere = 1.0D, 
        Device? device = null, PhysicalDevice? physicalDevice = null) 
        : base(vertexCount, indexCount, boundingSphere, device, physicalDevice)
    {
    }
}

public class MeshWriter : MeshWriter<u32>
{
    public MeshWriter(u64 vertexCount, u64 indexCount, f64 boundingSphere = 1.0D, 
        Device? device = null, PhysicalDevice? physicalDevice = null) 
        : base(vertexCount, indexCount, boundingSphere, device, physicalDevice)
    {
    }
}