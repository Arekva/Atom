namespace Atom.Engine.Shader;

[Module(ShaderStageFlags.AnyHit_KHR,"*.rahit.spv")]
public interface IAnyHitModule : IRayTracingModule { }