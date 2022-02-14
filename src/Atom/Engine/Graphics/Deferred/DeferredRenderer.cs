using System.Diagnostics;
using System.Runtime.CompilerServices;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Atom.Engine;

public class DeferredRenderer
{
    private struct SurfaceCapabilities
    {
        public Clamp<uint> ImageCount;
        public Vector2D<uint> CurrentExtent;
        public ClampVector2D<uint> ImageVectors;
        public uint MaxImageArrayLayers;
        public SurfaceTransformFlagsKHR SupportedTransforms;
        public SurfaceTransformFlagsKHR CurrentTransform;
        public CompositeAlphaFlagsKHR SupportedCompositeAlpha;
        public ImageUsageFlags SupportedUsageFlags;
    }

    private struct RenderPassClear
    {
        public ClearColorValue GBufferAlbedo = new ClearColorValue(0.0F, 0.0F, 0.0F, 0.0F);
        public ClearColorValue GBufferNormal = new ClearColorValue(0.0F, 0.0F, 0.0F, 0.0F);
        public ClearColorValue GBufferPosition = new ClearColorValue(0.0F, 0.0F, 0.0F, 0.0F);
        public ClearDepthStencilValue GBufferDepth = new ClearDepthStencilValue(1.0F);
    }

#region Configuration

    // -- Defaults
    public const Format DEFAULT_COLOR_FORMAT = Format.B8G8R8A8Unorm/*B8G8R8A8Srgb*/;
    private const ColorSpaceKHR DEFAULT_COLOR_SPACE = ColorSpaceKHR.ColorSpaceSrgbNonlinearKhr/*ColorSpaceSrgbNonlinearKhr*/;
    private const ImageUsageFlags SWAPCHAIN_IMAGE_USAGES = ImageUsageFlags.ImageUsageColorAttachmentBit;
    private const CompositeAlphaFlagsKHR ALPHA_COMPOSITING = CompositeAlphaFlagsKHR.CompositeAlphaOpaqueBitKhr;
    private const PresentModeKHR DEFAULT_PRESENT_MODE = PresentModeKHR.PresentModeImmediateKhr;
    private const bool DEFAULT_CLIPPED = false;
    private const uint ARRAY_LAYERS = 1U; // for VR: 2U
    private const SwapchainCreateFlagsKHR SWAPCHAIN_FLAGS = 0;

    // -- Frames & Images
    public const uint MAX_FRAMES_IN_FLIGHT_COUNT = 3U;
    private const uint RESULT_IMAGE_COUNT = 1U;
    private const uint RESULT_VIEW_COUNT = 1U;
    private const uint DEFERRED_IMAGE_COUNT = 3U; // gbuffer colors + gbuffer depth + lit result
    private const uint DEFERRED_VIEW_COUNT = 5U;


    private static readonly Pin<RenderPassClear> RENDER_PASS_CLEAR = new RenderPassClear();
    

    private const uint MAX_VIEW_COUNT = MAX_FRAMES_IN_FLIGHT_COUNT * (RESULT_VIEW_COUNT + DEFERRED_VIEW_COUNT);
    private const uint MAX_IMAGE_COUNT = MAX_FRAMES_IN_FLIGHT_COUNT * (RESULT_IMAGE_COUNT + DEFERRED_IMAGE_COUNT);
    
#endregion

    private Device _device;
    private PhysicalDevice _physicalDevice;
    private QueueFamily _renderFamily;
    private Queue _queue;

    private Silk.NET.Vulkan.RenderPass _renderPass;
    private uint _subpassIndex = 0U;
    
    private SurfaceKHR _surface;
    private KhrSurface _surfaceExtension;
    private SwapchainKHR _swapchain;
    private KhrSwapchain _swapchainExtension;
    
    // framebuffer
    private Image[] _images = new Image[MAX_IMAGE_COUNT];
    private ImageView[] _views = new ImageView[MAX_VIEW_COUNT];
    private Framebuffer[] _framebuffers = new Framebuffer[MAX_FRAMES_IN_FLIGHT_COUNT];
    
    // synchronisation
    // 0..7: fences in flight, 8..15: images in flight
    private SlimFence[] _fences = new SlimFence[MAX_FRAMES_IN_FLIGHT_COUNT * 2]; 
    // 0..7: image available, 8..15: render finished
    private Semaphore[] _semaphores = new Semaphore[MAX_FRAMES_IN_FLIGHT_COUNT * 2];
    
