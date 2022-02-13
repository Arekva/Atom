namespace Atom.Engine.Shader;

[Module(ShaderStageFlags.Fragment, "*.frag.spv")] 
public interface IFragmentModule : IRasterModule { }