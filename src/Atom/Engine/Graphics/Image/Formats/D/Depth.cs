// ReSharper disable InconsistentNaming

using System.Runtime.InteropServices;

namespace Atom.Engine;

public static class Depth
{
    /*
     * D16_UNorm
     * D32_SFloat
     * D16_UNorm_S8_UInt
     * D24_UNorm_S8_UInt
     * D32_SFloat_S8_UInt
     */
    
    [ImageFormat(ImageFormat.D16_UNorm)]
    public struct D16_UNorm : IImageFormat
    {
        public ushort D;
    }
    
    [ImageFormat(ImageFormat.D32_SFloat)]
    public struct D32_SFloat : IImageFormat
    {
        public float D;
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    [ImageFormat(ImageFormat.D16_UNorm_S8_UInt)]
    public struct D16_UNorm_S8_UInt : IImageFormat
    {
        public Half D;
        public byte S;
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    [ImageFormat(ImageFormat.D24_UNorm_S8_UInt)]
    public struct D24_UNorm_S8_UInt : IImageFormat
    {
        // Special behavior on copy: 
        // on buffer copy, it is possible to select either D or S
        // D is packed in 32 bit, the first 24bit being the data and 8 last being undefined.
        public float D; // this is utterly wrong eh, but at least the S is placed correctly... lol
        public byte S;
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    [ImageFormat(ImageFormat.D32_SFloat_S8_UInt)]
    public struct D32_SFloat_S8_UInt : IImageFormat
    {
        public float D;
        public byte S;
    }
}