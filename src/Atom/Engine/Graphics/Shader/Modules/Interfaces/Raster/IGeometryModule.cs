namespace Atom.Engine.Shader;

[Module(ShaderStageFlags.Geometry, "*.geom.spv")] 
public interface IGeometryModule : IRasterModule   { }