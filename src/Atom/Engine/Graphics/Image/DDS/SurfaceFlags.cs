namespace Atom.Engine.DDS;

[Flags]
public enum SurfaceFlags : uint
{
    None = 0,
    MipMap = CapsFlags.Complex | CapsFlags.MipMap,
    Texture = CapsFlags.Texture,
    Cubemap = CapsFlags.Complex
}