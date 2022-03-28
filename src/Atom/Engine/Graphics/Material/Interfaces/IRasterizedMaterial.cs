using Atom.Engine.Shader;
using Atom.Engine.Vulkan;
using Silk.NET.Maths;

namespace Atom.Engine;

public interface IRasterizedMaterial : IMaterial
{
    /// <summary> The shader used by this material's pipeline. </summary>
    public IRasterShader Shader { get; }


#region Settings
    
    public Topology Topology { get; set; }

    public Tessellation Tessellation { get; set; }
        
    public Viewport Viewport { get; set; }

    public Rasterizer Rasterizer { get; set; }

    public Multisampling Multisampling { get; set; }
        
    public DepthStencil DepthStencil { get; set; }
    
    public ColorBlending ColorBlending { get; set; }
    
#endregion

    public void CmdBindLightMaterial(SlimCommandBuffer cmd, Vector2D<u32> extent, u32 cameraIndex, u32 frameIndex);

}