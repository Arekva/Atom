using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;

namespace Atom.Engine;

public struct PipelineLayout
{
    private static ConcurrentDictionary<SlimPipelineLayout, Device> _pipelineLayouts = new();

    public SlimPipelineLayout Handle;
    
    public Device Device => _pipelineLayouts[Handle];
    
#region Creation & Non-API stuff

    public PipelineLayout( 
        ReadOnlySpan<SlimDescriptorSetLayout> setLayouts, 
        ReadOnlySpan<PushConstantRange> pushConstantRanges,
        Device? device = null)
    {
        Device used_device = device ?? VK.Device;

        Handle = new SlimPipelineLayout(used_device, setLayouts, pushConstantRanges);

        _pipelineLayouts.TryAdd(Handle, used_device);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Handle.GetHashCode();

#endregion

#region Standard API Proxying 
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Destroy()
    {
        if(_pipelineLayouts.TryRemove(Handle, out Device device))
        {
            Handle.Destroy(device);
        }
    }

#endregion
}