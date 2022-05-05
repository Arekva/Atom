using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

using Silk.NET.Maths;

using Atom.Engine.Vulkan;



namespace Atom.Engine;



public class Viewport : IDisposable
{
    private struct SurfaceCapabilities
    {
        public Clamp<u32>                   ImageCount              ;
        public Vector2D<u32>                CurrentExtent           ;
        public ClampVector2D<u32>           ImageVectors            ;
        public u32                          MaxImageArrayLayers     ;
        public vk.SurfaceTransformFlagsKHR  SupportedTransforms     ;
        public vk.SurfaceTransformFlagsKHR  CurrentTransform        ;
        public vk.CompositeAlphaFlagsKHR    SupportedCompositeAlpha ;
        public ImageUsageFlags              SupportedUsageFlags     ;
    }
    
    const vk.CompositeAlphaFlagsKHR SWAP_ALPHA_COMPOSITING = vk.CompositeAlphaFlagsKHR.CompositeAlphaOpaqueBitKhr      ;
    const vk.SurfaceTransformFlagsKHR SWAP_PRE_TRANSFORM   = vk.SurfaceTransformFlagsKHR.SurfaceTransformIdentityBitKhr;
    const ImageUsageFlags SWAP_IMAGE_USAGES                = ImageUsageFlags.TransferDestination; 
    const u32 SWAP_ARRAY_LAYERS                            = 1U /* for VR: 2U */                                       ; 
    const vk.SwapchainCreateFlagsKHR SWAP_FLAGS            = 0U                                                        ;
    const vk.ImageTiling SWAP_TILING                       = vk.ImageTiling.Optimal                                    ;
    const u32 SWAP_MIP_LEVELS                              = 1U                                                        ;
    const vk.SampleCountFlags SWAP_SAMPLES                 = vk.SampleCountFlags.SampleCount1Bit                       ;
    const vk.SharingMode SWAP_SHARING                      = vk.SharingMode.Exclusive                                  ;
    const vk.ImageLayout SWAP_LAYOUT                       = vk.ImageLayout.PresentSrcKhr                              ;
    const u32 MAX_IMAGES_IN_FLIGHT                         = 3U                                                        ;

    private static readonly HashSet<PresentMode> VSYNC_PRESENT_MODES     = new() { PresentMode.Mailbox, PresentMode.Fifo, PresentMode.FifoRelaxed };
    private static readonly HashSet<PresentMode> NON_VSYNC_PRESENT_MODES = new() { PresentMode.Immediate, PresentMode.SharedDemandRefresh, PresentMode.SharedContinuousRefresh };
    
    private vk.SwapchainKHR _swap           ;
    private u32             _swapImageCount ;
    private Image[]         _swapImages     ;
    private Vector2D<u32>   _swapResolution ;
    private PresentMode     _swapPresentMode;
    private ColorSpace      _swapColorSpace ;
    private ImageFormat     _swapFormat     ;
    private bool            _swapClipped    ;
    
    
    
    private vk.Device                _device        ;
    private vk.PhysicalDevice        _physicalDevice;
    private Ownership<vk.SurfaceKHR> _surface       ;
    private SlimQueue                _queue         ;
    private SlimCommandPool          _commandPool   ;
    private SlimCommandBuffer[]      _commands      ;
    // synchronisation
    // 0..7: fences in flight, 8..15: images in flight
    private SlimFence[]              _fences        ; 
    // 0..7: image available, 8..15: render finished
    private SlimSemaphore[]          _semaphores    ;

    private khr.KhrSwapchain         _swapchainExtension;
    private khr.KhrSurface           _surfaceExtension  ;

    private bool[]                   _swapchainsImagesPipelined;
    

    private Dictionary<ColorSpace, HashSet<ImageFormat>> _availableFormats     ;
    private HashSet<PresentMode>                         _availablePresentModes;
    private ImageUsageFlags                              _supportedUsageFlags  ;


    private Vector2D<u32>    _resolution   ;
    private ImageFormat      _colorFormat  ;
    private ColorSpace       _colorSpace   ;
    private PresentMode      _presentMode  ;
    private bool             _clipped      ;

    private u32              _current_frame;

    private Mutex            _renderGate   ;

    private ManualResetEvent _resizeFinish ;

    

    private volatile bool _deleted;
    public bool Deleted => _deleted;

    

