using Atom.Engine.Vulkan;
using Silk.NET.Maths;

namespace Atom.Engine.Pipelines;

public interface IPipeline : IDisposable
{
    public bool OutputsColor { get; }
    
    public bool OutputsDepth { get; }




    public void Resize(Vector2D<u32> resolution, RenderTarget target);

    public void CmdRender(Camera camera, u32 frameIndex, CommandRecorder recorder, IEnumerable<Drawer> drawers);
}