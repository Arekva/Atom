using System.Runtime.CompilerServices;
using Atom.Engine.Vulkan;
using Silk.NET.Maths;

namespace Atom.Engine;

public class Image : AtomObject
{
    private vk.Device                   _device         ;
    private Ownership<SlimImage>?       _handle         ;
    private Ownership<MemorySegment>?   _memory         ;
    private u32[]                       _queueFamilies  ;
    private vk.SharingMode              _sharingMode    ;
    

    private Vector3D<u32>               _resolution     ;
    private vk.ImageType                _dimension      ;
    private ImageFormat                 _format         ;

    private u32                         _mipLevels      ;
    private u32                         _arrayLayers    ;

    private vk.SampleCountFlags         _multisampling  ;
      
    private ImageUsageFlags             _usage          ;
    private vk.ImageTiling              _tiling         ;
    private vk.ImageLayout              _layout         ;
    private vk.ImageLayout              _initialLayout  ;
    
    

    public Vector3D<u32> Resolution => _resolution;

    public u32 Width  => _resolution.X;
    public u32 Height => _resolution.Y;
    public u32 Depth  => _resolution.Z;

    public vk.ImageType         Dimension => _dimension;
    
    public ImageFormat          Format    => _format;

    public u32                  MipLevels   => _mipLevels;
    
    public u32                  ArrayLayers => _arrayLayers;
    
    
    public vk.SampleCountFlags  Multisampling => _multisampling;
    
    public ImageUsageFlags      Usage => _usage;
    
    public vk.ImageTiling       Tiling => _tiling;
    
    public vk.ImageLayout       Layout => _layout;

    public vk.SharingMode SharingMode => _sharingMode;
    
    public vk.Device Device
    {
        get => _device;
        protected internal init => _device = value;
    }
    
    public Image(
        u32 resolution, ImageFormat format,
        vk.ImageTiling tiling, ImageUsageFlags usage, ReadOnlySpan<u32> queueFamilies,

        u32 mipLevels = 1U, u32 arrayLayers = 1U,
        vk.SampleCountFlags multisampling = vk.SampleCountFlags.SampleCount1Bit,

        vk.SharingMode sharingMode = vk.SharingMode.Exclusive,
        vk.ImageLayout layout = vk.ImageLayout.General,

        Ownership<MemorySegment>? memory = null,
        Ownership<SlimImage>? image = null,
        vk.Device? device = null) : this(
        new (resolution, 1U, 1U) , format, vk.ImageType.ImageType1D, tiling, usage, queueFamilies, mipLevels,
        arrayLayers, multisampling, sharingMode, layout, memory, image, device)
    { }

    public Image(
        Vector2D<u32> resolution, ImageFormat format,
        vk.ImageTiling tiling, ImageUsageFlags usage, ReadOnlySpan<u32> queueFamilies,

        u32 mipLevels = 1U, u32 arrayLayers = 1U,
        vk.SampleCountFlags multisampling = vk.SampleCountFlags.SampleCount1Bit,

        vk.SharingMode sharingMode = vk.SharingMode.Exclusive,
        vk.ImageLayout layout = vk.ImageLayout.General,

        Ownership<MemorySegment>? memory = null,
        Ownership<SlimImage>? image = null,
        vk.Device? device = null) : this(
        new Vector3D<u32>(resolution, 1U), format, vk.ImageType.ImageType2D, tiling, usage, queueFamilies, mipLevels,
        arrayLayers, multisampling, sharingMode, layout, memory, image, device)
    { }
    
    public Image(
        Vector3D<u32> resolution, ImageFormat format,
        vk.ImageTiling tiling, ImageUsageFlags usage, ReadOnlySpan<u32> queueFamilies,

        u32 mipLevels = 1U, u32 arrayLayers = 1U,
        vk.SampleCountFlags multisampling = vk.SampleCountFlags.SampleCount1Bit,

        vk.SharingMode sharingMode = vk.SharingMode.Exclusive,
        vk.ImageLayout layout = vk.ImageLayout.General,

        Ownership<MemorySegment>? memory = null,
        Ownership<SlimImage>? image = null,
        vk.Device? device = null) : this(
        resolution, format, vk.ImageType.ImageType3D, tiling, usage, queueFamilies, mipLevels,
        arrayLayers, multisampling, sharingMode, layout, memory, image, device)
    { }

