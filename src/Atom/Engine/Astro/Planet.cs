using Atom.Engine.Shader;
using Silk.NET.Maths;

namespace Atom.Engine.Astro;

public class Planet : CelestialBody
{
    public Planet(
        string name, f64 radius, f64 mass, ICelestialBody reference
    ) : base(name, radius, mass, reference)
    {
        
    }
}