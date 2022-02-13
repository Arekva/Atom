using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;

namespace Atom.Engine;

public class DeviceMemory : IDisposable
{
    private bool _isMapped;
    
    private vk.DeviceMemory _handle;
    public ref vk.DeviceMemory Handle => ref _handle;
    
    public Silk.NET.Vulkan.Device Device { get; }

    public bool IsMapped
    {
        get => Volatile.Read(ref _isMapped);
        private set => Volatile.Write(ref _isMapped, value);
    }
    
    public ulong Size { get; }

    public MemorySegment Whole { get; }
    
    

    // todo: user friendly API, just pass a memorytype object/struct and get its type index
    public unsafe DeviceMemory(Device device, ulong size, uint memoryTypeIndex)
    {
        Device = device;
        
        MemoryAllocateInfo mem = new (allocationSize: size, memoryTypeIndex: memoryTypeIndex);
        VK.API.AllocateMemory(
            device,
            pAllocateInfo: in mem, 
            pAllocator: null, 
            out _handle
        );
        
        Size = size;

        Whole = new MemorySegment(this, offset: 0UL, size);
    }

    public ulong GetCapacity<T>() => Size / (ulong)Unsafe.SizeOf<T>();
    
    
    
    public MemorySegment Segment(ulong offset, ulong size) => new (this, offset, size);

    public MemorySegment Segment<T>(ulong start, ulong length)
    {
        ulong sizeofT = (ulong)Unsafe.SizeOf<T>();
        
        ulong offset = start * sizeofT;
        ulong size = length * sizeofT;
        
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
        
        Result result = VK.API.MapMemory(
            Device, 
            _handle, 
            segment.Offset,
            segment.Size,
            flags: 0U,
            ref handle
        );
        
        if (result != Result.Success)
        {
            // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
            throw result switch
            {
                Result.ErrorOutOfHostMemory => new OutOfHostMemoryException("Host ran out of memory."),
                Result.ErrorOutOfDeviceMemory => new OutOfDeviceMemoryException("Device ran out of memory."),
                Result.ErrorMemoryMapFailed => new MemoryMapFailedException($"Memory mapping failed."),
                Result.ErrorValidationFailedExt => new ValidationFailedException("Incorrect memory mapping, more information in the Vulkan output."),
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

    public void Dispose()
    {
        if (IsMapped) // shouldn't happen, because a memory map implicitly means a memory segment object has been
                      // created, but we never know.
        {
            throw new MemoryMapException("Memory is still mapped. Please unmap please unmap your memory before " +
                                           "disposing ");
        }
        
        VK.API.FreeMemory(Device, _handle, ReadOnlySpan<AllocationCallbacks>.Empty);
        
        GC.SuppressFinalize(this);
    }

    ~DeviceMemory() => Dispose();

    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator vk.DeviceMemory(in DeviceMemory memory) => memory._handle;
}