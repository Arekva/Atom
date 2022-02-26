using Atom.Engine.DirectX;

namespace Atom.Engine.DDS;

public struct DXT10Header
{
    private DirectX.Format _format;

    private ResourceDimension _resourceDimension;

    private ResourceMiscFlags _miscFlag;

    private uint _arraySize;

    private AlphaModes _miscFlags2;



    public DirectX.Format Format => _format;

    public ResourceDimension ResourceDimension => _resourceDimension;

    public ResourceMiscFlags MiscFlags => _miscFlag;

    public uint ArraySize => _arraySize;

    public AlphaModes MiscFlags2 => _miscFlags2;
}