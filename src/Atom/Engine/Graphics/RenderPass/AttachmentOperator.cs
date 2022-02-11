using Silk.NET.Vulkan;

namespace Atom.Engine.RenderPass;

public struct AttachmentOperator
{
    public AttachmentLoadOp Load;
    public AttachmentStoreOp Store;

    public AttachmentOperator(AttachmentLoadOp load, AttachmentStoreOp store)
    {
        Load = load;
        Store = store;
    }
}