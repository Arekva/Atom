using Silk.NET.Maths;

namespace Atom.Engine;

public interface IDrawer
{
    public void CmdDraw(SlimCommandBuffer cmd, Vector2D<uint> extent, uint cameraIndex, uint frameIndex);
}