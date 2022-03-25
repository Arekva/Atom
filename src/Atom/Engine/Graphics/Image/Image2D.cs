using System.Reflection;
using Atom.Engine.Vulkan;

namespace Atom.Engine;


// Standard

public abstract class StandardImage2D : StandardImage
{
    internal StandardImage2D() : base()
    {
        this.Dimension = vk.ImageType.ImageType2D;
    }

    internal StandardImage2D(vk.Device device, SlimImage baseImage) : base(device, baseImage)
    {
        this.Dimension = vk.ImageType.ImageType2D;
    }

    internal StandardImage2D(vk.Device device, SlimImage baseImage, MemorySegment segment) : base(device, baseImage,
        segment)
    {
        this.Dimension = vk.ImageType.ImageType2D;
    }
}


// Device

public sealed class OptimalDeviceImage2D<T> : StandardImage2D, 
    IImageOptimalDevice2D, IImage2D<T>  
    where T : unmanaged, IImageFormat
{
    internal OptimalDeviceImage2D(vk.Device device, SlimImage baseImage) : base(device, baseImage) { }

    internal OptimalDeviceImage2D(vk.Device device, SlimImage baseImage, MemorySegment segment) : base(device, baseImage, segment) { }

    internal OptimalDeviceImage2D(vk.Device device, SlimImage baseImage, MemorySegment segment,
        vk.Extent3D extent, u32 mipLevels, u32 arrayLayers, 
        vk.SampleCountFlags multisampling, vk.ImageLayout layout) : base(device, baseImage, segment)
    {
        Format = typeof(T).GetCustomAttribute<ImageFormatAttribute>()!.Format;
        Tiling = vk.ImageTiling.Optimal;


        Extent        = extent       ;
        MipLevels     = mipLevels    ;
        ArrayLayers   = arrayLayers  ;
        Multisampling = multisampling;
        Layout        = layout       ;
    }
}

public sealed class LinearDeviceImage2D<T> : StandardImage2D, 
    IImageLinearDevice2D, IImage2D<T>  
    where T : unmanaged, IImageFormat
{
    internal LinearDeviceImage2D(vk.Device device, SlimImage baseImage) : base(device, baseImage) { }
    
    internal LinearDeviceImage2D(vk.Device device, SlimImage baseImage, MemorySegment segment) : base(device, baseImage, segment) { }
}


// Host

public sealed class OptimalHostImage2D<T> : StandardImage2D, 
    IImageOptimal, IImageHost, IImage2D<T>  
    where T : unmanaged, IImageFormat
{
    internal OptimalHostImage2D(vk.Device device, SlimImage baseImage) : base(device, baseImage) { }
    
    internal OptimalHostImage2D(vk.Device device, SlimImage baseImage, MemorySegment segment) : base(device, baseImage, segment) { }
}

public sealed class LinearHostImage2D<T> : StandardImage2D, 
    IImageLinear, IImageHost, IImage2D<T>  
    where T : unmanaged, IImageFormat
{
    internal LinearHostImage2D(vk.Device device, SlimImage baseImage) : base(device, baseImage) { }
    
    internal LinearHostImage2D(vk.Device device, SlimImage baseImage, MemorySegment segment) : base(device, baseImage, segment) { }


    public LinearHostImage2D(
        uint width, uint height,
        vk.ImageUsageFlags usages = vk.ImageUsageFlags.ImageUsageTransferSrcBit,
        vk.Device? device = null) : 
        this (width, height, queueFamilies: new[] { 0U } , usages, device) 
    { }

    public LinearHostImage2D(uint width, uint height,
        ReadOnlySpan<uint> queueFamilies,

        vk.ImageUsageFlags usages = vk.ImageUsageFlags.ImageUsageTransferSrcBit,
        vk.Device? device = null)
    {
        vk.Device used_device = device ?? VK.Device;
        Device = used_device;
        
        Handle = new SlimImage(
            used_device,
            type: vk.ImageType.ImageType2D,
            format: (vk.Format)ImageFormatMapper.Map(typeof(T)),
            new vk.Extent3D(width, height, 1U),
            mipLevels: 1U,
            arrayLayers: 1U,
            samples: vk.SampleCountFlags.SampleCount1Bit,
            vk.ImageTiling.Linear,
            usages,
            vk.SharingMode.Exclusive,
            queueFamilies,
            vk.ImageLayout.Undefined
        );

        CreateDedicatedMemory(properties: MemoryPropertyFlags.HostVisible | MemoryPropertyFlags.HostCoherent);
    }
}