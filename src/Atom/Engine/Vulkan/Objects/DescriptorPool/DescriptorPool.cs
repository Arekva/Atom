using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;

namespace Atom.Engine.Vulkan;

public struct DescriptorPool
{
    private static ConcurrentDictionary<SlimDescriptorPool, Device> _descriptorPools = new();

    public SlimDescriptorPool Handle;
    
    public Device Device => _descriptorPools[Handle];
    
#region Creation & Non-API stuff

    public DescriptorPool( 
        uint maxSets, 
        ReadOnlySpan<DescriptorPoolSize> poolSizes,
        DescriptorPoolCreateFlags flags = 0,
        Device? device = null)
    {
        Device used_device = device ?? VK.Device;

        Handle = new SlimDescriptorPool(used_device, maxSets, poolSizes, flags);

        _descriptorPools.TryAdd(Handle, used_device);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Handle.GetHashCode();

#endregion

#region Standard API Proxying 
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Destroy()
    {
        if(_descriptorPools.TryRemove(Handle, out Device device))
        {
            Handle.Destroy(device);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Result Reset() => Handle.Reset(_descriptorPools[this]);

#endregion
}