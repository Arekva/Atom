namespace Atom.Engine.Shader;

[Module(ShaderStageFlags.Mesh_NV, "*.mesh.spv")] 
public interface IMeshModule : IRasterModule   { }
