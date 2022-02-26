using System.Runtime.InteropServices;

namespace Atom.Engine.DDS;

[StructLayout(LayoutKind.Sequential)]
public struct Header
{
    public const uint STRUCTURE_SIZE = 124U;

    
    
    private uint _size;
    
    private HeaderFlags _flags;

    private uint _height;
    
    private uint _width;

    private uint _pitchOrLinearSize;

    private uint _depth;

    private uint _mipMapCount;
    
    private unsafe fixed uint _reserved1[11]; // unused

    private PixelFormat _pf;

    private CapsFlags _caps;

    private CapsFlags2 _caps2;

    private uint _caps3; // unused

    private uint _caps4; // unused

    private uint _reserved2; // unused



    public uint Size => _size;

    public HeaderFlags Flags => _flags;

    public uint Height => _height;

    public uint Width => _width;

    public uint PitchOrLinearSize => _pitchOrLinearSize;

    public uint Depth => _depth;

    public uint MipMapCount => _mipMapCount;

    public PixelFormat PixelFormat => _pf;

    public CapsFlags Caps => _caps;

    public CapsFlags2 Caps2 => _caps2;
    
    
    
    public void ThrowIfWrongSize()
    {
        if (_size != STRUCTURE_SIZE)
        {
            throw new InvalidDDSFile(message: $"Header structure's size is set to {_size} " + 
                                              $"while it should be {STRUCTURE_SIZE}");
        }
    }
}