    public Viewport(
        Ownership<vk.SurfaceKHR> surface,
        SlimQueue queue,
        vk.Device? device = null,
        vk.PhysicalDevice? physicalDevice = null)
    {
        _device = device ?? VK.Device;
        _physicalDevice = physicalDevice ?? VK.GPU.PhysicalDevice;
        _surface = surface;
        _queue = queue;
        
        if (!VK.API.TryGetInstanceExtension(VK.Instance, out _surfaceExtension))
        {
            throw new ExtensionNotFoundException(
                $"Surface extension not found, is {khr.KhrSurface.ExtensionName} present and enabled?");
        }
        
        if (!VK.API.TryGetDeviceExtension(VK.Instance, _device, out _swapchainExtension))
        {
            throw new ExtensionNotFoundException(
                $"Swapchain extension not found, is {khr.KhrSwapchain.ExtensionName} present and enabled?");
        }

        _commandPool = new SlimCommandPool(_device, 0, CommandPoolCreateFlags.ResetCommandBuffer);
        _commandPool.SetName("Viewport CommandPool");
        _commandPool.AllocateCommandBuffers(_device, CommandBufferLevel.Primary, MAX_IMAGES_IN_FLIGHT, out _commands);
        
#if DEBUG

        for (u32 i = 0; i < MAX_IMAGES_IN_FLIGHT; i++)
        {
            _commands[i].SetName($"Display command #{i}");
        }
        
#endif

        _fences     = new SlimFence    [MAX_IMAGES_IN_FLIGHT * 2];
        _semaphores = new SlimSemaphore[MAX_IMAGES_IN_FLIGHT * 2];

        _swapchainsImagesPipelined = new bool[MAX_IMAGES_IN_FLIGHT];
        
        for (u32 i = 0; i < MAX_IMAGES_IN_FLIGHT; i++)
        {
            _fences[i] = new SlimFence(_device, signaled: true);
            _fences[i].SetName($"Viewport frame Fence #{i}");
            _semaphores[i] = new SlimSemaphore(_device);
            _semaphores[i].SetName($"Viewport Image Available Semaphore #{i}");
        }
        for (i32 i = 0; i < MAX_IMAGES_IN_FLIGHT; i++) // 2x more semaphores than frames in flight.
        {
            _semaphores[i + MAX_IMAGES_IN_FLIGHT] = new SlimSemaphore(_device);
            _semaphores[i + MAX_IMAGES_IN_FLIGHT].SetName($"Viewport Display Finished Semaphore #{i}");
        }

        AssignPresentModes   ();
        AssignColors         ();
        AssignSupportedUsages();

        _colorFormat = //ImageFormat.A2B10G10R10_UNorm_Pack32;
                       ImageFormat.B8G8R8A8_UNorm      ;
        _colorSpace  = //ColorSpace.HDR10_ST2084             ;
                       ColorSpace.sRGB_NonLinear       ;

        if (IsVSyncSupported(out _presentMode))
        {
            
        }
        else
        {
            _presentMode = _availablePresentModes!.First();
        }
        
        _resolution  = new Vector2D<u32>(x:1024, y:768)    ;
        _clipped     = false                               ;
        
        LogSurfaceInfos   ();

        _renderGate = new Mutex(initiallyOwned: false);

        _resizeFinish = new ManualResetEvent(initialState: true);
    }
    
    public void Delete()
    {
        _renderGate.WaitOne();
        if (Deleted) return;
        _deleted = true;
        WaitForRenders();
        
        _commandPool.Destroy(_device);
        
        DestroyViews    (_swapImageCount);
        DestroySwapchain(_swap          );
        
        _surface.Do(DeleteSurface);
        
        for (u32 i = 0; i < MAX_IMAGES_IN_FLIGHT; i++)
        {
            _fences[i].Destroy(_device);
            _semaphores[i].Destroy(_device);
        }
        for (i32 i = 0; i < MAX_IMAGES_IN_FLIGHT; i++) // 2x more semaphores than frames in flight.
        {
            _semaphores[i + MAX_IMAGES_IN_FLIGHT].Destroy(_device);
        }

        GC.SuppressFinalize(this);
        
        _renderGate.ReleaseMutex();
    }

    public void Dispose() => Delete();

    ~Viewport() => Dispose();



