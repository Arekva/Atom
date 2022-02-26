using System.Runtime.InteropServices;

namespace Atom.Engine.DDS;

[StructLayout(LayoutKind.Sequential)]
public struct PixelFormat
{
    private const uint STRUCTURE_SIZE = 32U;
    
    
    
    private uint _size;
    
    private PixelFormatFlags _flags;
    
    private FourCharCodes _fourCC;
    
    private uint _rgbBitCount;
    
    private uint _rBitMask;
    
    private uint _gBitMask;

    private uint _bBitMask;
    
    private uint _aBitMask;
    
    
    
    public uint Size => _size;
    
    public PixelFormatFlags Flags => _flags;
    
    public FourCharCodes FourCharCode => _fourCC;
    
    public uint RGBBitCount => _rgbBitCount;
    
    public uint RBitMask => _rBitMask;
    
    public uint GBitMask => _gBitMask;
    
    public uint BBitMask => _bBitMask;
    
    public uint ABitMask => _aBitMask;

    

    public void ThrowIfWrongSize()
    {
        if (_size != STRUCTURE_SIZE)
        {
            throw new InvalidDDSFile(message: $"Pixel format structure's size is set to {_size} " + 
                                              $"while it should be {STRUCTURE_SIZE}");
        }
    }
}