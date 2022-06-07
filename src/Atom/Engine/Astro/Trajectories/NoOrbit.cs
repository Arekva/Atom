using Silk.NET.Maths;

namespace Atom.Engine.Astro;

public class NoOrbit : ITrajectory
{
    public Vector3D<f64> GetRelativePosition(f64 universalTime) => Vector3D<f64>.Zero;
}