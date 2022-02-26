using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;

namespace Atom.Engine.Vulkan;

public struct SlimImage
{
    public vk.Image Handle;
    
#region Creation & Non-API stuff

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe SlimImage(
        Device device, 
        ImageType type, Format format, Extent3D extent,
        uint mipLevels, uint arrayLayers,
        SampleCountFlags samples,
        ImageTiling tiling, ImageUsageFlags usage,
        SharingMode sharingMode, ReadOnlySpan<uint> queueFamilyIndices,
        ImageLayout initialLayout,
        ImageCreateFlags flags = 0)
    {
        fixed (uint* p_queues = queueFamilyIndices)
        {
            ImageCreateInfo create_info = new(
                imageType: type,
                format: format,
                extent: extent,
                mipLevels: mipLevels,
                arrayLayers: arrayLayers,
                samples: samples,
                tiling: tiling,
                usage: usage,
                sharingMode: sharingMode,
                queueFamilyIndexCount: (uint) queueFamilyIndices.Length,
                pQueueFamilyIndices: p_queues,
                initialLayout: initialLayout,
                flags: flags
            );

            Result result = VK.API.CreateImage(device, in create_info, null, out Handle);
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

#endregion
    
#region Standard API Proxying 

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Destroy(Device device) => VK.API.DestroyImage(device, Handle, ReadOnlySpan<AllocationCallbacks>.Empty);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetMemoryRequirements(Device device, out MemoryRequirements requirements) 
        => VK.API.GetImageMemoryRequirements(device, Handle, out requirements);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindMemory(Device device, vk.DeviceMemory memory, ulong memoryOffset)
        => VK.API.BindImageMemory(device, Handle, memory, memoryOffset);

    #endregion

#region User defined

    

#endregion
}