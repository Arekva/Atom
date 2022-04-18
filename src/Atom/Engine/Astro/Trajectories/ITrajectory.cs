using Silk.NET.Maths;

namespace Atom.Engine.Astro;

public interface ITrajectory
{
    public Vector3D<f64> GetRelativePosition(f64 universalTime);
}