    public bool IsVSyncSupported(out PresentMode mode)
    {
        foreach (PresentMode vsync_mode in VSYNC_PRESENT_MODES)
        {
            if (_availablePresentModes.Contains(vsync_mode))
            {
                mode = vsync_mode;
                return true;
            }
        }

        mode = default;
        return false;
    }

    public bool UpdateSwapchain(bool updateVideoSettings = false)
    {
        _surfaceExtension.GetPhysicalDeviceSurfaceCapabilities(_physicalDevice,
            _surface, out vk.SurfaceCapabilitiesKHR capabilities_khr);

        SurfaceCapabilities capabilities = Unsafe.As<vk.SurfaceCapabilitiesKHR, SurfaceCapabilities>(ref capabilities_khr);

        _resolution = capabilities.CurrentExtent;

        bool update_resolution = _swapResolution  != _resolution ;
        bool update_clip       = _swapClipped     != _clipped    ;
        bool update_format     = _swapFormat      != _colorFormat;
        bool update_space      = _swapColorSpace  != _colorSpace ;
        bool update_present    = _swapPresentMode != _presentMode;

        
        if (update_resolution || update_clip || update_format || update_space || update_present)
        {
            _resizeFinish.Reset();
            _renderGate.WaitOne();
            WaitForRenders(); // wait all frames to be rendered
            if (update_resolution && updateVideoSettings)
            {
                Video.SetResolutionAutoChange(_resolution);
            }
            
            RecreateSwapchain(in capabilities);

            _renderGate.ReleaseMutex();
            _resizeFinish.Set();
            return update_resolution;
        }

        return true;
    }

    public void WaitResizeFinished() => _resizeFinish.WaitOne();

    public void Present(ImageSubresource render)
    {
        _renderGate.WaitOne();
        if (_deleted) return;
        
        SlimFence     frame_fence                  = _fences[GetFrameInFlightFenceIndex(_current_frame)];
        SlimSemaphore image_availability_semaphore = _semaphores[GetImageAvailableSemaphoreIndex(_current_frame)];
        SlimSemaphore render_finished_semaphore    = _semaphores[GetRenderFinishedSemaphoreIndex(_current_frame)];

        
        frame_fence.Wait(_device);
        
        u32 swap_image_index = default;
        _swapchainExtension.AcquireNextImage(_device, _swap,
            u64.MaxValue, image_availability_semaphore, default, ref swap_image_index);

        u32 image_in_flight_fence_index = GetImageInFlightFenceIndex(swap_image_index);

        SlimFence image_fence = _fences[image_in_flight_fence_index];


        if (image_fence.Handle.Handle != 0)
        {
            image_fence.Wait(_device);
        }

        _fences[image_in_flight_fence_index] = frame_fence;
        frame_fence.Reset(_device);

        SlimCommandBuffer cmd = _commands[_current_frame];
        cmd.Reset();

        // todo: cache command
        using (CommandRecorder recorder = new(cmd, CommandBufferUsageFlags.OneTimeSubmit))
        {
            Image swap_image = _swapImages[_current_frame];

            vk.ImageSubresourceRange swap_resource_range = new(
                aspectMask: vk.ImageAspectFlags.ImageAspectColorBit,
                baseArrayLayer: 0U,
                layerCount: 1U,
                baseMipLevel: 0U,
                levelCount: 1U
            );

            vk.ImageMemoryBarrier ready_transfer_barrier;
            unsafe
            {
                ready_transfer_barrier = new vk.ImageMemoryBarrier(
                    srcAccessMask: vk.AccessFlags.AccessNoneKhr,
                    dstAccessMask: vk.AccessFlags.AccessTransferWriteBit,

                    oldLayout: vk.ImageLayout.PresentSrcKhr,
                    newLayout: vk.ImageLayout.TransferDstOptimal,

                    srcQueueFamilyIndex: 0,
                    dstQueueFamilyIndex: 0,

                    image: (SlimImage)swap_image,
                    subresourceRange: swap_resource_range
                );
            }

            PipelineStageFlags from_stage = PipelineStageFlags.TopOfPipe;

            if (!_swapchainsImagesPipelined[_current_frame])
            {
                ready_transfer_barrier.OldLayout = vk.ImageLayout.Undefined;
                from_stage = PipelineStageFlags.Transfer;
                _swapchainsImagesPipelined[_current_frame] = true;
            }

            vk.ImageMemoryBarrier ready_present_barrier = ready_transfer_barrier with
            {
                SrcAccessMask = vk.AccessFlags.AccessTransferWriteBit,
                DstAccessMask = vk.AccessFlags.AccessNone,

                OldLayout = vk.ImageLayout.TransferDstOptimal,
                NewLayout = vk.ImageLayout.PresentSrcKhr,
            };

            recorder.PipelineBarrier( // make swap image transferable
                sourceStageMask: from_stage,
                destinationStageMask: PipelineStageFlags.Transfer,
                imageMemoryBarriers: ready_transfer_barrier.AsSpan()
            );
            swap_image.ApplyPipelineBarrier(ready_transfer_barrier.NewLayout);

            if (swap_image.IsDeleted)
            {
                Log.Info("SWAP IMAGE DELETED !");
            }
            
            if (render.Image.IsDeleted)
            {
                Log.Info("RENDER IMAGE DELETED !");
            }

            recorder.Blit(source: render.Image, destination: swap_image);

            recorder.PipelineBarrier( // make swap image presentable
                sourceStageMask: PipelineStageFlags.Transfer,
                destinationStageMask: PipelineStageFlags.ColorAttachmentOutput,
                imageMemoryBarriers: ready_present_barrier.AsSpan()
            );
            swap_image.ApplyPipelineBarrier(ready_present_barrier.NewLayout);
        }

        _queue.Submit(
            waitSemaphores: image_availability_semaphore.AsSpan(),
            waitStage: PipelineStageFlags.ColorAttachmentOutput,
            commandBuffers: cmd.AsSpan(),
            signalSemaphores: render_finished_semaphore.AsSpan(),
            signalFence: frame_fence
        );

        _queue.Present(_swapchainExtension,
            waitSemaphores: render_finished_semaphore.AsSpan(),
            swapchains: _swap.AsSpan(),
            imageIndices: swap_image_index.AsSpan()
        );
        
        
        Graphics.FrameIndex = _current_frame = (_current_frame + 1) % MAX_IMAGES_IN_FLIGHT;
        
        _renderGate.ReleaseMutex();
    }

