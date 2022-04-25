using Atom.Engine.Vulkan;

namespace Atom.Engine.Pipelines;

public interface IPipeline
{
    public void CmdExecutePipeline(SlimCommandBuffer cmd, RenderTarget target, Camera camera);
}