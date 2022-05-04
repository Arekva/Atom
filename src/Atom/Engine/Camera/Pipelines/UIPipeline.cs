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
    

    public void CmdRender(CommandRecorder recorder)
    {
        
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}