    public void WaitForRenders()
    {
        Span<SlimFence> fences_to_wait = stackalloc SlimFence[(i32)MAX_IMAGES_IN_FLIGHT];

        u32 fences_count = 0;

        for (u32 i = 0; i < MAX_IMAGES_IN_FLIGHT; i++)
        {
            u32 fence_index = GetImageInFlightFenceIndex(i);
            SlimFence fence = _fences[fence_index];
            if (fence.Handle.Handle != 0)
            {
                fences_to_wait[(i32)fences_count] = fence;
                fences_count++;
            }
        }
        
        if (fences_count != 0)
        {
            SlimFence.WaitAll(_device, fences_to_wait, fences_count);
        }
    }

    private unsafe void RecreateSwapchain(in SurfaceCapabilities capabilities)
    {
        vk.SwapchainKHR previous = _swap;

        _swapResolution  = _resolution ;
        _swapClipped     = _clipped    ;
        _swapFormat      = _colorFormat;
        _swapColorSpace  = _colorSpace ;
        _swapPresentMode = _presentMode;

        ref readonly vk.Extent2D extent = ref Unsafe.As<Vector2D<u32>, vk.Extent2D>(ref _swapResolution);

        if (extent.Width < 16 || extent.Height < 16) return;

        u32 queue_family = 0;
        u32 image_count = GetImageCount(in capabilities);

        vk.SwapchainCreateInfoKHR swapchain_info = new(
            surface              : _surface                ,
            minImageCount        : image_count             ,
            imageFormat          : _swapFormat.ToVk()      ,
            imageColorSpace      : _swapColorSpace.ToVk()  ,
            imageExtent          : extent                  ,
            imageUsage           : SWAP_IMAGE_USAGES.ToVk(),
            imageSharingMode     : SWAP_SHARING            ,
            pQueueFamilyIndices  : &queue_family           ,
            queueFamilyIndexCount: 1                       ,
            preTransform         : SWAP_PRE_TRANSFORM      ,
            compositeAlpha       : SWAP_ALPHA_COMPOSITING  ,
            presentMode          : _swapPresentMode.ToVk() ,
            clipped              : _swapClipped            ,
            imageArrayLayers     : SWAP_ARRAY_LAYERS       ,
            oldSwapchain         : previous                ,
            flags                : SWAP_FLAGS
        );
        _swapchainExtension.CreateSwapchain(_device, in swapchain_info, null, out _swap);
        
        u32 previous_swap_image_count = _swapImageCount;
        /*for (u32 i = 0; i < previous_swap_image_count; i++) // signal all the semaphores.
        {
            u32 swap_index = 0;
            _swapchainExtension.AcquireNextImage(_device, _swap,
                u64.MaxValue, _semaphores[GetImageAvailableSemaphoreIndex(i)], default, ref swap_index);
        }*/
        
        DestroyViews(previous_swap_image_count);
        DestroySwapchain(previous);

        u32 new_swap_image_count = 0;
        khr.KhrSwapchainOverloads.GetSwapchainImages(_swapchainExtension, _device, _swap, new_swap_image_count.AsSpan(), Span<Silk.NET.Vulkan.Image>.Empty);

        if (new_swap_image_count != previous_swap_image_count)
        {
            _swapImageCount   = new_swap_image_count;
            _swapImages = new Image[new_swap_image_count];
        }

        Span<vk.Image> swap_images = stackalloc vk.Image[(i32)new_swap_image_count];
        khr.KhrSwapchainOverloads.GetSwapchainImages(_swapchainExtension, _device, _swap, new_swap_image_count.AsSpan(), swap_images);

        for (i32 i = 0; i < _swapImageCount; i++)
        {
            Image atom_image = new(
                resolution   : _swapResolution       ,
                format       : _swapFormat           ,
                tiling       : SWAP_TILING           ,
                usage        : SWAP_IMAGE_USAGES     ,
                queueFamilies: queue_family.AsSpan(),
                mipLevels    : SWAP_MIP_LEVELS       ,
                arrayLayers  : SWAP_ARRAY_LAYERS     ,
                multisampling: SWAP_SAMPLES          ,
                sharingMode  : SWAP_SHARING          ,
                layout       : SWAP_LAYOUT           ,
                image        : new Ownership<SlimImage>(swap_images[i], owned: false),
                device       : _device
            );

            _swapImages[i] = atom_image;

            _swapchainsImagesPipelined[i] = false;
        }

        Graphics.FrameIndex = _current_frame = 0;
    }
    
