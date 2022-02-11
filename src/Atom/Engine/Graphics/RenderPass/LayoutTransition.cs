using Silk.NET.Vulkan;

namespace Atom.Engine.RenderPass;

public struct LayoutTransition
{
    public ImageLayout Initial;
    public ImageLayout Final;

    public LayoutTransition(ImageLayout initial, ImageLayout final)
    {
        Initial = initial;
        Final = final;
    }
}