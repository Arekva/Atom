using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;

namespace Atom.Engine.Vulkan;

public struct DeviceMemory
{
    private static ConcurrentDictionary<SlimDeviceMemory, Device> _memories = new();

    public SlimDeviceMemory Handle;
    
    public Device Device => _memories[Handle];
    
#region Creation & Non-API stuff

    public DeviceMemory(ulong allocationSize, uint memoryTypeIndex, Device? device = null)
    {
        Device used_device = device ?? VK.Device;

        Handle = new SlimDeviceMemory(used_device, allocationSize, memoryTypeIndex);

        _memories.TryAdd(Handle, used_device);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Handle.GetHashCode();

#endregion

#region Standard API Proxying 
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Free()
    {
        if(_memories.TryRemove(Handle, out Device device))
        {
            Handle.Free(device);
        }
    }

#endregion
}