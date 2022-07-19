using System.Runtime.CompilerServices;
using Atom.Engine.Vulkan;

namespace Atom.Engine;

public class VulkanMemory : IDisposable
{
    private bool _isMapped;
    
    private SlimDeviceMemory _handle;
    public ref SlimDeviceMemory Handle => ref _handle;
    
    public Silk.NET.Vulkan.Device Device { get; }

    public bool IsMapped
    {
        get => Volatile.Read(ref _isMapped);
        private set => Volatile.Write(ref _isMapped, value);
    }
    
    public ulong Size { get; }

    public MemorySegment Whole { get; }


    public VulkanMemory(u64 size, u32 memoryTypeIndex) : this(VK.Device, size, memoryTypeIndex) { }

    // todo: user friendly API, just pass a memorytype object/struct and get its type index
    public VulkanMemory(vk.Device device, u64 size, u32 memoryTypeIndex)
    {
        Device = device;
        
        _handle = new SlimDeviceMemory(
            device         : device         ,
            allocationSize : size           ,
            memoryTypeIndex: memoryTypeIndex
        );

        Size = size;

        Whole = new MemorySegment(this, offset: 0UL, size);
    }

    public u64 GetCapacity<T>() => Size / (u64)Unsafe.SizeOf<T>();
    
    
    
    public MemorySegment Segment(ulong offset, ulong size) => new (this, offset, size);

    public MemorySegment Segment<T>(ulong start, ulong length)
    {
        u64 sizeofT = (u64)Unsafe.SizeOf<T>();
        
        u64 offset = start * sizeofT;
        u64 size = length * sizeofT;
        
        return new MemorySegment(this, offset, size);
    }
    

    public unsafe MemoryMap<T> Map<T>(MemorySegment segment) where T : unmanaged
    {
        if (IsMapped)
        {
            throw new MemoryMapException("This memory is already mapped on the host.");
        }

        IsMapped = true;
        
        void* handle = null;
        
        vk.Result result = VK.API.MapMemory(
            device: Device          , 
            memory: _handle         , 
            offset: segment.Offset  ,
            size  : segment.Size    ,
            flags : 0U              ,
            ppData: ref handle
        );
        
        if (result != vk.Result.Success)
        {
            // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
            throw result switch
            {
                vk.Result.ErrorOutOfHostMemory => new OutOfHostMemoryException("Host ran out of memory."),
                vk.Result.ErrorOutOfDeviceMemory => new OutOfDeviceMemoryException("Device ran out of memory."),
                vk.Result.ErrorMemoryMapFailed => new MemoryMapFailedException($"Memory mapping failed."),
                vk.Result.ErrorValidationFailedExt => new ValidationFailedException("Incorrect memory mapping, more information in the Vulkan output."),
                _ => throw new NotImplementedException($"Unexpected result: {result}")
            };
        }
        

        return new MemoryMap<T>((nint)handle, segment);
    }

    public MemoryMap<T> Map<T>(ulong start, ulong length) where T : unmanaged
    {
        ulong sizeofT = (ulong)Unsafe.SizeOf<T>();

        ulong offset = start * sizeofT;
        ulong size = length * sizeofT;

        return Map<T>(new MemorySegment(this, offset, size));
    }
    
    public MemoryMap<T> Map<T>() where T : unmanaged => Map<T>(this.Whole);

    public MemoryMap<byte> Map(ulong offset, ulong size) => Map(new MemorySegment(this, offset, size));

    public MemoryMap<byte> Map(MemorySegment segment) => Map<byte>(segment);

    public MemoryMap<byte> Map() => Map(Whole);

    public void Unmap(IMemoryMap map) // current map must be owned by the caller 
    {
        if (map == null)
        {
            throw new ArgumentNullException(nameof(map));
        }

        if (!IsMapped)
        {
            throw new Exception("This memory is not currently mapped.");
        }

        if (map.Segment.Memory != this)
        {
            throw new ArgumentException("The provided memory map is not mapping this memory.", nameof(map));
        }

        IsMapped = false;
        
        VK.API.UnmapMemory(Device, _handle);
    }

    public void Delete()
    {
        if (IsMapped) // shouldn't happen, because a memory map implicitly means a memory segment object has been
            // created, but we never know.
        {
            throw new MemoryMapException("Memory is still mapped. Please unmap please unmap your memory before " +
                                         "disposing ");
        }
        
        vk.VkOverloads.FreeMemory(VK.API, Device, _handle, ReadOnlySpan<vk.AllocationCallbacks>.Empty);
        
        GC.SuppressFinalize(this);
    }

    public void Dispose() => Delete();

    ~VulkanMemory() => Dispose();

    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator vk.DeviceMemory(in VulkanMemory memory) => memory._handle;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator u64(in VulkanMemory memory) => 
        Unsafe.As<SlimDeviceMemory, u64>(ref memory._handle);
}