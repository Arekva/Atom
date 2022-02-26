using Atom.Engine.Vulkan;

namespace Atom.Engine;


// Standard

public abstract class StandardImage1D : StandardImage
{
    internal StandardImage1D(SlimImage baseImage, vk.Device device) : base(baseImage, device) { }
}


// Device

public sealed class OptimalDeviceImage1D<T> : StandardImage1D, 
    IImageOptimal, IImageDevice, IImage1D<T>  
    where T : unmanaged, IImageFormat
{
    internal OptimalDeviceImage1D(SlimImage baseImage, vk.Device device) : base(baseImage, device) { }
}

public sealed class LinearDeviceImage1D<T> : StandardImage1D, 
    IImageLinear, IImageDevice, IImage1D<T>  
    where T : unmanaged, IImageFormat
{
    internal LinearDeviceImage1D(SlimImage baseImage, vk.Device device) : base(baseImage, device) { }
}


// Host

public sealed class OptimalHostImage1D<T> : StandardImage1D, 
    IImageOptimal, IImageHost, IImage1D<T>  
    where T : unmanaged, IImageFormat
{
    internal OptimalHostImage1D(SlimImage baseImage, vk.Device device) : base(baseImage, device) { }
}

public sealed class LinearHostImage1D<T> : StandardImage1D, 
    IImageLinear, IImageHost, IImage1D<T>  
    where T : unmanaged, IImageFormat
{
    internal LinearHostImage1D(SlimImage baseImage, vk.Device device) : base(baseImage, device) { }
}