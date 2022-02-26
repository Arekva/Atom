// ReSharper disable InconsistentNaming
namespace Atom.Engine;

[ImageFormat(ImageFormat.B8G8R8A8_UInt)]
public struct B8G8R8A8_UInt : IImageFormat
{
    public byte B, G, R, A;
}

[ImageFormat(ImageFormat.B8G8R8A8_SInt)]
public struct B8G8R8A8_SInt : IImageFormat
{
    public sbyte B, G, R, A;
}

[ImageFormat(ImageFormat.B8G8R8A8_sRGB)]
public struct B8G8R8A8_sRGB : IImageFormat
{
    public byte B, G, R, A;
}

[ImageFormat(ImageFormat.B8G8R8A8_UScaled)]
public struct B8G8R8A8_UScaled : IImageFormat
{
    public byte B, G, R, A;
}

[ImageFormat(ImageFormat.B8G8R8A8_SScaled)]
public struct B8G8R8A8_SScaled : IImageFormat
{
    public sbyte B, G, R, A;
}

[ImageFormat(ImageFormat.B8G8R8A8_UNorm)]
public struct B8G8R8A8_UNorm : IImageFormat
{
    public byte B, G, R, A;
}

[ImageFormat(ImageFormat.B8G8R8A8_SNorm)]
public struct B8G8R8A8_SNorm : IImageFormat
{
    public sbyte B, G, R, A;
}