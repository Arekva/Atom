namespace Atom.Engine.DDS;

[Flags]
public enum CapsFlags : uint
{
    None = 0,
    /// <summary> Optional; must be used on any file that contains more than one surface (a mipmap, a cubic environment
    /// map, or mipmapped volume texture). </summary>
    Complex = 0x8,
    /// <summary> Required </summary>
    Texture = 0x1000,
    /// <summary> Optional; should be used for a mipmap.</summary>
    MipMap = 0x400000,
}