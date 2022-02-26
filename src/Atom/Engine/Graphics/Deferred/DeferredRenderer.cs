using System.Diagnostics;
using System.Runtime.CompilerServices;
using Atom.Engine.Global;
using Silk.NET.Maths;
using Silk.NET.Vulkan.Extensions.KHR;

using Atom.Engine.Vulkan;

namespace Atom.Engine;

public class DeferredRenderer
{
    private struct SurfaceCapabilities
    {
        public Clamp<uint> ImageCount;
        public Vector2D<uint> CurrentExtent;
        public ClampVector2D<uint> ImageVectors;
        public uint MaxImageArrayLayers;
        public vk.SurfaceTransformFlagsKHR SupportedTransforms;
        public vk.SurfaceTransformFlagsKHR CurrentTransform;
        public vk.CompositeAlphaFlagsKHR SupportedCompositeAlpha;
        public vk.ImageUsageFlags SupportedUsageFlags;
    }

    private struct RenderPassClear
    {
        public vk.ClearColorValue GBufferAlbedo        = new (0.0F, 0.0F, 0.0F, 0.0F);
        public vk.ClearColorValue GBufferNormal        = new (0.0F, 0.0F, 0.0F, 0.0F);
        public vk.ClearColorValue GBufferPosition      = new (0.0F, 0.0F, 0.0F, 0.0F);
        public vk.ClearDepthStencilValue GBufferDepth  = new (0.0F);
        
        public RenderPassClear() { }
    }

#region Configuration

    // -- Defaults
    public const vk.Format DEFAULT_COLOR_FORMAT = vk.Format.B8G8R8A8Unorm/*B8G8R8A8Srgb*/;
    private const vk.ColorSpaceKHR DEFAULT_COLOR_SPACE = vk.ColorSpaceKHR.ColorSpaceSrgbNonlinearKhr/*ColorSpaceSrgbNonlinearKhr*/;
    private const vk.ImageUsageFlags SWAPCHAIN_IMAGE_USAGES = vk.ImageUsageFlags.ImageUsageColorAttachmentBit;
    private const vk.CompositeAlphaFlagsKHR ALPHA_COMPOSITING = vk.CompositeAlphaFlagsKHR.CompositeAlphaOpaqueBitKhr;
    private const vk.PresentModeKHR DEFAULT_PRESENT_MODE = vk.PresentModeKHR.PresentModeImmediateKhr;
    private const bool DEFAULT_CLIPPED = false;
    private const uint ARRAY_LAYERS = 1U; // for VR: 2U
    private const vk.SwapchainCreateFlagsKHR SWAPCHAIN_FLAGS = 0;

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

    private vk.Device _device;
    private vk.PhysicalDevice _physicalDevice;
    private QueueFamily _renderFamily;
    private vk.Queue _queue;

    private Silk.NET.Vulkan.RenderPass _renderPass;
    private uint _subpassIndex = 0U;
    
    private vk.SurfaceKHR _surface;
    private KhrSurface _surfaceExtension;
    private vk.SwapchainKHR _swapchain;
    private KhrSwapchain _swapchainExtension;
    
    // framebuffer
    private SlimImage[] _images = new SlimImage[MAX_IMAGE_COUNT];
    private SlimImageView[] _views = new SlimImageView[MAX_VIEW_COUNT];
    private SlimFramebuffer[] _framebuffers = new SlimFramebuffer[MAX_FRAMES_IN_FLIGHT_COUNT];
    
    // synchronisation
    // 0..7: fences in flight, 8..15: images in flight
    private SlimFence[] _fences = new SlimFence[MAX_FRAMES_IN_FLIGHT_COUNT * 2]; 
    // 0..7: image available, 8..15: render finished
    private SlimSemaphore[] _semaphores = new SlimSemaphore[MAX_FRAMES_IN_FLIGHT_COUNT * 2];
    
    // commands
    private SlimCommandPool _commandPool;
    private SlimCommandBuffer[] _commands = new SlimCommandBuffer[MAX_FRAMES_IN_FLIGHT_COUNT];
    
    // memory storage for all the framebuffers
    private DeviceMemory _framebuffersMemory;
    
