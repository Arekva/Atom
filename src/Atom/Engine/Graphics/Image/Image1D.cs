using Atom.Engine.Vulkan;

namespace Atom.Engine;


// Standard

public abstract class StandardImage1D : StandardImage
{
    internal StandardImage1D(vk.Device device, SlimImage baseImage) : base(device, baseImage) { }
    
    internal StandardImage1D(vk.Device device, SlimImage baseImage, MemorySegment segment) : base(device, baseImage, segment) { }
}


// Device

public sealed class OptimalDeviceImage1D<T> : StandardImage1D, 
    IImageOptimal, IImageDevice, IImage1D<T>  
    where T : unmanaged, IImageFormat
{
    internal OptimalDeviceImage1D(vk.Device device, SlimImage baseImage) : base(device, baseImage) { }
    
    internal OptimalDeviceImage1D(vk.Device device, SlimImage baseImage, MemorySegment segment) : base(device, baseImage, segment) { }
}

public sealed class LinearDeviceImage1D<T> : StandardImage1D, 
    IImageLinear, IImageDevice, IImage1D<T>  
    where T : unmanaged, IImageFormat
{
    internal LinearDeviceImage1D(vk.Device device, SlimImage baseImage) : base(device, baseImage) { }
    
    internal LinearDeviceImage1D(vk.Device device, SlimImage baseImage, MemorySegment segment) : base(device, baseImage, segment) { }
}


// Host

public sealed class OptimalHostImage1D<T> : StandardImage1D, 
    IImageOptimal, IImageHost, IImage1D<T>  
    where T : unmanaged, IImageFormat
{
    internal OptimalHostImage1D(vk.Device device, SlimImage baseImage) : base(device, baseImage) { }
    
    internal OptimalHostImage1D(vk.Device device, SlimImage baseImage, MemorySegment segment) : base(device, baseImage, segment) { }
}

public sealed class LinearHostImage1D<T> : StandardImage1D, 
    IImageLinear, IImageHost, IImage1D<T>  
    where T : unmanaged, IImageFormat
{
    internal LinearHostImage1D(vk.Device device, SlimImage baseImage) : base(device, baseImage) { }
    
    internal LinearHostImage1D(vk.Device device, SlimImage baseImage, MemorySegment segment) : base(device, baseImage, segment) { }
}