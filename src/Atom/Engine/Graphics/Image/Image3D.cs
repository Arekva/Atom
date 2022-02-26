namespace Atom.Engine;


// Standard

public abstract class StandardImage3D : StandardImage
{
    internal StandardImage3D(SlimImage baseImage, vk.Device device) : base(baseImage, device) { }
}


// Device

public sealed class OptimalDeviceImage3D<T> : StandardImage3D, 
    IImageOptimal, IImageDevice, IImage3D<T>  
    where T : unmanaged, IImageFormat
{
    internal OptimalDeviceImage3D(SlimImage baseImage, vk.Device device) : base(baseImage, device) { }
}

public sealed class LinearDeviceImage3D<T> : StandardImage3D, 
    IImageLinear, IImageDevice, IImage3D<T>  
    where T : unmanaged, IImageFormat
{
    internal LinearDeviceImage3D(SlimImage baseImage, vk.Device device) : base(baseImage, device) { }
}


// Host

public sealed class OptimalHostImage3D<T> : StandardImage3D, 
    IImageOptimal, IImageHost, IImage3D<T>  
    where T : unmanaged, IImageFormat
{
    internal OptimalHostImage3D(SlimImage baseImage, vk.Device device) : base(baseImage, device) { }
}

public sealed class LinearHostImage3D<T> : StandardImage3D, 
    IImageLinear, IImageHost, IImage3D<T>  
    where T : unmanaged, IImageFormat
{
    internal LinearHostImage3D(SlimImage baseImage, vk.Device device) : base(baseImage, device) { }
}