    // surface
    private Vector2D<uint> _extent;
    private vk.Format _colorFormat = DEFAULT_COLOR_FORMAT;
    private vk.ColorSpaceKHR _colorSpace = DEFAULT_COLOR_SPACE;
    private vk.PresentModeKHR _presentMode = DEFAULT_PRESENT_MODE;
    private bool _clip = DEFAULT_CLIPPED;
    
    private NDCDrawer _screenDrawer = null!;
    private GBufferDrawer _gbufferDrawer = null!;

    private uint _swapchainImageCount;
    private uint _frameIndex = 0;
    private vk.Format _depthFormat;
    
    
    public vk.Format ColorFormat
    {
        get => _colorFormat;
        set
        {
            unsafe
            {
                uint format_mode_count = default;
                _surfaceExtension.GetPhysicalDeviceSurfaceFormats(_physicalDevice, _surface,
                    ref format_mode_count, null);
                Span<vk.SurfaceFormatKHR> formats = stackalloc vk.SurfaceFormatKHR[(int)format_mode_count];
                _surfaceExtension.GetPhysicalDeviceSurfaceFormats(_physicalDevice, _surface,
                    &format_mode_count, formats);

                vk.SurfaceFormatKHR available = default;
                for (int i = 0; i < format_mode_count; i++)
                {
                    if (formats[i].Format == value)
                    {
                        available = formats[i];
                        break;
                    }
                }
                
                if (available.Format == vk.Format.Undefined)
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
    public vk.ColorSpaceKHR ColorSpace => _colorSpace;
    public vk.PresentModeKHR PresentMode
    {
        get => _presentMode;
        set
        {
            unsafe
            {
                uint present_mode_count = default;
                _surfaceExtension.GetPhysicalDeviceSurfacePresentModes(_physicalDevice, _surface,
                    ref present_mode_count, null);
                Span<vk.PresentModeKHR> present_modes = stackalloc vk.PresentModeKHR[(int)present_mode_count];
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
        vk.Device device,
        vk.PhysicalDevice physicalDevice,
        KhrSurface surfaceExtension,
        vk.SurfaceKHR surface,
        QueueFamily renderFamily,
        vk.Queue queue)
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

        for (int i = 0; i < MAX_FRAMES_IN_FLIGHT_COUNT; i++)
        {
            _fences[i] = new SlimFence(_device, signaled: true);
            _semaphores[i] = new SlimSemaphore(_device);
        }

        for (int i = 0; i < MAX_FRAMES_IN_FLIGHT_COUNT; i++) // 2x more semaphores than frames in flight.
        {
            _semaphores[i + MAX_FRAMES_IN_FLIGHT_COUNT] = new SlimSemaphore(_device);
        }
        
        DeferredRenderPass.CreateRenderPass(_device, _physicalDevice, out _renderPass, out _depthFormat);
        Graphics.MainRenderPass = _renderPass;
        _gbufferDrawer = new GBufferDrawer(MAX_FRAMES_IN_FLIGHT_COUNT, device);

        _commandPool = new SlimCommandPool(device, renderFamily.Index, CommandPoolCreateFlags.ResetCommandBuffer);
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
        SlimSemaphore image_availability_semaphore = _semaphores[GetImageAvailableSemaphoreIndex(_frameIndex)];
        SlimSemaphore render_finished_semaphore = _semaphores[GetRenderFinishedSemaphoreIndex(_frameIndex)];

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
        
        // save camera state for this frame
        CameraData.UpdateFrame(_frameIndex);

        if (Draw.HasUpdates(frameIndex: swap_image_index, cameraIndex: 0))
        {
            Log.Trace($"UPDATE [{swap_image_index}][{0}]");
            vk.Extent2D extent = new(_extent.X, _extent.Y);
            BuildCommands(swap_image_index, extent, true);
        }


        vk.PipelineStageFlags wait_dst_draw = vk.PipelineStageFlags.PipelineStageColorAttachmentOutputBit;
        
        vk.SubmitInfo draw_submit = new(
            pWaitDstStageMask: &wait_dst_draw,
            waitSemaphoreCount: 1,
            pWaitSemaphores: (vk.Semaphore*)&image_availability_semaphore,
            signalSemaphoreCount: 1,
            pSignalSemaphores: (vk.Semaphore*)&render_finished_semaphore,
            commandBufferCount: 1,
            pCommandBuffers: (vk.CommandBuffer*)&command
        );
        VK.API.QueueSubmit(
            _queue,
            submitCount: 1,
            in draw_submit,
            frame_fence.Handle
        );

        vk.SwapchainKHR swapchain = _swapchain;
        
        vk.PresentInfoKHR present_info = new(
            waitSemaphoreCount: 1,
            pWaitSemaphores: (vk.Semaphore*)&render_finished_semaphore,
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
            out vk.SurfaceCapabilitiesKHR capabilities_khr
        );

        SurfaceCapabilities capabilities = Unsafe.As<vk.SurfaceCapabilitiesKHR, SurfaceCapabilities>(ref capabilities_khr);

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

        vk.SwapchainKHR previous = _swapchain;

        vk.SwapchainCreateInfoKHR swapchain_info = new(
            surface: _surface,
            minImageCount: image_count,
            imageFormat: _colorFormat,
            imageColorSpace: _colorSpace,
            imageExtent: *(vk.Extent2D*)&extent,
            imageUsage: SWAPCHAIN_IMAGE_USAGES,
            imageSharingMode: vk.SharingMode.Exclusive, // todo: screenshots, ...
            pQueueFamilyIndices: &image_count,
            queueFamilyIndexCount: 1,
            preTransform: vk.SurfaceTransformFlagsKHR.SurfaceTransformIdentityBitKhr,
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
        Span<vk.Image> swap_images = stackalloc vk.Image[(int)swap_image_count];
        _swapchainExtension.GetSwapchainImages(_device, _swapchain, &swap_image_count, swap_images);

        if (previous_swap_image_count != 0)
        {
            CleanRenderObjects(previous_swap_image_count);
        }

        // image creation and memory allocation / binding
        Span<vk.MemoryRequirements> requirements = 
            stackalloc vk.MemoryRequirements[(int)DEFERRED_IMAGE_COUNT * (int)new_swap_image_count];
        ulong required_bytes_to_allocate = 0UL;
        
        for (uint i = 0U; i < new_swap_image_count; i++)
        {
            uint image_index = GetImageBaseIndex(i);

            CreateImages(i, extent, swap_images[(int)i]);
            
            // get required memory allocations properties
            for (int j = 0; j < DEFERRED_IMAGE_COUNT; j++)
            {
                int requirements_index = (int)(i * DEFERRED_IMAGE_COUNT) + j;
                _images[image_index + 1 + j].GetMemoryRequirements(_device, out requirements[requirements_index]);
                ref vk.MemoryRequirements reqs = ref requirements[requirements_index];
                required_bytes_to_allocate += reqs.Size;
            }
        }

        AllocateImagesMemory(new_swap_image_count, required_bytes_to_allocate, requirements);
        
        // views and framebuffer creation
        for (uint i = 0U; i < new_swap_image_count; i++)
        {
            uint view_index = GetViewBaseIndex(i);
            CreateViews(i);

            ReadOnlySpan<SlimImageView> framebuffer_views = _views.AsSpan((int)view_index, (int)DEFERRED_VIEW_COUNT);

            _framebuffers[i] = new(
                device: _device,
                renderPass: _renderPass,
                attachments: framebuffer_views,
                width: extent.X,
                height: extent.Y,
                layers: ARRAY_LAYERS
            );
        }

        Span<SlimImageView> swapchain_views = stackalloc SlimImageView[(int)new_swap_image_count];
        for (uint i = 0; i < new_swap_image_count; i++)
        {
            swapchain_views[(int)i] = _views[GetViewBaseIndex(i) + 5];
        }

        vk.Extent2D vk_extent = new(extent.X, extent.Y);
        
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

    private void ResetCommandBuffers() => _commandPool.Reset(_device);

    private unsafe void CleanMemory() => VK.API.FreeMemory(_device, _framebuffersMemory, null);
    
    private void CleanFramebuffers(uint count)
    {
        for (int i = 0; i < count; i++)
        {
            _framebuffers[i].Destroy(_device);
        }
    }
    
    private void CleanViews(uint count)
    {
        for (uint i = 0; i < count * (RESULT_VIEW_COUNT + DEFERRED_VIEW_COUNT); i++)
        {
            _views[i].Destroy(_device);
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
                _images[i].Destroy(_device);
            }
        }
    } 
    

    public unsafe void Dispose()
    {
        _gbufferDrawer.Dispose();
        _screenDrawer.Dispose();

        CleanRenderObjects(_swapchainImageCount);
        
        _commandPool.Destroy(_device);

        for (int i = 0; i < MAX_FRAMES_IN_FLIGHT_COUNT; i++)
        {
            _fences[i].Destroy(_device);
            _semaphores[i].Destroy(_device);
        }
        for (int i = 0; i < MAX_FRAMES_IN_FLIGHT_COUNT; i++) // actually 16 semaphores to destroy.
        {
            _semaphores[i + MAX_FRAMES_IN_FLIGHT_COUNT].Destroy(_device);
        }
        
        DestroySwapchain(_swapchain);
        
        VK.API.DestroyRenderPass(_device, _renderPass, null);
        
        GC.SuppressFinalize(this);
    }

    ~DeferredRenderer() => Dispose();

    private unsafe void DestroySwapchain(vk.SwapchainKHR? swapchain)
    {
        _swapchainExtension.DestroySwapchain(_device, swapchain.Value, null);
    }

    private unsafe void BuildCommands(uint swapImageIndex, vk.Extent2D extent, bool doReset = false)
    {
        vk.Rect2D area = new(extent: extent);
        SlimCommandBuffer cmd = _commands[swapImageIndex];

        if (doReset)
        {
            cmd.Reset();
        }

        vk.CommandBufferBeginInfo begin = new(flags: 0);
        VK.API.BeginCommandBuffer(cmd, in begin);
        {
            vk.RenderPassBeginInfo pass_info = new (
                renderPass: _renderPass,
                renderArea: area,
                framebuffer: _framebuffers[swapImageIndex],
                clearValueCount: 4,
                pClearValues: (vk.ClearValue*)(RenderPassClear*)RENDER_PASS_CLEAR
            );
            VK.API.CmdBeginRenderPass(cmd, in pass_info, vk.SubpassContents.Inline);
            // draw meshes in gbuffer
            // just consider 1 camera for now.
            
            Vector2D<uint> vec_extent = new(extent.Width, extent.Height);
            Draw.UpdateFrame(cmd, vec_extent, cameraIndex: 0, frameIndex: swapImageIndex);
            
            VK.API.CmdNextSubpass(cmd, vk.SubpassContents.Inline);
            // draw lit render
            int view_index = (int)GetViewBaseIndex(swapImageIndex);
            _gbufferDrawer.CmdComputeGBuffer(cmd, swapImageIndex,
                _views.AsSpan()[view_index..(view_index+4)]);
            VK.API.CmdEndRenderPass(cmd);
            
            // draw on swapchain image as a fullscreen image
            _screenDrawer.CmdDrawView(cmd, swapImageIndex, _views[GetViewBaseIndex(swapImageIndex) + 4]);
        }
        VK.API.EndCommandBuffer(cmd);
    }

    private void CreateViews(uint swapImageIndex)
    {
        uint view_index  = GetViewBaseIndex (swapImageIndex);
        uint image_index = GetImageBaseIndex(swapImageIndex);

        SlimImage final_image           = _images[image_index    ];
        SlimImage gbuffer_main_image    = _images[image_index + 1];
        SlimImage gbuffer_depth_image   = _images[image_index + 2];
        SlimImage lit_image             = _images[image_index + 3];

        void create_view(SlimImage image, vk.Format format, ImageAspectFlags aspect, out SlimImageView view, uint layer = 0)
        {
            view = new SlimImageView(
                _device,  image,
                viewType: vk.ImageViewType.ImageViewType2D,
                format:   format,
                components: ComponentMapping.Identity,
                subresourceRange: new vk.ImageSubresourceRange(
                    aspectMask:     (vk.ImageAspectFlags)aspect,
                    baseMipLevel:   0    , levelCount: 1,
                    baseArrayLayer: layer, layerCount: 1)
            );
        }

        const vk.Format GBUFFER_FORMAT = vk.Format.R32G32B32A32Sfloat;
        
        // albedo / luminance | normal / roughness / metalness | position / translucency
        for (uint i = 0; i < 3; i++) // 3 views for the main g buffer
        {
            create_view(gbuffer_main_image, 
                GBUFFER_FORMAT, ImageAspectFlags.Color, 
                out _views[view_index + i], 
                layer: i
            );
        }

        // depth
        create_view(gbuffer_depth_image,
            _depthFormat, ImageAspectFlags.Depth, 
            out _views[view_index + 3]
        );
        
        // lit
        create_view(lit_image,
            GBUFFER_FORMAT, ImageAspectFlags.Color, 
            out _views[view_index + 4]
        );
        
        // final
        create_view(final_image, 
            _colorFormat, ImageAspectFlags.Color, 
            out _views[view_index + 5]
        );
    }
    
    private void AllocateImagesMemory(uint swapImageCount, ulong size, ReadOnlySpan<vk.MemoryRequirements> requirements)
    {
        // allocate the memory required for all the framebuffers
        _framebuffersMemory = new DeviceMemory(
            _device,
            size: size,
            memoryTypeIndex: VK.FindMemoryType(
                physicalDevice: _physicalDevice, 
                typeFilter: requirements[0].MemoryTypeBits, // consider [0] as all other memory representation
                properties: MemoryPropertyFlags.DeviceLocal
            )
        );
        
        ulong offset = 0UL;
        // assign images memories
        for (uint i = 0; i < swapImageCount; i++)
        {
            uint image_base_index = GetImageBaseIndex(i);
            
            for (int j = 0; j < DEFERRED_IMAGE_COUNT; j++)
            {
                int image_index = (int)image_base_index + 1 + j;
                int requirements_index = (int)(i * DEFERRED_IMAGE_COUNT) + j;
                vk.MemoryRequirements reqs = requirements[requirements_index];

                ulong mem_size = reqs.Size;
                _images[image_index].BindMemory(_device, _framebuffersMemory, offset);
                offset += mem_size;
            }
        }
    }
    private void CreateImages(uint swapImageIndex, Vector2D<uint> extent, SlimImage swapImage)
    {
        uint image_index = GetImageBaseIndex(swapImageIndex);
        ref uint width = ref extent.X;
        ref uint height = ref extent.Y;

        vk.Extent3D size = new vk.Extent3D(width, height, 1);

        uint fam_index = _renderFamily.Index;

        void create_image(vk.Format format, uint arrayLayers, vk.ImageUsageFlags usages, out SlimImage image)
        {
            uint f = fam_index;
            image = new SlimImage(
                _device, flags: 0,
                type: vk.ImageType.ImageType2D, format: format, extent: size,
                mipLevels: 1, arrayLayers: arrayLayers,
                samples: vk.SampleCountFlags.SampleCount1Bit,
                tiling: vk.ImageTiling.Optimal,
                usage: usages, initialLayout: vk.ImageLayout.Undefined,
                queueFamilyIndices: f.AsSpan(), sharingMode: vk.SharingMode.Exclusive // only used for rendering
            );
        }
        
        // create images
        // main gbuffer image: albedo/luminance + normal/roughness/metalness + position/translucency (3x, all RGBA32)
        _images[image_index] = swapImage;

        create_image( /* gBuffer main */
            format: vk.Format.R32G32B32A32Sfloat, 
            arrayLayers: 3,
            usages: vk.ImageUsageFlags.ImageUsageColorAttachmentBit | vk.ImageUsageFlags.ImageUsageInputAttachmentBit,
            out _images[image_index + 1]
        );
        
        create_image( /* gBuffer depth */
            format: _depthFormat, 
            arrayLayers: 1,
            usages: vk.ImageUsageFlags.ImageUsageDepthStencilAttachmentBit | vk.ImageUsageFlags.ImageUsageInputAttachmentBit,
            out _images[image_index + 2]
        );
        
        create_image( /* lit */
            format: vk.Format.R32G32B32A32Sfloat, 
            arrayLayers: 1,
            usages: vk.ImageUsageFlags.ImageUsageColorAttachmentBit | vk.ImageUsageFlags.ImageUsageSampledBit,
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