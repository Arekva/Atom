using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;

namespace Atom.Engine.Vulkan;

public struct SlimImageView
{
    public vk.ImageView Handle;
    
#region Creation & Non-API stuff

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe SlimImageView(
        Device device, SlimImage image,
        ImageViewType viewType, Format format,
        ComponentMapping components,
        vk.ImageSubresourceRange subresourceRange,
        ImageViewCreateFlags flags = 0)
    {
        ImageViewCreateInfo create_info = new(
          flags: flags,
          image: image,
          viewType: viewType,
          format: format,
          components: Unsafe.As<ComponentMapping, vk.ComponentMapping>(ref components),
          subresourceRange: subresourceRange
        );
        
        Result result = VK.API.CreateImageView(device, in create_info, null, out Handle);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Handle.GetHashCode();
    
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator ImageView(in SlimImageView view)
        => Unsafe.As<SlimImageView, ImageView>(ref Unsafe.AsRef(in view));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SlimImageView(in ImageView view)
        => Unsafe.As<ImageView, SlimImageView>(ref Unsafe.AsRef(in view));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Silk.NET.Vulkan.ImageView(in SlimImageView view)
        => Unsafe.As<SlimImageView, Silk.NET.Vulkan.ImageView>(ref Unsafe.AsRef(in view));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SlimImageView(in Silk.NET.Vulkan.ImageView view)
        => Unsafe.As<Silk.NET.Vulkan.ImageView, SlimImageView>(ref Unsafe.AsRef(in view));

#endregion
    
#region Standard API Proxying 

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Destroy(Device device) => VK.API.DestroyImageView(device, Handle, ReadOnlySpan<AllocationCallbacks>.Empty);

#endregion

#region User defined


    
#endregion
}