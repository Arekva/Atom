using Silk.NET.Maths;

namespace Atom.Engine.Astro;

public interface IGravity
{
    public Vector3D<f32> Force { get; }
}