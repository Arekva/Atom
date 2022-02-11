using Silk.NET.Maths;

namespace Atom.Engine;

public struct Plane
{
    public Vector3D<double> Right, Up, Forward;

    public Vector3D<double> Left     => -Right;
    public Vector3D<double> Down     => -Up;
    public Vector3D<double> Backward => -Forward;

    public Plane(Vector3D<double> right, Vector3D<double> up, Vector3D<double> forward) 
        => (Right, Up, Forward) = (right, up, forward);
}