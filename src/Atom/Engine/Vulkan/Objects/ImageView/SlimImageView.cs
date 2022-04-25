using System.Runtime.CompilerServices;

namespace Atom.Engine.Vulkan;

public struct SlimImageView
{
    public vk.ImageView Handle;
    
#region Creation & Non-API stuff

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe SlimImageView(
        vk.Device device, SlimImage image,
        ImageViewType viewType, ImageFormat format,
        ComponentMapping components,
        vk.ImageSubresourceRange subresourceRange,
        vk.ImageViewCreateFlags flags = 0)
    {
        vk.ImageViewCreateInfo create_info = new(
          flags           : flags,
          image           : image,
          viewType        : viewType.ToVk(),
          format          : format.ToVk(),
          components      : Unsafe.As<ComponentMapping, vk.ComponentMapping>(ref components),
          subresourceRange: subresourceRange
        );
        
        vk.Result result = VK.API.CreateImageView(device, in create_info, null, out Handle);
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
    public readonly unsafe void Destroy(vk.Device device) => VK.API.DestroyImageView(device, Handle, null);

#endregion

#region User defined


    
#endregion
}