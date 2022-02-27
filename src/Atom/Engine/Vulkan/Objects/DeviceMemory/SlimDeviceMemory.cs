using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;

namespace Atom.Engine.Vulkan;

public struct SlimDeviceMemory
{
    public vk.DeviceMemory Handle;
    
#region Creation & Non-API stuff

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe SlimDeviceMemory(Device device, ulong allocationSize, uint memoryTypeIndex)
    {
        MemoryAllocateInfo create_info = new(
            allocationSize: allocationSize,
            memoryTypeIndex: memoryTypeIndex
        );
            
        Result result = VK.API.AllocateMemory(device, in create_info, null, out Handle);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Handle.GetHashCode();
    
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator DeviceMemory(in SlimDeviceMemory memory)
        => Unsafe.As<SlimDeviceMemory, DeviceMemory>(ref Unsafe.AsRef(in memory));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SlimDeviceMemory(in DeviceMemory memory)
        => Unsafe.As<DeviceMemory, SlimDeviceMemory>(ref Unsafe.AsRef(in memory));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Silk.NET.Vulkan.DeviceMemory(in SlimDeviceMemory memory)
        => Unsafe.As<SlimDeviceMemory, Silk.NET.Vulkan.DeviceMemory>(ref Unsafe.AsRef(in memory));

#endregion
    
#region Standard API Proxying 

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Free(Device device) => VK.API.FreeMemory(device, Handle, ReadOnlySpan<AllocationCallbacks>.Empty);

#endregion

#region User defined


    
#endregion
}