    private unsafe void DeleteSurface(ref vk.SurfaceKHR surface)
    {
        if (_surfaceExtension != null!)
        {
            _surfaceExtension.DestroySurface(VK.Instance, surface, null);
        }
    }
    
    private unsafe void DestroySwapchain(vk.SwapchainKHR swapchain)
    {
        _swapchainExtension.DestroySwapchain(_device, swapchain, null);
    }

    private void DestroyViews(u32 count)
    {
        for (i32 i = 0; i < count; i++)
        {
            _swapImages[i].Delete();
        }
    }
    
    private static u32 GetImageCount(in SurfaceCapabilities capabilities)
    {
        ref readonly u32 min_img_count = ref capabilities.ImageCount.Min;
        ref readonly u32 max_img_count = ref capabilities.ImageCount.Max;
        u32 image_count = min_img_count + 1;
        if (max_img_count > 0)
        {
            image_count = Math.Clamp(
                value: image_count  ,
                min  : min_img_count,
                max  : max_img_count
            );
        }

        return image_count;
    }

    private void LogSurfaceInfos()
    {
        StringBuilder debug_info = new("`Creation of a viewport with available settings:\n");
        debug_info.Append("* Present modes: ");
        debug_info.AppendJoin(", ", _availablePresentModes);
        debug_info.Append("\n* Colors       : ");
        debug_info.AppendJoin(", ", _availableFormats.Select(
            kvp => $"{kvp.Key} {{ {string.Join(", ", kvp.Value.Select(s => s))} }}"
        ));
        debug_info.Append($"\n* Vertical Sync: {(IsVSyncSupported(out PresentMode vsync_mode) ? $"Supported ({vsync_mode})" : "Unsupported")}");
        debug_info.Append('`');
        Log.Info(debug_info);
    }

