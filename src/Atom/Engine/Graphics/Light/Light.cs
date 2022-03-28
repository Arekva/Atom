using Silk.NET.Maths;

namespace Atom.Engine;

public abstract class Light : ILight
{
    public static vk.RenderPass MainRenderPass { get; set; }
    public static u32 MainSubpass { get; set; }

    static Light()
    {
        
    }
    
    
    private u32 Index { get; }

    public Location Location { get; set; }
    public LightType Type { get; }
    public Vector3D<f64> Color { get; set; }
    public f64 Power { get; set; }
    public f64 EmitterSize { get; set; }
    public bool CastShadow { get; }
    
}