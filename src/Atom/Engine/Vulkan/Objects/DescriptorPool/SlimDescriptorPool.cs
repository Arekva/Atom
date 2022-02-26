using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;

namespace Atom.Engine.Vulkan;

public struct SlimDescriptorPool
{
    public vk.DescriptorPool Handle;
    
#region Creation & Non-API stuff

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe SlimDescriptorPool(Device device, 
        uint maxSets, 
        ReadOnlySpan<DescriptorPoolSize> poolSizes,
        DescriptorPoolCreateFlags flags = 0)
    {
        fixed (DescriptorPoolSize* p_pool_sizes = poolSizes)
        {
            DescriptorPoolCreateInfo create_info = new(
                flags: flags,
                maxSets: maxSets,
                poolSizeCount: (uint)poolSizes.Length,
                pPoolSizes: p_pool_sizes
            );

            Result result = VK.API.CreateDescriptorPool(device, in create_info, null, out Handle);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Handle.GetHashCode();
    
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator DescriptorPool(in SlimDescriptorPool descriptorPool)
        => Unsafe.As<SlimDescriptorPool, DescriptorPool>(ref Unsafe.AsRef(in descriptorPool));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SlimDescriptorPool(in DescriptorPool descriptorPool)
        => Unsafe.As<DescriptorPool, SlimDescriptorPool>(ref Unsafe.AsRef(in descriptorPool));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Silk.NET.Vulkan.DescriptorPool(in SlimDescriptorPool descriptorPool)
        => Unsafe.As<SlimDescriptorPool, Silk.NET.Vulkan.DescriptorPool>(ref Unsafe.AsRef(in descriptorPool));

#endregion
    
#region Standard API Proxying 

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Destroy(Device device) => VK.API.DestroyDescriptorPool(device, Handle, ReadOnlySpan<AllocationCallbacks>.Empty);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Result Reset(Device device) => VK.API.ResetDescriptorPool(device, this, 0);

#endregion

#region User defined



#endregion
}