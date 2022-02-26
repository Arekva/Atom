namespace Atom.Engine.DDS;

[Flags]
public enum PixelFormatFlags : uint
{
    None = 0,
    /// <summary> Texture contains alpha data; <see cref="PixelFormat.R/G/B/AlphaBitMask"/> contains valid data. </summary>
    AlphaPixels = 0x1,
    /// <summary> Used in some older DDS files for alpha channel only uncompressed data
    /// (<see cref="PixelFormat.RGBBitCount"/> contains the alpha channel bitcount; <see cref="PixelFormat.ABitMask"/>
    /// contains valid data) </summary>
    Alpha = 0x2,
    /// <summary> Texture contains compressed RGB data; <see cref="PixelFormat.FourCharCode"/> contains valid data. </summary>
    FourCharCode = 0x4,
    /// <summary> Texture contains uncompressed RGB data; <see cref="PixelFormat.RGBBitCount"/> and the RGB masks
    /// (<see cref="PixelFormat.RBitMask"/>, <see cref="PixelFormat.GBitMask"/>, <see cref="PixelFormat.BBitMask"/>)
    /// contain valid data. </summary>
    RGB = 0x40,
    /// <summary> Used in some older DDS files for YUV uncompressed data (<see cref="PixelFormat.RGBBitCount"/> contains
    /// the YUV bit count; <see cref="PixelFormat.RBitMask"/> contains the Y mask, <see cref="PixelFormat.GBitMask"/>
    /// contains the U mask, <see cref="PixelFormat.BBitMask"/> contains the V mask) </summary>
    YUV = 0x200,
    /// <summary> Used in some older DDS files for single channel color uncompressed data
    /// (<see cref="PixelFormat.RGBBitCount"/> contains the luminance channel bit count;
    /// <see cref="PixelFormat.RBitMask"/> contains the channel mask). Can be combined with <see cref="AlphaPixels"/>
    /// for a two channel DDS file. </summary>
    Luminance = 0x20000,
}