using Silk.NET.Maths;
using Atom.Engine.Vulkan;

namespace Atom.Engine;

public interface IDrawer
{
    public void CmdDraw(SlimCommandBuffer cmd, Vector2D<uint> extent, uint cameraIndex, uint frameIndex);
}