    // commands
    private SlimCommandPool _commandPool;
    private SlimCommandBuffer[] _commands = new SlimCommandBuffer[MAX_FRAMES_IN_FLIGHT_COUNT];
    
    // memory storage for all the framebuffers
    private vk.DeviceMemory _framebuffersMemory;
    
    // surface
    private Vector2D<uint> _extent;
    private Format _colorFormat = DEFAULT_COLOR_FORMAT;
    private ColorSpaceKHR _colorSpace = DEFAULT_COLOR_SPACE;
    private PresentModeKHR _presentMode = DEFAULT_PRESENT_MODE;
    private bool _clip = DEFAULT_CLIPPED;
    
    private NDCDrawer _screenDrawer = null!;
    private GBufferDrawer _gbufferDrawer = null!;

    private uint _swapchainImageCount;
    private uint _frameIndex = 0;
    private Format _depthFormat;
    
    
    public Format ColorFormat
    {
        get => _colorFormat;
        set
        {
            unsafe
            {
                uint format_mode_count = default;
                _surfaceExtension.GetPhysicalDeviceSurfaceFormats(_physicalDevice, _surface,
                    ref format_mode_count, null);
                Span<SurfaceFormatKHR> formats = stackalloc SurfaceFormatKHR[(int)format_mode_count];
                _surfaceExtension.GetPhysicalDeviceSurfaceFormats(_physicalDevice, _surface,
                    &format_mode_count, formats);

                SurfaceFormatKHR available = default;
                for (int i = 0; i < format_mode_count; i++)
                {
                    if (formats[i].Format == value)
                    {
                        available = formats[i];
                        break;
                    }
                }
                
                if (available.Format == Format.Undefined)
                {
                    throw new Exception($"Color format {value} is not supported by the rendering surface.");
                }
                else
                {
                    _colorFormat = value;
                    _colorSpace = available.ColorSpace;
                }
            }
        }
    }
    /// <summary>
    /// Get only, use <see cref="ColorFormat"/> setter to set the color space (it is automatically set by the color format compatibility)
    /// </summary>
    public ColorSpaceKHR ColorSpace => _colorSpace;
    public PresentModeKHR PresentMode
    {
        get => _presentMode;
        set
        {
            unsafe
            {
                uint present_mode_count = default;
                _surfaceExtension.GetPhysicalDeviceSurfacePresentModes(_physicalDevice, _surface,
                    ref present_mode_count, null);
                Span<PresentModeKHR> present_modes = stackalloc PresentModeKHR[(int)present_mode_count];
                _surfaceExtension.GetPhysicalDeviceSurfacePresentModes(_physicalDevice, _surface,
                    &present_mode_count, present_modes);

                for (int i = 0; i < present_mode_count; i++)
                {
                    if (present_modes[i] == value)
                    {
                        _presentMode = value;
                        return;
                    }
                }

                throw new Exception($"Present mode {value} is not supported by the rendering surface.");
            }
        }
    }

    public unsafe void Setup(
        Device device,
        PhysicalDevice physicalDevice,
        KhrSurface surfaceExtension,
        SurfaceKHR surface,
        QueueFamily renderFamily,
        Queue queue)
    {
        _device = device;
        _physicalDevice = physicalDevice;
        _surfaceExtension = surfaceExtension;
        _surface = surface;
        _renderFamily = renderFamily;
        _screenDrawer = new NDCDrawer(MAX_FRAMES_IN_FLIGHT_COUNT, device);
        _queue = queue;

        if (!VK.API.TryGetDeviceExtension(VK.Instance, device, out _swapchainExtension))
        {
            throw new ExtensionNotFoundException(
                $"Swapchain couldn't be created, is {KhrSwapchain.ExtensionName} present and enabled?");
        }

        ColorFormat = _colorFormat;

        SemaphoreCreateInfo semaphore_info = new(flags: 0);
        for (int i = 0; i < MAX_FRAMES_IN_FLIGHT_COUNT; i++)
        {
            _fences[i] = new SlimFence(_device, signaled: true);
            VK.API.CreateSemaphore(_device, in semaphore_info, null, out _semaphores[i]);
        }

        for (int i = 0; i < MAX_FRAMES_IN_FLIGHT_COUNT; i++) // 2x more semaphores than frames in flight.
        {
            VK.API.CreateSemaphore(_device, in semaphore_info, null, out _semaphores[i + MAX_FRAMES_IN_FLIGHT_COUNT]);
        }
        
        DeferredRenderPass.CreateRenderPass(_device, _physicalDevice, out _renderPass, out _depthFormat);
        Graphics.MainRenderPass = _renderPass;
        _gbufferDrawer = new GBufferDrawer(MAX_FRAMES_IN_FLIGHT_COUNT, device);

        _commandPool = new SlimCommandPool(device, renderFamily.Index);
        _commandPool.AllocateCommandBuffers(device, CommandBufferLevel.Primary, MAX_FRAMES_IN_FLIGHT_COUNT,
            _commands.AsSpan(0));

        Graphics.SetRenderReady();
    }

