using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;

namespace Atom.Engine.Vulkan;

public struct DescriptorSetLayout
{
    private static ConcurrentDictionary<SlimDescriptorSetLayout, Device> _descriptorSetLayouts = new();

    public SlimDescriptorSetLayout Handle;
    
    public Device Device => _descriptorSetLayouts[Handle];
    
#region Creation & Non-API stuff

    public DescriptorSetLayout( 
        ReadOnlySpan<DescriptorSetLayoutBinding> bindings, 
        DescriptorSetLayoutCreateFlags flags = 0,
        Device? device = null)
    {
        Device used_device = device ?? VK.Device;

        Handle = new SlimDescriptorSetLayout(used_device, bindings, flags);

        _descriptorSetLayouts.TryAdd(Handle, used_device);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Handle.GetHashCode();

#endregion

#region Standard API Proxying 
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Destroy()
    {
        if(_descriptorSetLayouts.TryRemove(Handle, out Device device))
        {
            Handle.Destroy(device);
        }
    }

#endregion
}