    public Image(
        Vector3D<u32> resolution, ImageFormat format, vk.ImageType imageType, 
        vk.ImageTiling tiling, ImageUsageFlags usage, ReadOnlySpan<u32> queueFamilies,

        u32 mipLevels = 1U, u32 arrayLayers = 1U,
        vk.SampleCountFlags multisampling = vk.SampleCountFlags.SampleCount1Bit,
        
        vk.SharingMode sharingMode = vk.SharingMode.Exclusive,
        vk.ImageLayout layout = vk.ImageLayout.General,
        
        Ownership<MemorySegment>? memory = null,
        Ownership<SlimImage>?     image  = null,
        vk.Device? device = null)
    {
        vk.Device used_device = device ?? VK.Device;
        
        _device        = used_device            ;
        _queueFamilies = queueFamilies.ToArray();
        _resolution    = resolution             ;
        _sharingMode   = sharingMode            ;

        _dimension     = imageType              ;
        _format        = format                 ;

        _mipLevels     = mipLevels              ;
        _arrayLayers   = arrayLayers            ;

        _multisampling = multisampling          ;
        _tiling        = tiling                 ;
        _layout        = layout                 ;
        _usage         = usage                  ;
        
        if (image != null)
        {
            _initialLayout = layout;
            _handle        = image ;
        }
        else
        {
            _initialLayout = vk.ImageLayout.Undefined;
        }
        if (memory != null) _memory = memory;

        MakeReady();
    }
    
    public override void Delete()
    {
        base.Delete();

        _memory?.Do(DeleteMemory);
        
        _handle?.Do(DeleteImage);
    }

    private void DeleteMemory(ref MemorySegment data) => data.Memory.Dispose();
    
    private void DeleteImage(ref SlimImage data) => data.Destroy(_device);
    
    
    
    public Ownership<SlimImage> CreateImage(bool autoBind = true)
    {
        ref readonly vk.Extent3D extent = ref Unsafe.As<Vector3D<u32>, vk.Extent3D>(ref _resolution);
        
        Ownership<SlimImage> image = new SlimImage(
            device            : _device         ,
            type              : _dimension      ,
            format            : _format         ,
            extent            : extent          ,
            mipLevels         : _mipLevels      ,
            arrayLayers       : _arrayLayers    ,
            samples           : _multisampling  ,
            tiling            : _tiling         ,
            usage             : _usage          ,
            sharingMode       : _sharingMode    ,
            queueFamilyIndices: _queueFamilies,
            initialLayout     : _initialLayout  ,
            flags             : 0
        );

        if (autoBind)
        {
            BindImage(image);
            return image.Borrow();
        }
        else
        {
            return image;
        }
    }
    
    public Ownership<MemorySegment> CreateMemory(MemoryPropertyFlags properties, bool autoBind = true)
    {
#if !UNSAFE_VK
        if (_memory != null)
        {
            throw new Exception("This image already has been bound to memory.");
        }

        if (_handle == null)
        {
            throw new Exception("A slim image must be bound before creating any memory for it.");
        }
#endif
        MemorySegment memory = _handle.Data.CreateDedicatedMemory(_device, properties, false);
        
        if (autoBind)
        {
            BindMemory(memory);
            return _memory!.Borrow();
        }
        else
        {
            return memory;
        }
    }

    public void BindImage(Ownership<SlimImage>? image)
    {
        if (_handle != null)
        {
            throw new Exception("A handle already has been bound to this image.");
        }
        
        _handle = image;
        
        if (_handle?.Data != 0UL && _memory != null && _memory.Data.Memory != 0UL)
        {
            _handle!.Data.BindMemory(_device, _memory!.Data);
        }
    }

    public void BindMemory(Ownership<MemorySegment>? memory)
    {
        if (_memory != null)
        {
            throw new Exception("This image already has been bound to memory.");
        }

        if (_handle?.Data != 0UL && memory?.Data.Memory! != 0UL)
        {
            _handle!.Data.BindMemory(_device, memory!.Data);
        }
        
        _memory = memory;
    }
    

    public ImageSubresource CreateSubresource(
        ImageViewType     viewType = ImageViewType.Dim2D   , 
        ImageFormat       format   = ImageFormat.None      ,
        ImageAspectFlags  aspect   = ImageAspectFlags.Color,
        ComponentMapping? mapping  = null                  ,
        Range? mipLevels           = null                  ,
        Range? arrayLayers         = null
    ) => new(
        image      : this                                        ,
        viewType   : viewType                                    ,
        format     : format == ImageFormat.None ? Format : format,
        components : mapping     ?? ComponentMapping.Identity    ,
        aspectMask : aspect                                      ,
        mipLevels  : mipLevels   ?? Range.All                    ,
        arrayLayers: arrayLayers ?? Range.All
    );

    public void ApplyPipelineBarrier(vk.ImageLayout layout)
    {
        _layout = layout;
    }
    

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SlimImage(in Image image) => image._handle?.Data ?? new SlimImage();
}