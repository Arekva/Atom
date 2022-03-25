using Atom.Engine.Vulkan;
using Silk.NET.Vulkan;
using ComponentMapping = Atom.Engine.Vulkan.ComponentMapping;
using MemoryPropertyFlags = Atom.Engine.Vulkan.MemoryPropertyFlags;

namespace Atom.Engine;

public abstract partial class StandardImage : AtomObject, IImage
{
    
#region Vulkan Handles

    private SlimImage     _handle;
    private vk.Device     _device;
    
#endregion

    public vk.Extent3D Extent { get; protected init; }

    public u32 Width => Extent.Width;
    public u32 Height => Extent.Height;
    public u32 Depth => Extent.Depth;

    public vk.ImageType Dimension { get; protected init; }

    public ImageFormat Format { get; protected init; }

    public u32 MipLevels { get; protected init; }
    public u32 ArrayLayers { get; protected init; }

    public vk.SampleCountFlags Multisampling { get; protected init; }

    public vk.ImageTiling Tiling { get; protected init; }
    
    public vk.ImageLayout Layout { get; protected init; }
    

    
    private MemorySegment? _boundMemorySegment;

    private bool _doUseDedicatedMemory;
    


    public SlimImage Handle
    {
        get => _handle;
        protected internal init => _handle = value;
    }
    
    public vk.Device Device
    {
        get => _device;
        protected internal init => _device = value;
    }
    
    internal StandardImage() { }

    internal StandardImage(vk.Device device, SlimImage baseImage)
    {
        _handle = baseImage;
        _device = device;
    }
    
    internal StandardImage(vk.Device device, SlimImage baseImage,
        MemorySegment segment)
    {
        _handle = baseImage;
        _device = device;
        _boundMemorySegment = segment;

        _doUseDedicatedMemory = segment == segment.Memory.Whole;
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

    public ImageSubresource CreateSubresource()
    {
        return new ImageSubresource(
            image      : this                         ,
            viewType   : ImageViewType.ImageViewType2D,
            format     : Format                       ,
            components : ComponentMapping.Identity    ,
            aspectMask : ImageAspectFlags.Color       ,
            mipLevels  : ..(i32)MipLevels             ,
            arrayLayers: ..(i32)ArrayLayers
        );
    }
}