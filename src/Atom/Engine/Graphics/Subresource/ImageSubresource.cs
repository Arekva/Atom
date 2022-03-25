using System.Runtime.CompilerServices;
using Atom.Engine.Vulkan;

namespace Atom.Engine;

public class ImageSubresource : ISubresource, IDisposable
{
    public vk.Device Device => Image.Device;
    
    public readonly IImage       Image;
    public readonly SlimImageView View;

    public ImageSubresource(IImage image, 
        vk.ImageViewType viewType, ImageFormat format,
        ComponentMapping components, ImageAspectFlags aspectMask,
        Range mipLevels, Range arrayLayers
        )
    {
        Image = image;
        
        u32 mip_start_val   = (u32)mipLevels.Start.Value  ;
        u32 mip_end_val     = (u32)mipLevels.End.Value    ;
        u32 mip_count       = image.MipLevels             ;
        u32 mip_start       = mipLevels.Start.IsFromEnd   ? mip_count   - mip_start_val   : mip_start_val  ;
        u32 mip_end         = mipLevels.Start.IsFromEnd   ? mip_count   - mip_end_val     : mip_end_val    ;
        
        u32 array_start_val = (u32)arrayLayers.Start.Value;
        u32 array_end_val   = (u32)arrayLayers.End.Value  ;
        u32 array_count     = image.ArrayLayers           ;
        u32 array_start     = arrayLayers.Start.IsFromEnd ? array_count - array_start_val : array_start_val;
        u32 array_end       = arrayLayers.Start.IsFromEnd ? array_count - array_end_val   : array_end_val  ;

        vk.Device device = image.Device;
        View = new SlimImageView(
            device: device, 
            image: image.Handle, 
            viewType: viewType,
            format: (vk.Format)format, 
            components: components,
            subresourceRange: new vk.ImageSubresourceRange(
                aspectMask    : (vk.ImageAspectFlags) aspectMask,
                baseMipLevel  : mip_start                       ,
                levelCount    : mip_end - mip_start             ,
                baseArrayLayer: array_start                     ,
                layerCount    : array_end - array_start
            )
        );
    }

    public void Delete()
    {
        View.Destroy(Device);
        
        GC.SuppressFinalize(this);
    }
    
    public void Dispose() => Delete();

    ~ImageSubresource() => Dispose();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SlimImageView(ImageSubresource @this) => @this.View;
}