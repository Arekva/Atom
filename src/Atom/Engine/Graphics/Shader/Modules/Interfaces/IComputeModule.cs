namespace Atom.Engine.Shader;

[Module(ShaderStageFlags.Compute, "*.comp.spv")]
public interface IComputeModule : IShaderModule { }