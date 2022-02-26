using Atom.Engine.Vulkan;

namespace Atom.Engine;

public abstract partial class StandardImage : AtomObject, IImage
{
    
#region Vulkan Handles

    private SlimImage _handle;
    private vk.Device _device;
    
#endregion

    private vk.Extent3D _extent;
    public vk.Extent3D Extent => _extent;

    public uint Width => _extent.Width;
    public uint Height => _extent.Height;
    public uint Depth => _extent.Depth;

    public vk.ImageType Dimension { get; }

    public ImageFormat Format { get; }

    public uint MipLevels { get; }
    public uint ArrayLayers { get; }

    public vk.SampleCountFlags Multisampling { get; }

    public vk.ImageTiling Tiling { get; }
    

    
    private MemorySegment? _boundMemorySegment;

    private bool _doUseDedicatedMemory;
    


    protected internal SlimImage Handle
    {
        get => _handle;
        init => _handle = value;
    }
    
    protected internal vk.Device Device
    {
        get => _device;
        init => _device = value;
    }
    
    internal StandardImage() { }

    internal StandardImage(SlimImage baseImage, vk.Device device)
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

        _handle.GetMemoryRequirements(_device, out vk.MemoryRequirements reqs);

        return _boundMemorySegment.Value;
    }
}