    public void WaitForRenders()
    {
        int base_index = (int)GetFrameInFlightFenceIndex(0U);

        SlimFence.WaitAll(_device, _fences.AsSpan(base_index, (int)MAX_FRAMES_IN_FLIGHT_COUNT));
    }

    public unsafe void Render()
    {
        // if the surface is not valid for rendering, throw the render out.
        if (_extent.X == 0 || _extent.Y == 0) return;

        SlimFence frame_fence = _fences[GetFrameInFlightFenceIndex(_frameIndex)];
        Semaphore image_availability_semaphore = _semaphores[GetImageAvailableSemaphoreIndex(_frameIndex)];
        Semaphore render_finished_semaphore = _semaphores[GetRenderFinishedSemaphoreIndex(_frameIndex)];

        frame_fence.Wait(_device);
        
        uint swap_image_index = default;
        _swapchainExtension.AcquireNextImage(_device, _swapchain, 
        ulong.MaxValue, image_availability_semaphore, default, ref swap_image_index);

        uint image_in_flight_fence_index = GetImageInFlightFenceIndex(swap_image_index);
        uint command_buffer_index = GetCommandBufferIndex(swap_image_index);
        
        SlimFence image_fence = _fences[image_in_flight_fence_index];
        SlimCommandBuffer command = _commands[command_buffer_index];

        if (image_fence.Handle.Handle != 0)
        {
            image_fence.Wait(_device);
        }

        _fences[image_in_flight_fence_index] = frame_fence;
        frame_fence.Reset(_device);

        PipelineStageFlags wait_dst_draw = PipelineStageFlags.PipelineStageColorAttachmentOutputBit;
        
        SubmitInfo draw_submit = new(
            pWaitDstStageMask: &wait_dst_draw,
            waitSemaphoreCount: 1,
            pWaitSemaphores: &image_availability_semaphore,
            signalSemaphoreCount: 1,
            pSignalSemaphores: &render_finished_semaphore,
            commandBufferCount: 1,
            pCommandBuffers: (vk.CommandBuffer*)&command
        );
        VK.API.QueueSubmit(
            _queue,
            submitCount: 1,
            in draw_submit,
            frame_fence.Handle
        );

        SwapchainKHR swapchain = _swapchain;
        
        PresentInfoKHR present_info = new(
            waitSemaphoreCount: 1,
            pWaitSemaphores: &render_finished_semaphore,
            swapchainCount: 1,
            pSwapchains: &swapchain,
            pImageIndices: &swap_image_index

        );
        _swapchainExtension.QueuePresent(_queue, in present_info);
        
        _frameIndex = ++_frameIndex % MAX_FRAMES_IN_FLIGHT_COUNT;
    }

