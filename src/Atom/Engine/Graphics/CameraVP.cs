using Silk.NET.Maths;

namespace Atom.Engine;

public struct CameraVP
{
    public Matrix4X4<float> View;
    public Matrix4X4<float> Projection;
}