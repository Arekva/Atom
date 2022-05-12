using Atom.Engine.Vulkan;
using Silk.NET.Maths;

namespace Atom.Engine.Pipelines;

public class UIPipeline : IPipeline
{
    public bool OutputsColor => true ;
    
    public bool OutputsDepth => false;
    public void Resize(Vector2D<UInt32> resolution, RenderTarget target)
    {
        throw new NotImplementedException();
    }
    

    public void CmdRender(Camera camera, u32 frameIndex, CommandRecorder recorder, IEnumerable<Drawer> drawers)
    {
        
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}