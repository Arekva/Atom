namespace Atom.Engine.Shader;

[Module(ShaderStageFlags.Intersection_KHR, "*.rint.spv")]
public interface IIntersectionModule : IRayTracingModule { }