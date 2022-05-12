using Silk.NET.Maths;

namespace Atom.Engine;

public interface IProjection
{
    delegate void ProjectionCallback(in Matrix4X4<f64> projectionMatrix);
    
    
    
    ref readonly Matrix4X4<f64> ProjectionMatrix { get; }
    
    
    
    event ProjectionCallback? OnProjectionMatrixChange;
    
    
    
    ref readonly Matrix4X4<f64> Bake();
}