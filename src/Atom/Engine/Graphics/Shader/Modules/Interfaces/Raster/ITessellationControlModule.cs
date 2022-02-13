namespace Atom.Engine.Shader;

[Module(ShaderStageFlags.TessellationControl, "*.tesc.spv")]
public interface ITessellationControlModule : IRasterModule   { }