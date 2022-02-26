namespace Atom.Engine.DirectX;

public enum ResourceDimension : uint
{
    Unknown   = 0,
    Buffer    = 1,
    /// <summary> Resource is a 1D texture. The <see cref="Header.Width"/> member of <see cref="Header"/> specifies the
    /// size of the texture. Typically, you set the <see cref="Header.Height"/> member of DDS_HEADER to 1; you also must
    /// set the <see cref="HeaderFlags.Height"/> flag in the <see cref="Header.Flags"/> member of <see cref="Header"/>.
    /// </summary>
    Texture1D = 2,
    /// <summary> Resource is a 2D texture with an area specified by the <see cref="Header.Width"/> and
    /// <see cref="Header.Height"/> members of <see cref="Header"/>. You can also use this type to identify a cube-map
    /// texture. For more information about how to identify a cube-map texture, see <see cref="DXT10Header.MiscFlag"/> and
    /// <see cref="DXT10Header.ArraySize"/> members. </summary>
    Texture2D = 3,
    /// <summary> Resource is a 3D texture with a volume specified by the <see cref="Header.Width"/>,
    /// <see cref="Header.Height"/>, and <see cref="Header.Depth"/> members of <see cref="Header"/>.
    /// You also must set the <see cref="HeaderFlags.Depth"/> flag in the dwFlags member of <see cref="Header"/>. </summary>
    Texture3D = 4,
}