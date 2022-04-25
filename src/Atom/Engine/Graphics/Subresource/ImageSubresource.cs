using System.Runtime.CompilerServices;

using Atom.Engine.Vulkan;



namespace Atom.Engine;



public class ImageSubresource : ISubresource, IDisposable
{
    public readonly Image         Image;
    public readonly SlimImageView View ;


    public readonly ImageViewType    Type   ;
    public readonly ImageFormat      Format ;
    public readonly ComponentMapping Mapping;
    public readonly ImageAspectFlags Aspects;

    public readonly u32 BaseMip   ;
    public readonly u32 MipCount  ;
    public readonly u32 BaseArray ;
    public readonly u32 ArrayCount;
    
    
    
    public vk.Device Device => Image.Device;
    
    public Range MipRange => new((i32)BaseMip, (i32)MipCount);
    
    public Range ArrayRange => new((i32)BaseArray, (i32)ArrayCount);

    
    
    public ImageSubresource(Image image, 
        ImageViewType viewType, ImageFormat format,
        ComponentMapping components, ImageAspectFlags aspectMask,
        Range mipLevels, Range arrayLayers
        )
    {
        Image   = image     ;
        Type    = viewType  ;
        Format  = format    ;
        Mapping = components;
        Aspects = aspectMask;
        
        u32 mip_start_val    = (u32)mipLevels.Start.Value  ;
        u32 mip_end_val      = (u32)mipLevels.End  .Value  ;
        u32 mip_count_base   = image.MipLevels             ;
        u32 mip_start        = mipLevels.Start.IsFromEnd   ? unchecked(mip_count_base   - mip_start_val)   : mip_start_val  ;
        u32 mip_end          = mipLevels.End  .IsFromEnd   ? unchecked(mip_count_base   - mip_end_val  )   : mip_end_val    ;
        u32 mip_count        = unchecked(mip_end - mip_start);
        
        u32 array_start_val  = (u32)arrayLayers.Start.Value;
        u32 array_end_val    = (u32)arrayLayers.End  .Value;
        u32 array_count_base = image.ArrayLayers           ;
        u32 array_start      = arrayLayers.Start.IsFromEnd ? unchecked(array_count_base - array_start_val) : array_start_val;
        u32 array_end        = arrayLayers.End  .IsFromEnd ? unchecked(array_count_base - array_end_val  ) : array_end_val  ;
        u32 array_count      = unchecked(array_end - array_start);
        
        BaseMip    = mip_start       ;
        MipCount   = mip_count_base  ;

        BaseArray  = array_start     ;
        ArrayCount = array_count_base;

        View = new SlimImageView(
            device    : image.Device, 
            image     : image      , 
            viewType  : viewType    ,
            format    : format      , 
            components: components  ,
            subresourceRange  : new vk.ImageSubresourceRange(
                aspectMask    : (vk.ImageAspectFlags) aspectMask,
                baseMipLevel  : mip_start                       ,
                levelCount    : mip_count                       ,
                baseArrayLayer: array_start                     ,
                layerCount    : array_count
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