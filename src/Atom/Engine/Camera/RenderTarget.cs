using System.Globalization;
using System.Reflection;
using Atom.Engine.Vulkan;
using Silk.NET.Maths;

namespace Atom.Engine;

public class RenderTarget : IDisposable
{
    // Both optimal SRGB/UINT/UNORM for R8G8B8A8 and SFLOAT/SINT/UINT for R32G32B32A32 supported on all devices(*).
    // However linear is a less bit supported but should be fine on desktops.
    public const ImageFormat DEFAULT_COLOR_FORMAT = ImageFormat.R8G8B8A8_UNorm;

    // Both optimal D32_SFloat and D32_SFloat_S8_UInt should be supported on all devices(*).
    // However they are almost not supported as linear, not even on NVIDIA nor AMD.
    // So never use them with linear memory.
    public const ImageFormat DEFAULT_DEPTH_FORMAT = ImageFormat.D32_SFloat;

    // (*) on all devices: 98% of the total Vulkan devices, confidently 99.99% of the desktop devices.
    
    
    
    public delegate void ResizeDelegate(Vector2D<u32> resolution, ImageSubresource? color, ImageSubresource? depth);



    private vk.Device                 _device       ;
    private Vector2D<u32>             _resolution   ;
    private u32[]                     _queueFamilies;
    private bool                      _initialized  ;

    private ImageSubresource?         _colorImage   ;
    private ImageSubresource?         _depthImage   ;
    
    private Ownership<MemorySegment>? _memory       ;
    private bool                      _ownsImages   ;
    
    private string?                   _name         ;


    public ImageFormat ColorFormat => _colorImage?.Format ?? ImageFormat.None;
    public ImageFormat DepthFormat => _depthImage?.Format ?? ImageFormat.None;
    
    public bool ContainsColor      => _colorImage != null;
    public bool ContainsDepth      => _depthImage != null;

    public ImageSubresource? Color => _colorImage   ;
    public ImageSubresource? Depth => _depthImage   ;

    


    public event ResizeDelegate? OnResize;



    public RenderTarget(Vector2D<u32> resolution,
        ImageFormat colorFormat = DEFAULT_COLOR_FORMAT, ImageFormat depthFormat = DEFAULT_DEPTH_FORMAT,
        string? name = null,
        vk.Device? device = null)
    {
        _device = device ?? VK.Device;
        
        _name = name;

        CreateOwnedData(resolution, colorFormat, depthFormat);
        
        _resolution = resolution;
        _initialized = true;
        _ownsImages  = true;

        
    }

    public RenderTarget(Vector2D<u32> resolution,
        ImageSubresource? colorImage, ImageSubresource? depthImage
    )
    {
        _colorImage  = colorImage;

        _depthImage  = depthImage;

        _resolution = resolution;
        _initialized = true;
        _ownsImages = false;
    }

    public void Delete()
    {
        if (_ownsImages)
        {
            if (ContainsColor)
            {
                _colorImage?.Delete();
                _colorImage?.Image.Delete();
            }

            if (ContainsDepth)
            {
                _depthImage?.Delete();
                _depthImage?.Image.Delete();
            }
        }

        // delete if memory exists and is owned by this object (and not one of the images)
        _memory?.Do(FreeOwnedMemory);

        _colorImage = null;
        _depthImage = null;
        _memory     = null;

        GC.SuppressFinalize(this);
    }

    public void Dispose() => Delete();

    ~RenderTarget() => Dispose();
    
    
    
    public void Resize(Vector2D<u32> resolution) // only automatic 
    {
        if (_resolution == resolution) return;

        CreateOwnedData(resolution, ColorFormat, DepthFormat);

        _resolution = resolution;

        OnResize?.Invoke(resolution, _colorImage, _depthImage);
    }

    public void Resize(Vector2D<u32> resolution, ImageSubresource? newColor, ImageSubresource? newDepth)
    {
        if (resolution.X == 0U || resolution.Y == 0U || _resolution.X == resolution.X && _resolution.Y == resolution.Y)
        {
            return;
        }

        if (_ownsImages)
        {
            Log.Warning("This render texture has been created without base images, using automatic resize instead.");
            Resize(resolution);
            return;
        }

        // don't put extra maps if target doesn't already have one.
        if (ContainsColor) _colorImage = newColor;
        if (ContainsDepth) _depthImage = newDepth;

        OnResize?.Invoke(resolution, _colorImage, _depthImage); 
    }
    
    private void FreeOwnedMemory(ref MemorySegment segment) => segment.Memory.Delete();