    public unsafe bool Update(bool updateVideoSettings)
    {
        WaitForRenders();

        _surfaceExtension.GetPhysicalDeviceSurfaceCapabilities(
            _physicalDevice,
            _surface,
            out SurfaceCapabilitiesKHR capabilities_khr
        );

        SurfaceCapabilities capabilities = Unsafe.As<SurfaceCapabilitiesKHR, SurfaceCapabilities>(ref capabilities_khr);

        Vector2D<uint> extent = _extent = GetExtent(
            capabilities: in capabilities,
            out bool isValidExtent);

        if (!isValidExtent)
        {
            // do not do anything if invalid extent
            Log.Warning($"Unable to use extent {_extent.X}x{_extent.Y}");
            return false;
        }

        uint image_count = GetImageCount(in capabilities);
        uint fam_index = _renderFamily.Index;

        SwapchainKHR previous = _swapchain;

        SwapchainCreateInfoKHR swapchain_info = new(
            surface: _surface,
            minImageCount: image_count,
            imageFormat: _colorFormat,
            imageColorSpace: _colorSpace,
            imageExtent: *(Extent2D*)&extent,
            imageUsage: SWAPCHAIN_IMAGE_USAGES,
            imageSharingMode: SharingMode.Exclusive, // todo: screenshots, ...
            pQueueFamilyIndices: &image_count,
            queueFamilyIndexCount: 1,
            preTransform: SurfaceTransformFlagsKHR.SurfaceTransformIdentityBitKhr,
            compositeAlpha: ALPHA_COMPOSITING,
            presentMode: _presentMode,
            clipped: _clip,
            imageArrayLayers: ARRAY_LAYERS,
            oldSwapchain: previous,
            flags: SWAPCHAIN_FLAGS
        );
        _swapchainExtension.CreateSwapchain(_device, in swapchain_info, null, out _swapchain);
        DestroySwapchain(previous);

        uint previous_swap_image_count = _swapchainImageCount;
        uint new_swap_image_count = _swapchainImageCount = image_count;

        uint swap_image_count = 0;
        _swapchainExtension.GetSwapchainImages(_device, _swapchain, ref swap_image_count, null);
        Span<Image> swap_images = stackalloc Image[(int)swap_image_count];
        _swapchainExtension.GetSwapchainImages(_device, _swapchain, &swap_image_count, swap_images);

        if (previous_swap_image_count != 0)
        {
            CleanRenderObjects(previous_swap_image_count);
        }

        // image creation and memory allocation / binding
        Span<MemoryRequirements> requirements = 
            stackalloc MemoryRequirements[(int)DEFERRED_IMAGE_COUNT * (int)new_swap_image_count];
        ulong required_bytes_to_allocate = 0UL;
        
        for (uint i = 0U; i < new_swap_image_count; i++)
        {
            uint image_index = GetImageBaseIndex(i);

            CreateImages(i, extent, swap_images[(int)i]);
            
            // get required memory allocations properties
            for (int j = 0; j < DEFERRED_IMAGE_COUNT; j++)
            {
                int requirements_index = (int)(i * DEFERRED_IMAGE_COUNT) + j;
                VK.API.GetImageMemoryRequirements(
                    _device, 
                    _images[image_index + 1 + j], 
                    out requirements[requirements_index]
                );
                ref MemoryRequirements reqs = ref requirements[requirements_index];
                required_bytes_to_allocate += reqs.Size;
            }
        }

        AllocateImagesMemory(new_swap_image_count, required_bytes_to_allocate, requirements);
        
        // views and framebuffer creation
        for (uint i = 0U; i < new_swap_image_count; i++)
        {
            uint view_index = GetViewBaseIndex(i);
            CreateViews(i);

            ReadOnlySpan<ImageView> framebuffer_views = _views.AsSpan((int)view_index + 1, (int)DEFERRED_VIEW_COUNT);

            fixed (ImageView* p_attachments = framebuffer_views)
            {
                FramebufferCreateInfo framebuffer_create_info = new(
                    attachmentCount: DEFERRED_VIEW_COUNT,
                    pAttachments: p_attachments,
                    renderPass: _renderPass,
                    width: extent.X,
                    height: extent.Y,
                    layers: ARRAY_LAYERS
                );

                VK.API.CreateFramebuffer(_device, in framebuffer_create_info, null, out _framebuffers[i]);
            }
        }

        Span<ImageView> swapchain_views = stackalloc ImageView[(int)new_swap_image_count];
        for (uint i = 0; i < new_swap_image_count; i++)
        {
            swapchain_views[(int)i] = _views[GetViewBaseIndex(i)];
        }

        Extent2D vk_extent = new(extent.X, extent.Y);
        
        _screenDrawer.Resize(extent, swapchain_views);
        _gbufferDrawer.Resize(extent);
        
        for (uint i = 0; i < new_swap_image_count; i++)
        {
            BuildCommands(swapImageIndex: i, extent: vk_extent);
        }
        
        if (updateVideoSettings)
        {
            Video.Resolution = extent;
        }

        return true;
    }

    private void CleanRenderObjects(uint count)
    {
        ResetCommandBuffers();
        CleanFramebuffers(count);
        CleanViews(count);
        CleanImages(count);
        CleanMemory();
    }
    
