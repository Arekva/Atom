using Silk.NET.Vulkan;

namespace Atom.Engine;

public abstract partial class StandardImage : AtomObject, IImage
{
    
#region Vulkan Handles

    private SlimImage _handle;
    private Device _device;
    
#endregion

    private Extent3D _extent;
    public Extent3D Extent => _extent;

    public uint Width => _extent.Width;
    public uint Height => _extent.Height;
    public uint Depth => _extent.Depth;

    public ImageType Dimension { get; }

    public ImageFormat Format { get; }

    public uint MipLevels { get; }
    public uint ArrayLayers { get; }

    public SampleCountFlags Multisampling { get; }

    public ImageTiling Tiling { get; }
    

    
    private MemorySegment? _boundMemorySegment;

    private bool _doUseDedicatedMemory;
    


    protected internal SlimImage Handle
    {
        get => _handle;
        init => _handle = value;
    }
    
    protected internal Device Device
    {
        get => _device;
        init => _device = value;
    }
    
    internal StandardImage() { }

    internal StandardImage(SlimImage baseImage, Device device)
    {
        _handle = baseImage;
        _device = device;
    }
    
    public override void Delete()
    {
        base.Delete();

        if (_doUseDedicatedMemory)
        {
            _boundMemorySegment!.Value.Memory.Dispose();
        }
        
        _handle.Destroy(_device);
    }
    
    
    internal MemorySegment CreateDedicatedMemory(MemoryPropertyFlags properties)
    {
        if (_boundMemorySegment != null)
        {
            throw new Exception("This image already has been bound to memory.");
        }
        _doUseDedicatedMemory = true;

        _boundMemorySegment = Handle.CreateDedicatedMemory(_device, properties);

        _handle.GetMemoryRequirements(_device, out MemoryRequirements reqs);

        return _boundMemorySegment.Value;
    }
}