using Silk.NET.Vulkan;

namespace Atom.Engine;

public struct VertexInput
{
    public VertexInputBindingDescription Binding;
    
    public Dictionary<uint, VertexInputAttribute> Attributes;
}