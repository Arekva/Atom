namespace Atom.Engine;

public struct ImageSubresourceRange
{
    public ImageSubresourceRange(ImageAspectFlags aspectMask, Range mipLevels, Range arrayLayers)
    {
        AspectMask = aspectMask;
        MipLevels = mipLevels;
        ArrayLayers = arrayLayers;
    }

    public ImageAspectFlags AspectMask { get; }
    public Range MipLevels { get; }
    public Range ArrayLayers { get; }
}