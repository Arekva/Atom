namespace Atom.Engine.Shader;

[Module(ShaderStageFlags.Miss_KHR,"*.rmiss.spv")] 
public interface IMissModule : IRayTracingModule { }