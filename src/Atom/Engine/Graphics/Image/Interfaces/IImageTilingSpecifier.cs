using Silk.NET.Vulkan;

namespace Atom.Engine;

public interface IImageTilingSpecifier
{
    public ImageTiling Tiling { get; }
}

public interface IImageLinear : IImageTilingSpecifier { }

public interface IImageOptimal : IImageTilingSpecifier { }