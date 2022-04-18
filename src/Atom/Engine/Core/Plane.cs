using Silk.NET.Maths;

namespace Atom.Engine;

public struct Plane
{
    public Vector3D<f64> Right, Up, Forward;

    public Vector3D<f64> Left     => -Right;
    public Vector3D<f64> Down     => -Up;
    public Vector3D<f64> Backward => -Forward;

    public Plane(Vector3D<f64> right, Vector3D<f64> up, Vector3D<f64> forward) 
        => (Right, Up, Forward) = (right, up, forward);
}