    private void CreateOwnedData(Vector2D<u32> resolution, ImageFormat colorFormat, ImageFormat depthFormat)
    {
        if (resolution.X == 0U || resolution.Y == 0U)
        {
            // Log.Error("Unable to resize render target with 0 width or 0 height.");
            return;
        }

        if (_resolution.X == resolution.X && _resolution.Y == resolution.Y)
        {
            return;
        }

        bool do_color = colorFormat != ImageFormat.None;
        bool do_depth = depthFormat != ImageFormat.None;

        Vector3D<u32> res_3d = new(resolution, 1U);

        if (do_color && do_depth)
        {
            if (_initialized)
            {
                _colorImage!     .Delete();
                _colorImage.Image.Delete();
                
                _depthImage!     .Delete();
                _depthImage.Image.Delete();
                
                _memory!.Data.Memory.Delete();
            }

            Image color_image = new(
                resolution   : res_3d                         ,
                format       : colorFormat                    ,
                imageType    : vk.ImageType.ImageType2D       ,
                tiling       : vk.ImageTiling.Optimal         ,
                usage        : ImageUsageFlags.ColorAttachment|
                               ImageUsageFlags.InputAttachment|
                               ImageUsageFlags.TransferSource ,
                queueFamilies: _queueFamilies                 ,
                device       : _device
            );
            color_image.CreateImage(autoBind: true).Data.SetName($"{_name} RenderTarget color Image");
            color_image.ApplyPipelineBarrier(vk.ImageLayout.TransferSrcOptimal);
            
            Image depth_image = new(
                resolution   : res_3d                                 ,
                format       : depthFormat                            ,
                imageType    : vk.ImageType.ImageType2D               ,
                tiling       : vk.ImageTiling.Optimal                 ,
                usage        : ImageUsageFlags.DepthStencilAttachment |
                               ImageUsageFlags.InputAttachment        ,
                queueFamilies: _queueFamilies                        ,
                device       : _device
            );
            depth_image.CreateImage(autoBind: true).Data.SetName($"{_name} RenderTarget depth Image");
            depth_image.ApplyPipelineBarrier(vk.ImageLayout.TransferSrcOptimal);
            
            ((SlimImage)color_image).GetMemoryRequirements(_device, out vk.MemoryRequirements color_reqs);
            ((SlimImage)depth_image).GetMemoryRequirements(_device, out vk.MemoryRequirements depth_reqs);
            
            

            u64 color_offset = 0UL;
            u64 color_size   = color_reqs.Size;
            u64 depth_offset = AMath.Align(color_offset + color_size, depth_reqs.Alignment); 
            u64 depth_size   = depth_reqs.Size;

            u64 total_size = depth_offset + depth_size;
            
            VulkanMemory images_memory = new(_device, total_size, 
                memoryTypeIndex: VK.GPU.PhysicalDevice.FindMemoryType(
                    typeFilter : color_reqs.MemoryTypeBits,
                    properties : MemoryPropertyFlags.DeviceLocal)
            );

            color_image.BindMemory(new Ownership<MemorySegment>(
                data: images_memory.Segment(color_offset, color_size), 
                owned: false)
            );
            depth_image.BindMemory(new Ownership<MemorySegment>(
                data: images_memory.Segment(depth_offset, depth_size), 
                owned: false)
            );

            _colorImage = color_image.CreateSubresource(format: colorFormat, aspect: ImageAspectFlags.Color);
            ((SlimImageView)_colorImage).SetName($"{_name} RenderTarget color ImageSubresource");
            color_image.ApplyPipelineBarrier(vk.ImageLayout.TransferSrcOptimal);
            
            _depthImage = depth_image.CreateSubresource(format: depthFormat, aspect: ImageAspectFlags.Depth);
            ((SlimImageView)_depthImage).SetName($"{_name} RenderTarget depth ImageSubresource");
            depth_image.ApplyPipelineBarrier(vk.ImageLayout.TransferSrcOptimal);
            
            _memory = images_memory.Whole;
            _memory.Data.Memory.Handle.SetName($"{_name} RenderTarget Owned image's VulkanMemory");
        }
        else // only one image
        {
            ImageFormat     format;
            ImageUsageFlags usages;

            Image image;
            
            if (do_color)
            {
                if (_initialized)
                {
                    _colorImage!     .Delete();
                    _colorImage.Image.Delete();
                }

                format = colorFormat;
                usages = ImageUsageFlags.ColorAttachment        | ImageUsageFlags.InputAttachment;
            }
            else /*if (do_depth)*/
            {
                if (_initialized)
                {
                    _depthImage!     .Delete();
                    _depthImage.Image.Delete();
                }

                format = depthFormat;
                usages = ImageUsageFlags.DepthStencilAttachment | ImageUsageFlags.InputAttachment;
            }
            
            _memory?.Do(FreeOwnedMemory);
            
            image = new Image(
                resolution   : res_3d                  ,
                format       : format                  ,
                imageType    : vk.ImageType.ImageType2D,
                tiling       : vk.ImageTiling.Optimal  ,
                usage        : usages                  ,
                queueFamilies: _queueFamilies         ,
                device       : _device
            );
            image.CreateImage(autoBind: true);
            _memory = image.CreateMemory(properties: MemoryPropertyFlags.DeviceLocal, autoBind: true);
            _memory.Data.Memory.Handle.SetName($"{_name} RenderTarget Owned image's VulkanMemory");

            image.ApplyPipelineBarrier(vk.ImageLayout.TransferSrcOptimal);
            
            if (ContainsColor)
            {
                _colorImage = image.CreateSubresource(format: colorFormat, aspect: ImageAspectFlags.Color);
                ((SlimImage)image).SetName($"{_name} RenderTarget color Image");
            }
            else
            {
                _depthImage = image.CreateSubresource(format: depthFormat, aspect: ImageAspectFlags.Depth);
                ((SlimImage)image).SetName($"{_name} RenderTarget depth Image");
            }
        }
    }
}