    private void ResetCommandBuffers() => VK.API.ResetCommandPool(_device, _commandPool, 0);

    private void CleanMemory() => VK.API.FreeMemory(_device, _framebuffersMemory, ReadOnlySpan<AllocationCallbacks>.Empty);
    
    private void CleanFramebuffers(uint count)
    {
        for (int i = 0; i < count; i++)
        {
            VK.API.DestroyFramebuffer(_device, _framebuffers[i], ReadOnlySpan<AllocationCallbacks>.Empty);
        }
    }
    
    private void CleanViews(uint count)
    {
        for (uint i = 0; i < count * (RESULT_VIEW_COUNT + DEFERRED_VIEW_COUNT); i++)
        {
            VK.API.DestroyImageView(_device, _views[i], ReadOnlySpan<AllocationCallbacks>.Empty);
        }
    }
    
    private void CleanImages(uint count)
    {
        for (uint i = 0U; i < count * (RESULT_IMAGE_COUNT + DEFERRED_IMAGE_COUNT); i++)
        {
            // do not delete swapchain's image (result image), it is automatically done when freeing it.
            if (i % (RESULT_IMAGE_COUNT + DEFERRED_IMAGE_COUNT) > 0)
            {
                // memory is one unique allocation for all images, it requires to be deleted in another call.
                VK.API.DestroyImage(_device, _images[i], ReadOnlySpan<AllocationCallbacks>.Empty);
            }
        }
    } 
    

    public void Dispose()
    {
        _gbufferDrawer.Dispose();
        _screenDrawer.Dispose();

        CleanRenderObjects(_swapchainImageCount);
        
        VK.API.DestroyCommandPool(_device, _commandPool, ReadOnlySpan<AllocationCallbacks>.Empty);

        for (int i = 0; i < MAX_FRAMES_IN_FLIGHT_COUNT; i++)
        {
            _fences[i].Destroy(_device);
            //VK.API.DestroyFence(_device, _fences[i], ReadOnlySpan<AllocationCallbacks>.Empty);
            VK.API.DestroySemaphore(_device, _semaphores[i], ReadOnlySpan<AllocationCallbacks>.Empty);
        }
        for (int i = 0; i < MAX_FRAMES_IN_FLIGHT_COUNT; i++) // actually 16 semaphores to destroy.
        {
            VK.API.DestroySemaphore(_device, _semaphores[i + MAX_FRAMES_IN_FLIGHT_COUNT], ReadOnlySpan<AllocationCallbacks>.Empty);
        }
        
        DestroySwapchain(_swapchain);
        
        VK.API.DestroyRenderPass(_device, _renderPass, ReadOnlySpan<AllocationCallbacks>.Empty);
        
        GC.SuppressFinalize(this);
    }

    ~DeferredRenderer() => Dispose();

    private unsafe void DestroySwapchain(SwapchainKHR? swapchain)
    {
        _swapchainExtension.DestroySwapchain(_device, swapchain.Value, null);
    }
    
    private unsafe void BuildCommands(uint swapImageIndex, Silk.NET.Vulkan.Extent2D extent)
    {
        Silk.NET.Vulkan.Rect2D area = new(extent: extent);
        SlimCommandBuffer cmd = _commands[swapImageIndex];

        CommandBufferBeginInfo begin = new(flags: 0);
        VK.API.BeginCommandBuffer(cmd, in begin);
        {
            RenderPassBeginInfo pass_info = new RenderPassBeginInfo(
                renderPass: _renderPass,
                renderArea: area,
                framebuffer: _framebuffers[swapImageIndex],
                clearValueCount: 4,
                pClearValues: (ClearValue*)(RenderPassClear*)RENDER_PASS_CLEAR
            );
            VK.API.CmdBeginRenderPass(cmd, in pass_info, SubpassContents.Inline);
            // draw meshes in gbuffer
            
            VK.API.CmdNextSubpass(cmd, SubpassContents.Inline);
            // draw lit render
            int gbuffer_views_index = (int)GetViewBaseIndex(swapImageIndex);
            _gbufferDrawer.CmdComputeGBuffer(cmd, swapImageIndex, 
                _views.AsSpan()[gbuffer_views_index..(gbuffer_views_index+4)]);
            VK.API.CmdEndRenderPass(cmd);
            
            // draw on swapchain image as a fullscreen image
            _screenDrawer.CmdDrawView(cmd, swapImageIndex, _views[GetViewBaseIndex(swapImageIndex) + DEFERRED_VIEW_COUNT]);
        }
        VK.API.EndCommandBuffer(cmd);
    }

