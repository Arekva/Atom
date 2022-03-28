using Silk.NET.Maths;

namespace Atom.Engine;

public interface ILight
{
    LightType     Type        { get;      }
    
    Location      Location    { get; set; }

    Vector3D<f64> Color       { get; set; }
    
    f64           Power       { get; set; }
    
    f64           EmitterSize { get; set; }
    
    bool          CastShadow  { get;      }
}