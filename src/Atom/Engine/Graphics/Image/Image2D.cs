using Atom.Engine.Vulkan;

namespace Atom.Engine;


// Standard

public abstract class StandardImage2D : StandardImage
{
    internal StandardImage2D() : base() { }
    internal StandardImage2D(SlimImage baseImage, vk.Device device) : base(baseImage, device) { }
}


// Device

public sealed class OptimalDeviceImage2D<T> : StandardImage2D, 
    IImageOptimalDevice2D, IImage2D<T>  
    where T : unmanaged, IImageFormat
{
    internal OptimalDeviceImage2D(SlimImage baseImage, vk.Device device) : base(baseImage, device) { }
}

public sealed class LinearDeviceImage2D<T> : StandardImage2D, 
    IImageLinearDevice2D, IImage2D<T>  
    where T : unmanaged, IImageFormat
{
    internal LinearDeviceImage2D(SlimImage baseImage, vk.Device device) : base(baseImage, device) { }
}


// Host

public sealed class OptimalHostImage2D<T> : StandardImage2D, 
    IImageOptimal, IImageHost, IImage2D<T>  
    where T : unmanaged, IImageFormat
{
    internal OptimalHostImage2D(SlimImage baseImage, vk.Device device) : base(baseImage, device) { }
}

public sealed class LinearHostImage2D<T> : StandardImage2D, 
    IImageLinear, IImageHost, IImage2D<T>  
    where T : unmanaged, IImageFormat
{
    internal LinearHostImage2D(SlimImage baseImage, vk.Device device) : base(baseImage, device) { }


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