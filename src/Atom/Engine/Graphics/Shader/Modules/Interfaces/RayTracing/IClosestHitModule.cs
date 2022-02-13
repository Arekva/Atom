namespace Atom.Engine.Shader;

[Module(ShaderStageFlags.ClosestHit_KHR,"*.rchit.spv")] 
public interface IClosestHitModule : IRayTracingModule { }
