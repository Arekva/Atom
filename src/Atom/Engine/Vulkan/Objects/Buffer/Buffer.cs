using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;

namespace Atom.Engine.Vulkan;

public struct Buffer
{ 
    private static ConcurrentDictionary<SlimBuffer, Device> _buffers = new();
    
    public SlimBuffer Handle;

    public Device Device => _buffers[Handle];
    
#region Creation & Non-API stuff
    
    public Buffer(
        ulong size, 
        BufferUsageFlags usage, 
        SharingMode sharingMode,
        ReadOnlySpan<uint> queueFamilyIndices,
        BufferCreateFlags flags = 0,
        Device? device = null)
    {
        Device used_device = device ?? VK.Device;

        Handle = new SlimBuffer(used_device, size, usage, sharingMode, queueFamilyIndices, flags);

        _buffers.TryAdd(Handle, used_device);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Handle.GetHashCode();
    
#endregion
    
#region Standard API Proxying
    
#endregion
    
#region User defined
    
#endregion
}