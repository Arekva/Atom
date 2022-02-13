namespace Atom.Engine.Shader;

/// <summary> A raytrace-able shader. </summary>
public interface IRaytraceShader : IShader
{ // todo: doc
    public IRayGenerationModule RayGenerationModule { get; }
    public IAnyHitModule AnyHitModule { get; }
    public IClosestHitModule ClosetHitModule { get; }
    public IMissModule MissModule { get; }
    public IIntersectionModule IntersectionModule { get; }
    public ICallableModule CallableModule { get; }
}