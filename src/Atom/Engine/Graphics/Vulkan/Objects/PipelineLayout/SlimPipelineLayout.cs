using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;

namespace Atom.Engine;

public struct SlimPipelineLayout
{
    public vk.PipelineLayout Handle;
    
#region Creation & Non-API stuff

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe SlimPipelineLayout(Device device, 
        ReadOnlySpan<SlimDescriptorSetLayout> setLayouts, 
        ReadOnlySpan<PushConstantRange> pushConstantRanges)
    {
        fixed (SlimDescriptorSetLayout* p_set_layouts = setLayouts)
        fixed (PushConstantRange* p_push_constant_range = pushConstantRanges)
        {
            PipelineLayoutCreateInfo create_info = new(
                setLayoutCount: (uint)setLayouts.Length,
                pSetLayouts: (vk.DescriptorSetLayout*)p_set_layouts,
                pushConstantRangeCount: (uint)pushConstantRanges.Length,
                pPushConstantRanges: p_push_constant_range
            );
            
            Result result = VK.API.CreatePipelineLayout(device, in create_info, null, out Handle);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Handle.GetHashCode();
    
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator PipelineLayout(in SlimPipelineLayout fence)
        => Unsafe.As<SlimPipelineLayout, PipelineLayout>(ref Unsafe.AsRef(in fence));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SlimPipelineLayout(in PipelineLayout fence)
        => Unsafe.As<PipelineLayout, SlimPipelineLayout>(ref Unsafe.AsRef(in fence));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Silk.NET.Vulkan.PipelineLayout(in SlimPipelineLayout fence)
        => Unsafe.As<SlimPipelineLayout, Silk.NET.Vulkan.PipelineLayout>(ref Unsafe.AsRef(in fence));

#endregion
    
#region Standard API Proxying 

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Destroy(Device device) => VK.API.DestroyPipelineLayout(device, Handle, ReadOnlySpan<AllocationCallbacks>.Empty);

#endregion

#region User defined


    
#endregion
}