    private HashSet<PresentMode> AssignPresentModes()
    {
        u32 present_mode_count = 0;

        // the first call must contain no pointer, the driver will force an
        // incomplete status even with a pre allocated buffer that fits the counts
        khr.KhrSurfaceOverloads.GetPhysicalDeviceSurfacePresentModes(_surfaceExtension,
            _physicalDevice, _surface.Data, 
            present_mode_count.AsSpan(), Span<vk.PresentModeKHR>.Empty 
        );

        Span<vk.PresentModeKHR> present_modes = stackalloc vk.PresentModeKHR[(i32)present_mode_count];

        khr.KhrSurfaceOverloads.GetPhysicalDeviceSurfacePresentModes(_surfaceExtension,
            _physicalDevice, _surface.Data, 
            present_mode_count.AsSpan(), present_modes
        );

        _availablePresentModes = new HashSet<PresentMode>(capacity: (i32)present_mode_count);
        for (int i = 0; i < present_mode_count; i++)
        {
            _availablePresentModes.Add(item: present_modes[i].ToAtom());
        }
        
        return _availablePresentModes;
    }

    private Dictionary<ColorSpace, HashSet<ImageFormat>> AssignColors()
    {
        Span<vk.SurfaceFormatKHR> formats = stackalloc vk.SurfaceFormatKHR[32]; 
        u32 format_count = 0;
        
        khr.KhrSurfaceOverloads.GetPhysicalDeviceSurfaceFormats(_surfaceExtension,
            _physicalDevice, _surface.Data, format_count.AsSpan(), Span<vk.SurfaceFormatKHR>.Empty);

        khr.KhrSurfaceOverloads.GetPhysicalDeviceSurfaceFormats(_surfaceExtension,
            _physicalDevice, _surface.Data, format_count.AsSpan(), formats);

        Dictionary<ColorSpace, HashSet<ImageFormat>> by_space = new (capacity: (i32)format_count);

        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        for (i32 i = 0; i < format_count; i++)
        {
            ref readonly ColorSpace  color_space  = ref Unsafe.As<vk.ColorSpaceKHR, ColorSpace >(ref formats[i].ColorSpace);
            ref readonly ImageFormat color_format = ref Unsafe.As<vk.Format, ImageFormat>(ref formats[i].Format);

            if (by_space.TryGetValue(color_space, out HashSet<ImageFormat>? by_space_formats))
            {
                by_space_formats.Add(color_format);
            }
            else
            {
                by_space.Add(color_space, new HashSet<ImageFormat> { color_format });
            }
        }

        _availableFormats = new Dictionary<ColorSpace, HashSet<ImageFormat>>(capacity: by_space.Count);

        foreach ((ColorSpace color_space, HashSet<ImageFormat> color_formats) in by_space)
        {
            _availableFormats.Add(color_space, color_formats);
        }

        return _availableFormats;
    }

    public ImageUsageFlags AssignSupportedUsages()
    {
        vk.SurfaceCapabilitiesKHR vk_caps = default;
        khr.KhrSurfaceOverloads.GetPhysicalDeviceSurfaceCapabilities(_surfaceExtension, _physicalDevice, _surface, vk_caps.AsSpan());

        ref readonly SurfaceCapabilities caps = ref Unsafe.As<vk.SurfaceCapabilitiesKHR, SurfaceCapabilities>(ref vk_caps);

        return _supportedUsageFlags = caps.SupportedUsageFlags;
    }
    
    // array packs both frame in flight and image in flight arrays into one of 2*MAX_FRAMES_IN_FLIGHT_COUNT
    // frame in flight being the MAX_FRAMES_IN_FLIGHT_COUNT first ones and images in flight
    // the MAX_FRAMES_IN_FLIGHT_COUNT last ones (ex: with MAX_FRAMES_IN_FLIGHT_COUNT = 8, 0..7 are frame in flight and
    // 8..15 are image in flight
    private static u32 GetFrameInFlightFenceIndex(u32 frameIndex) => frameIndex;
    private static u32 GetImageInFlightFenceIndex(u32 frameIndex) => frameIndex + MAX_IMAGES_IN_FLIGHT;
    
    private static u32 GetImageAvailableSemaphoreIndex(u32 frameIndex) => frameIndex;
    private static u32 GetRenderFinishedSemaphoreIndex(u32 frameIndex) => frameIndex + MAX_IMAGES_IN_FLIGHT;
}