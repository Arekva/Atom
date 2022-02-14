using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;

namespace Atom.Engine;

public struct SlimDescriptorSetLayout
{
    public vk.DescriptorSetLayout Handle;
    
#region Creation & Non-API stuff

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe SlimDescriptorSetLayout(Device device, 
        ReadOnlySpan<DescriptorSetLayoutBinding> bindings, 
        DescriptorSetLayoutCreateFlags flags = 0)
    {
        fixed (DescriptorSetLayoutBinding* p_bindings = bindings)
        {
            DescriptorSetLayoutCreateInfo create_info = new(
                flags: flags,
                bindingCount: (uint)bindings.Length,
                pBindings: p_bindings
            );
            
            Result result = VK.API.CreateDescriptorSetLayout(device, in create_info, null, out Handle);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Handle.GetHashCode();
    
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator DescriptorSetLayout(in SlimDescriptorSetLayout descriptorSetLayout)
        => Unsafe.As<SlimDescriptorSetLayout, DescriptorSetLayout>(ref Unsafe.AsRef(in descriptorSetLayout));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SlimDescriptorSetLayout(in DescriptorSetLayout descriptorSetLayout)
        => Unsafe.As<DescriptorSetLayout, SlimDescriptorSetLayout>(ref Unsafe.AsRef(in descriptorSetLayout));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Silk.NET.Vulkan.DescriptorSetLayout(in SlimDescriptorSetLayout descriptorSetLayout)
        => Unsafe.As<SlimDescriptorSetLayout, Silk.NET.Vulkan.DescriptorSetLayout>(ref Unsafe.AsRef(in descriptorSetLayout));

#endregion
    
#region Standard API Proxying 

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Destroy(Device device) => VK.API.DestroyDescriptorSetLayout(device, Handle, ReadOnlySpan<AllocationCallbacks>.Empty);

#endregion

#region User defined


    
#endregion
}