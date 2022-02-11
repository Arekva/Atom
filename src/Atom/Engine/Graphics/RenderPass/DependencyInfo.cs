using Silk.NET.Vulkan;

namespace Atom.Engine.RenderPass;

public struct DependencyInfo
{
    public DependencyInfo(Subpass subpass, PipelineStageFlags stageMask, AccessFlags accessMask)
    {
        Subpass = subpass;
        StageMask = stageMask;
        AccessMask = accessMask;
    }

    public Subpass Subpass;
    public PipelineStageFlags StageMask;
    public AccessFlags AccessMask;
}