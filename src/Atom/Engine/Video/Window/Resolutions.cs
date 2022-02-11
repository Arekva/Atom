using System.Reflection;
using Silk.NET.Maths;

// ReSharper disable InconsistentNaming

namespace Atom.Engine;

public static class Resolutions
{
    // 1:1
    public static readonly Vector2D<uint> Res1K = new(1<<10, 1<<10); // 1:1 2^10 - 1024
    public static readonly Vector2D<uint> Res2K = new(1<<11, 1<<11); // 1:1 2^11 - 2048
    public static readonly Vector2D<uint> Res4K = new(1<<12, 1<<12); // 1:1 2^12 - 4096
    public static readonly Vector2D<uint> Res8K = new(1<<13, 1<<13); // 1:1 2^13 - 8196
    
    // 16:9
    public static readonly Vector2D<uint> HD     = new(1280, 720); // 16:9 720p
    public static readonly Vector2D<uint> FullHD = new(1920, 1080); // 16:9 1080p
    public static readonly Vector2D<uint> QuadHD = new(2560, 1440); // 16:9 1440p

    // 21:9
    public static readonly Vector2D<uint> UltraWideFullHD = new(2560, 1080); // 21:9 1080p
    public static readonly Vector2D<uint> UltraWideQuadHD = new(3440, 1440); // 21:9 1440p

    public static IEnumerable<Vector2D<uint>> Enumerate 
    => typeof(Resolutions).GetFields(BindingFlags.Public | BindingFlags.Static).Select(field => (Vector2D<uint>)field.GetValue(null)!);
}