    private void CreateViews(uint swapImageIndex)
    {
        uint view_index = GetViewBaseIndex(swapImageIndex);
        uint image_index = GetImageBaseIndex(swapImageIndex);

        Image final_image = _images[image_index];
        Image gbuffer_main_image = _images[image_index + 1];
        Image gbuffer_depth_image = _images[image_index + 2];
        Image lit_image = _images[image_index + 3];

        unsafe Result create_view(Image image, Format format, ImageAspectFlags aspect, out ImageView view, uint layer = 0)
        {
            ImageViewCreateInfo info = new(
                flags: 0,
                image: image,
                format: format,
                viewType: ImageViewType.ImageViewType2D,
                components: new ComponentMapping(
                    r: ComponentSwizzle.Identity,
                    g: ComponentSwizzle.Identity,
                    b: ComponentSwizzle.Identity, 
                    a: ComponentSwizzle.Identity),
                subresourceRange: new ImageSubresourceRange(aspectMask: aspect, baseMipLevel: 0, levelCount: 1,
                    baseArrayLayer: layer, layerCount: 1)
            );
            return VK.API.CreateImageView(_device, in info, null, out view);
        }

        const Format GBUFFER_FORMAT = Format.R32G32B32A32Sfloat;
        
        // final
        create_view(
            final_image,
            _colorFormat, 
            ImageAspectFlags.ImageAspectColorBit, 
            out _views[view_index]
        );
        // albedo / luminance | normal / roughness / metalness | position / translucency
        for (uint i = 0; i < 3; i++) // 3 views for the main g buffer
        {
            create_view(gbuffer_main_image, 
                GBUFFER_FORMAT,
                ImageAspectFlags.ImageAspectColorBit, 
                out _views[view_index + 1 + i], 
                layer: i
            );
        }
        // depth
        create_view(
            gbuffer_depth_image,
            _depthFormat,
            ImageAspectFlags.ImageAspectDepthBit, 
            out _views[view_index + 4]
        );
        // final
        create_view(
            lit_image,
            GBUFFER_FORMAT,
            ImageAspectFlags.ImageAspectColorBit, 
            out _views[view_index + 5]
        );
    }
    
    private unsafe void AllocateImagesMemory(uint swap_image_count, ulong size, ReadOnlySpan<MemoryRequirements> requirements)
    {
        uint fam_index = _renderFamily.Index;
        
        // allocate the memory required for all the framebuffers
        MemoryAllocateInfo alloc_info = new(
            allocationSize: size,
            memoryTypeIndex: VK.FindMemoryType(
                physicalDevice: _physicalDevice, 
                typeFilter: requirements[0].MemoryTypeBits, // consider [0] as all other memory representation
                properties: MemoryPropertyFlags.DeviceLocal
            )
        );
        VK.API.AllocateMemory(_device, in alloc_info, null, out _framebuffersMemory);


        ulong offset = 0UL;
        // assign images memories
        for (uint i = 0; i < swap_image_count; i++)
        {
            uint image_base_index = GetImageBaseIndex(i);
            
            for (int j = 0; j < DEFERRED_IMAGE_COUNT; j++)
            {
                int image_index = (int)image_base_index + 1 + j;
                int requirements_index = (int)(i * DEFERRED_IMAGE_COUNT) + j;
                MemoryRequirements reqs = requirements[requirements_index];

                ulong mem_size = reqs.Size;
                VK.API.BindImageMemory(_device, _images[image_index], _framebuffersMemory, offset);
                offset += mem_size;
            }
        }
    }
    private unsafe void CreateImages(uint swapImageIndex, Vector2D<uint> extent, Image swapImage)
    {
        uint image_index = GetImageBaseIndex(swapImageIndex);
        ref uint width = ref extent.X;
        ref uint height = ref extent.Y;

        Extent3D size = new Extent3D(width, height, 1);

        uint fam_index = _renderFamily.Index;

        Result create_image(Format format, uint arrayLayers, ImageUsageFlags usages, out Image image)
        {
            uint f = fam_index;
            ImageCreateInfo gbuffer_main = new ImageCreateInfo( /* gBufferMainImage */ 
                flags: 0,
                imageType: ImageType.ImageType2D,
                format: format,
                extent: size,
                mipLevels: 1,
                arrayLayers: arrayLayers,
                samples: SampleCountFlags.SampleCount1Bit,
                tiling: ImageTiling.Optimal,
                usage: usages,
                initialLayout: ImageLayout.Undefined,
                queueFamilyIndexCount: 1,
                pQueueFamilyIndices: &f,
                sharingMode: SharingMode.Exclusive // only used for rendering
            );
            return VK.API.CreateImage(_device, in gbuffer_main, null, out image);
        }
        
        // create images
        // main gbuffer image: albedo/luminance + normal/roughness/metalness + position/translucency (3x, all RGBA32)
        _images[image_index] = swapImage;

        create_image( /* gBuffer main */
            format: Format.R32G32B32A32Sfloat, 
            arrayLayers: 3,
            usages: ImageUsageFlags.ImageUsageColorAttachmentBit | ImageUsageFlags.ImageUsageInputAttachmentBit,
            out _images[image_index + 1]
        );
        
        create_image( /* gBuffer depth */
            format: _depthFormat, 
            arrayLayers: 1,
            usages: ImageUsageFlags.ImageUsageDepthStencilAttachmentBit | ImageUsageFlags.ImageUsageInputAttachmentBit,
            out _images[image_index + 2]
        );
        
        create_image( /* lit */
            format: Format.R32G32B32A32Sfloat, 
            arrayLayers: 1,
            usages: ImageUsageFlags.ImageUsageColorAttachmentBit | ImageUsageFlags.ImageUsageSampledBit,
            out _images[image_index + 3]
        );
    }
 
