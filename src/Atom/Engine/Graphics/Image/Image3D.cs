using Atom.Engine.Vulkan;

namespace Atom.Engine;


// Standard

public abstract class StandardImage3D : StandardImage
{
    internal StandardImage3D(vk.Device device, SlimImage baseImage) : base(device, baseImage) { }
    
    internal StandardImage3D(vk.Device device, SlimImage baseImage, MemorySegment segment) : base(device, baseImage, segment) { }
}


// Device

public sealed class OptimalDeviceImage3D<T> : StandardImage3D, 
    IImageOptimal, IImageDevice, IImage3D<T>  
    where T : unmanaged, IImageFormat
{
    internal OptimalDeviceImage3D(vk.Device device, SlimImage baseImage) : base(device, baseImage) { }
    
    internal OptimalDeviceImage3D(vk.Device device, SlimImage baseImage, MemorySegment segment) : base(device, baseImage, segment) { }
}

public sealed class LinearDeviceImage3D<T> : StandardImage3D, 
    IImageLinear, IImageDevice, IImage3D<T>  
    where T : unmanaged, IImageFormat
{
    internal LinearDeviceImage3D(vk.Device device, SlimImage baseImage) : base(device, baseImage) { }
    
    internal LinearDeviceImage3D(vk.Device device, SlimImage baseImage, MemorySegment segment) : base(device, baseImage, segment) { }
}


// Host

public sealed class OptimalHostImage3D<T> : StandardImage3D, 
    IImageOptimal, IImageHost, IImage3D<T>  
    where T : unmanaged, IImageFormat
{
    internal OptimalHostImage3D(vk.Device device, SlimImage baseImage) : base(device, baseImage) { }
    
    internal OptimalHostImage3D(vk.Device device, SlimImage baseImage, MemorySegment segment) : base(device, baseImage, segment) { }
}

public sealed class LinearHostImage3D<T> : StandardImage3D, 
    IImageLinear, IImageHost, IImage3D<T>  
    where T : unmanaged, IImageFormat
{
    internal LinearHostImage3D(vk.Device device, SlimImage baseImage) : base(device, baseImage) { }
    
    internal LinearHostImage3D(vk.Device device, SlimImage baseImage, MemorySegment segment) : base(device, baseImage, segment) { }
}