using Atom.Engine.Shader;
using Silk.NET.Maths;

namespace Atom.Engine.Astro;

public class Star : CelestialBody
{
    public f64 Temperature { get; set; }
    
    public Star(
        string name, f64 radius, f64 mass, ICelestialBody reference
    ) : base(name, radius, mass, reference)
    {
        
    }
}