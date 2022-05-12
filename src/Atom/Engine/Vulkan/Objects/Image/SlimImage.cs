using System.Runtime.CompilerServices;

namespace Atom.Engine.Vulkan;

public struct SlimImage
{
    public vk.Image Handle;
    
#region Creation & Non-API stuff

    public SlimImage()
    {
        Handle = new vk.Image(handle: 0UL);
        Log.Warning("Null SlimImage is being created.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe SlimImage(
        vk.Device device, 
        vk.ImageType type, ImageFormat format, vk.Extent3D extent,
        u32 mipLevels, u32 arrayLayers,
        vk.SampleCountFlags samples,
        vk.ImageTiling tiling, ImageUsageFlags usage,
        vk.SharingMode sharingMode, ReadOnlySpan<u32> queueFamilyIndices,
        vk.ImageLayout initialLayout,
        vk.ImageCreateFlags flags = 0)
    {
        fixed (u32* p_queues = queueFamilyIndices)
        {
            vk.ImageCreateInfo create_info = new(
                imageType            : type                           ,
                format               : format.ToVk()                  ,
                extent               : extent                         ,
                mipLevels            : mipLevels                      ,
                arrayLayers          : arrayLayers                    ,
                samples              : samples                        ,
                tiling               : tiling                         ,
                usage                : usage.ToVk()                   ,
                sharingMode          : sharingMode                    ,
                queueFamilyIndexCount: (u32) queueFamilyIndices.Length,
                pQueueFamilyIndices  : p_queues                       ,
                initialLayout        : initialLayout                  ,
                flags                : flags
            );

            vk.Result result = VK.API.CreateImage(device, in create_info, null, out Handle);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Handle.GetHashCode();
    
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Image(in SlimImage image)
        => Unsafe.As<SlimImage, Image>(ref Unsafe.AsRef(in image));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SlimImage(in Image image)
        => Unsafe.As<Image, SlimImage>(ref Unsafe.AsRef(in image));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Silk.NET.Vulkan.Image(in SlimImage image)
        => Unsafe.As<SlimImage, Silk.NET.Vulkan.Image>(ref Unsafe.AsRef(in image));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SlimImage(in Silk.NET.Vulkan.Image image)
        => Unsafe.As<Silk.NET.Vulkan.Image, SlimImage>(ref Unsafe.AsRef(in image));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator u64(in SlimImage image)
        => Unsafe.As<SlimImage, u64>(ref Unsafe.AsRef(in image));

#endregion
    
#region Standard API Proxying 

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void Destroy(vk.Device device) => VK.API.DestroyImage(device, Handle, null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetMemoryRequirements(vk.Device device, out vk.MemoryRequirements requirements) 
        => VK.API.GetImageMemoryRequirements(device, Handle, out requirements);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindMemory(vk.Device device, SlimDeviceMemory memory, u64 memoryOffset)
        => VK.API.BindImageMemory(device, Handle, memory, memoryOffset);

    #endregion

#region User defined

    

#endregion
}