namespace Atom.Engine.DDS;

[Flags]
public enum CubemapFlags : uint
{
    None = 0,
    
    PositiveX = CapsFlags2.CubeMap | CapsFlags2.CubeMapPositiveX,
    NegativeX = CapsFlags2.CubeMap | CapsFlags2.CubeMapNegativeX,
    PositiveY = CapsFlags2.CubeMap | CapsFlags2.CubeMapPositiveY,
    NegativeY = CapsFlags2.CubeMap | CapsFlags2.CubeMapNegativeY,
    PositiveZ = CapsFlags2.CubeMap | CapsFlags2.CubeMapPositiveZ,
    NegativeZ = CapsFlags2.CubeMap | CapsFlags2.CubeMapNegativeZ,
    
    AllFaces = PositiveX | NegativeX | PositiveY | NegativeY | PositiveZ | NegativeZ,
}