using Atom.Engine.Vulkan;
using Silk.NET.Maths;

namespace Atom.Engine;

public class ClassicSkySphere : AtomObject, ISkySphere
{
    public Vector3D<double> AbsoluteAngle { get; set; }
    
    public ClassicSkySphere()
    {
        
    }
    
    
    public override void Delete()
    {
        base.Delete();
    }

    public void CmdDraw(SlimCommandBuffer cmd, Vector2D<uint> extent, uint cameraIndex, uint frameIndex)
    {
        
    }
}