    private static Vector2D<uint> GetExtent(in SurfaceCapabilities capabilities, out bool isValid)
    {
        // this is required in order to avoid the image creation crashing when the window extent
        // height is 0 and the width is being changed while the height being changed by the user.
        // yep ikr.
        const uint MIN_EXTENT = 16;
        const uint MIN_WIDTH = MIN_EXTENT;
        const uint MIN_HEIGHT = MIN_EXTENT;


        Vector2D<uint> extent = Vector2DExtender<uint>.Clamp(
            value: capabilities.CurrentExtent,
            min: capabilities.ImageVectors.Min,
            max: capabilities.ImageVectors.Max
        );

        isValid = extent.X > MIN_WIDTH && extent.Y > MIN_HEIGHT;

        return extent;
    }

    private static uint GetImageCount(in SurfaceCapabilities capabilities)
    {
        uint min_img_count = capabilities.ImageCount.Min;
        uint max_img_count = capabilities.ImageCount.Max;
        uint image_count = min_img_count + 1;
        if (max_img_count > 0)
        {
            image_count = Math.Clamp(
                value: image_count,
                min: min_img_count,
                max: max_img_count
            );
        }

        return image_count;
    }
    
    private static uint GetImageBaseIndex(uint i) => i * (RESULT_IMAGE_COUNT + DEFERRED_IMAGE_COUNT);
    private static uint GetViewBaseIndex(uint i) => i * (RESULT_VIEW_COUNT + DEFERRED_VIEW_COUNT);

    // array packs both frame in flight and image in flight arrays into one of 2*MAX_FRAMES_IN_FLIGHT_COUNT
    // frame in flight being the MAX_FRAMES_IN_FLIGHT_COUNT first ones and images in flight
    // the MAX_FRAMES_IN_FLIGHT_COUNT last ones (ex: with MAX_FRAMES_IN_FLIGHT_COUNT = 8, 0..7 are frame in flight and
    // 8..15 are image in flight
    private static uint GetFrameInFlightFenceIndex(uint frameIndex) => frameIndex;
    private static uint GetImageInFlightFenceIndex(uint frameIndex) => frameIndex + MAX_FRAMES_IN_FLIGHT_COUNT;
    
    private static uint GetImageAvailableSemaphoreIndex(uint frameIndex) => frameIndex;
    private static uint GetRenderFinishedSemaphoreIndex(uint frameIndex) => frameIndex + MAX_FRAMES_IN_FLIGHT_COUNT;
    private static uint GetCommandBufferIndex(uint frameIndex) => frameIndex;
}