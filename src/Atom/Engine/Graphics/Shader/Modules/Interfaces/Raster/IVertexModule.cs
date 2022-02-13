using Silk.NET.Vulkan;

namespace Atom.Engine.Shader;

[Module(ShaderStageFlags.Vertex, "*.vert.spv")]
public interface IVertexModule : IRasterModule
{
#region Descriptors

    public Dictionary<uint, VertexInput> VertexInputs { get; }

#endregion
}