// ReSharper disable InconsistentNaming
namespace Atom.Engine;

[ImageFormat(ImageFormat.R8G8B8A8_UInt)]
public struct R8G8B8A8_UInt : IImageFormat
{
    public byte R, G, B, A;
}

[ImageFormat(ImageFormat.R8G8B8A8_SInt)]
public struct R8G8B8A8_SInt : IImageFormat
{
    public sbyte R, G, B, A;
}

[ImageFormat(ImageFormat.R32G32B32A32_SFloat)]
public struct R32G32B32A32_SFloat : IImageFormat
{
    public float R, G, B, A;
}

[ImageFormat(ImageFormat.R32G32B32A32_UInt)]
public struct R32G32B32A32_UInt : IImageFormat
{
    public uint R, G, B, A;
}

[ImageFormat(ImageFormat.R32G32B32A32_SInt)]
public struct R32G32B32A32_SInt : IImageFormat
{
    public int R, G, B, A;
}