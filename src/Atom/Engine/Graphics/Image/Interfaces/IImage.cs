using Atom.Engine.Vulkan;

namespace Atom.Engine;

public interface IImage : IDisposable
{
    public vk.Device Device { get; }
    public SlimImage Handle { get; }
    
    public uint Width { get; }
    public uint Height { get; }
    public uint Depth { get; }


    public vk.ImageType Dimension { get; }

    public ImageFormat Format { get; }

    public uint MipLevels { get; }
    public uint ArrayLayers { get; }

    public vk.SampleCountFlags Multisampling { get; }
    
    public vk.ImageLayout Layout { get; }

    public ImageSubresource CreateSubresource();
}

public interface IImage<T> : IImage where T : unmanaged, IImageFormat { }