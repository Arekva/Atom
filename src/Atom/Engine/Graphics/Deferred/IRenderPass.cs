using Atom.Engine.Vulkan;

namespace Atom.Engine;

public interface IRenderPass
{
    public string Name { get; }

    public List<IRenderPass> SubRenderPasses { get; }

    public void Perform(
        SlimCommandBuffer cmd, 
        ReadOnlySpan<SlimImageView> inputs, ReadOnlySpan<SlimImageView> outputs)
    {
        
    }
}