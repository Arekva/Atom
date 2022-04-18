using Silk.NET.Maths;

namespace Atom.Engine.Astro;

public class NoOrbit : ITrajectory
{
    public Vector3D<Double> GetRelativePosition(Double universalTime)
    {
        return Vector3D<Double>.Zero;
    }
}