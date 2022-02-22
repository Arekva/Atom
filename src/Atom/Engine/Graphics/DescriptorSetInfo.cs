using Atom.Engine.Shader;
using Silk.NET.Vulkan;

namespace Atom.Engine;

public struct DescriptorSetInfo
{
    public DescriptorSet Set;
    public Descriptor Descriptor;
    public DescriptorType Type;
}