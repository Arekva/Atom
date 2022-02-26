using Silk.NET.Vulkan;

namespace Atom.Engine;

public interface IImage : IDisposable
{
    public uint Width { get; }
    public uint Height { get; }
    public uint Depth { get; }


    public ImageType Dimension { get; }

    public ImageFormat Format { get; }

    public uint MipLevels { get; }
    public uint ArrayLayers { get; }

    public SampleCountFlags Multisampling { get; }
}

public interface IImage<T> : IImage where T : unmanaged, IImageFormat { }