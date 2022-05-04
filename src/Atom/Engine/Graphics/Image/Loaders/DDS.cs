using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using Atom.Engine.Vulkan;
using Silk.NET.Maths;

namespace Atom.Engine.Loaders;

using Atom.Engine.DDS;

using dxf = DirectX.Format;
using vkf = ImageFormat;

public static class DDS
{
    // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
    private static ImageFormat GetVkBCFormat(DirectX.Format dxformat, AlphaModes alphaMode) => dxformat switch
    {
        // BC1
        dxf.BC1_UNorm      when alphaMode is AlphaModes.Straight => vkf.BC1_RGBA_UNorm_Block,
        dxf.BC1_UNorm      when alphaMode is AlphaModes.Opaque   => vkf.BC1_RGB_UNorm_Block ,
        dxf.BC1_UNorm_SRGB when alphaMode is AlphaModes.Straight => vkf.BC1_RGBA_sRGB_Block ,
        dxf.BC1_UNorm_SRGB when alphaMode is AlphaModes.Opaque   => vkf.BC1_RGB_sRGB_Block  ,
        dxf.BC1_Typeless   => throw new NotImplementedException("Typeless BC1 is not available on Vulkan."),

        // BC2
        dxf.BC2_UNorm      => vkf.BC2_UNorm_Block,
        dxf.BC2_UNorm_SRGB => vkf.BC2_sRGB_Block ,

        // BC3
        dxf.BC3_UNorm      => vkf.BC3_UNorm_Block,
        dxf.BC3_UNorm_SRGB => vkf.BC3_sRGB_Block ,

        // BC4
        dxf.BC4_UNorm      => vkf.BC4_UNorm_Block,
        dxf.BC4_SNorm      => vkf.BC4_SNorm_Block,

        // BC5
        dxf.BC5_UNorm      => vkf.BC5_UNorm_Block,
        dxf.BC5_SNorm      => vkf.BC5_SNorm_Block,
        dxf.BC5_Typeless   => throw new NotImplementedException("Typeless BC5 is not available on Vulkan."),

        // BC6
        dxf.BC6H_UFloat16  => vkf.BC6H_UFloat_Block,
        dxf.BC6H_SFloat16  => vkf.BC6H_SFloat_Block,
        dxf.BC6H_Typeless  => throw new NotImplementedException("Typeless BC6H is not available on Vulkan."),

        // BC7
        dxf.BC7_UNorm      => vkf.BC7_UNorm_Block,
        dxf.BC7_UNorm_SRGB => vkf.BC7_sRGB_Block,
        dxf.BC7_Typeless   => throw new NotImplementedException("Typeless BC7 is not available on Vulkan."),
        
        
        _ => throw new InvalidDDSFile(
            $"DirectX format {dxformat} (Alpha: {alphaMode}) is not a valid DirectDraw format.")
    };

    public static unsafe Image Load(Stream stream,
        ReadOnlySpan<u32> queueFamilies,
        vk.SharingMode sharingMode = vk.SharingMode.Exclusive,
        vk.SampleCountFlags samples = vk.SampleCountFlags.SampleCount1Bit,
        vk.ImageLayout layout = vk.ImageLayout.ShaderReadOnlyOptimal,
        PipelineStageFlags stage = PipelineStageFlags.FragmentShader,
        vk.AccessFlags accessMask = vk.AccessFlags.AccessShaderReadBit,
        ImageUsageFlags usages = ImageUsageFlags.Sampled,
        vk.Device? device = null)
    {
        vk.Device used_device = device ?? VK.Device;
        
        const u32 DDS_MAGIC = (u32)FourCharCodes.DDS;

        u64 index = 0UL;
        
        // First verify if this is a (modern) DDS texture
        u32 magic = 0U ;
        stream.Read(buffer: new Span<u8>(&magic, 4));

        if (DDS_MAGIC != magic)
        {
            throw new InvalidDDSFile(
                $"The magic of the file ({magic}) is not the DDS magic (DDS as fourCC: {DDS_MAGIC})."
            );
        }
        
        // Get information from the base DDS Header 
        Header header;
        stream.Read(buffer: new Span<u8>(&header, (i32)Header.STRUCTURE_SIZE));
        header.ThrowIfWrongSize();
        header.PixelFormat.ThrowIfWrongSize();

        DXT10Header dxt10_header;
        stream.Read(buffer: new Span<u8>(&dxt10_header, length: Unsafe.SizeOf<DXT10Header>()));
        
        
        // Some sanitary header checking
        if (header.PixelFormat.FourCharCode != FourCharCodes.DX10)
        {
            throw new NotImplementedException("Only DX10 DDS files are implemented.");
        }

        // Get Vulkan parameters
        u32          vk_width        = header.Width == 0
                                        ? throw new InvalidDDSFile("No width is specified in DDS file's header.")
                                        : header.Width;
        u32          vk_height       = Math.Max(1, header.Height); // 1D textures might not have a height set.
        u32          vk_depth        = Math.Max(1, header.Depth ); // 1D & 2D textures might not have a depth set.
        u32          vk_mip_levels   = header.MipMapCount;
        u32          vk_array_layers = Math.Max(1, dxt10_header.ArraySize);
        vk.ImageType vk_image_type   = (vk.ImageType)((u32)dxt10_header.ResourceDimension - 2);
        ImageFormat  vk_format       = GetVkBCFormat(dxt10_header.Format, dxt10_header.MiscFlags2);

        // No other information is required for Vulkan, let's create that image !
        u64 linear_size = header.PitchOrLinearSize;
        u64 min_linear_size = vk_format      is
            ImageFormat.BC1_RGB_UNorm_Block  or
            ImageFormat.BC1_RGBA_UNorm_Block or
            ImageFormat.BC1_RGB_sRGB_Block   or
            ImageFormat.BC1_RGBA_sRGB_Block
            ? 8UL // dxt1 is 8bit while dxt2-5 are 16bit (for square texture)
            : 16UL;
        
        Span<u64> mip_lengths = stackalloc u64[(i32)vk_mip_levels];
        for (i32 mip = 0; mip < vk_mip_levels; mip++)
        {
            mip_lengths[mip] = linear_size;
            linear_size = Math.Max(min_linear_size, linear_size / 4);
        }

        ref readonly vk.SharingMode      final_sharing = ref sharingMode  ;
        ref readonly ReadOnlySpan<u32>   final_queues  = ref queueFamilies;
        ref readonly vk.SampleCountFlags final_samples = ref samples      ;
        
        const vk.ImageTiling         FINAL_TILING          = vk.ImageTiling.Optimal  ;
        ImageUsageFlags final_usages  =  usages | ImageUsageFlags.TransferDestination;
        const vk.ImageLayout         FINAL_INITIAL_LAYOUT  = vk.ImageLayout.Undefined;
        ref readonly vk.ImageLayout  final_layout          = ref layout              ;

        vk.Extent3D image_extent  = new(vk_width, vk_height, vk_depth);
        
        // Create the final image
        SlimImage final_image = new(
            device            : used_device    ,
            type              : vk_image_type  ,
            format            : vk_format      ,
            extent            : image_extent   ,
            mipLevels         : vk_mip_levels  ,
            arrayLayers       : vk_array_layers,
            samples           : final_samples  , /* unrelated to DDS */
            tiling            : FINAL_TILING   , /* unrelated to DDS */
            usage             : final_usages   , /* unrelated to DDS */
            sharingMode       : final_sharing  ,
            queueFamilyIndices: final_queues   ,
            initialLayout     : FINAL_INITIAL_LAYOUT     /* unrelated to DDS */
        );
        MemorySegment final_memory = final_image.CreateDedicatedMemory(
            device: used_device,
            properties: MemoryPropertyFlags.DeviceLocal
        );
        
        const vk.SampleCountFlags STAGING_SAMPLES = vk.SampleCountFlags.SampleCount1Bit;
        const vk.ImageTiling      STAGING_TILING  = vk.ImageTiling.Linear              ;
        const    ImageUsageFlags  STAGING_USAGES  =    ImageUsageFlags.TransferSource  ;
        const vk.SharingMode      STAGING_SHARING = vk.SharingMode.Exclusive           ;
        const vk.ImageLayout      STAGING_LAYOUT  = vk.ImageLayout.Undefined           ;
        
        // create image with max caps for one final resource. so it is used as a buffer for all the other transfers.
        SlimImage staging_image = new( // for now grab the required memory
            device            : used_device            ,
            type              : vk_image_type          ,
            format            : vk_format              ,
            extent            : image_extent           ,
            mipLevels         : 1                      ,
            arrayLayers       : vk_array_layers        ,
            samples           : STAGING_SAMPLES        , /* unrelated to DDS */
            tiling            : STAGING_TILING         , /* unrelated to DDS */
            usage             : STAGING_USAGES         , /* unrelated to DDS */
            sharingMode       : STAGING_SHARING        ,
            queueFamilyIndices: ReadOnlySpan<u32>.Empty,
            initialLayout     : STAGING_LAYOUT           /* unrelated to DDS */
        );
        staging_image.GetMemoryRequirements(used_device, out vk.MemoryRequirements staging_reqs);
        staging_image.Destroy(used_device);
        
        u32 total_staging_images = vk_array_layers * vk_mip_levels;


        vk.ImageSubresourceRange final_all_resources = new(
            aspectMask    : vk.ImageAspectFlags.ImageAspectColorBit,
            baseArrayLayer: 0U                                     ,
            layerCount    : vk_array_layers                        ,
            baseMipLevel  : 0U                                     ,
            levelCount    : vk_mip_levels
        );
        
        
        Span<vk.ImageMemoryBarrier> make_transfer_ready_barriers = 
            stackalloc vk.ImageMemoryBarrier[(i32)total_staging_images + 1];
        
        ref vk.ImageMemoryBarrier image_ready_barrier = ref make_transfer_ready_barriers[^1];
        
        image_ready_barrier = new vk.ImageMemoryBarrier(
            srcAccessMask      : vk.AccessFlags.AccessNoneKhr         ,
            dstAccessMask      : vk.AccessFlags.AccessTransferWriteBit,

            oldLayout          : vk.ImageLayout.Undefined             ,
            newLayout          : vk.ImageLayout.TransferDstOptimal    ,

            srcQueueFamilyIndex: 0                                    ,
            dstQueueFamilyIndex: 0                                    ,
            
            image              : final_image                        ,
            subresourceRange   : final_all_resources
        );

        vk.ImageMemoryBarrier image_end_barrier = image_ready_barrier with
        {
            SrcAccessMask      = vk.AccessFlags.AccessTransferWriteBit,
            DstAccessMask      = accessMask                           ,

            OldLayout          = vk.ImageLayout.TransferDstOptimal    ,
            NewLayout          = layout                               ,
        };

        Span<SlimImage> staging_images = stackalloc SlimImage[(i32)total_staging_images];
        
        u64 total_data_required = 0UL;
        Span<vk.MemoryRequirements> staging_memory_requirements 
            = stackalloc vk.MemoryRequirements[(i32)total_staging_images];

        vk.ImageSubresourceRange mip_image_subresource = new(
            aspectMask    : vk.ImageAspectFlags.ImageAspectColorBit,
            baseArrayLayer: 0U                                     ,
            layerCount    : 1U                                     ,
            baseMipLevel  : 0U                                     ,
            levelCount    : 1U
        );

        for (u32 array = 0U; array < vk_array_layers; array++)
        {
            u32 mip_width = vk_width, mip_height = vk_height, mip_depth = vk_depth;
            for (u32 mip = 0U; mip < vk_mip_levels; mip++)
            {
                u32 i = AMath.To1D(mip, array, vk_array_layers);
                vk.Extent3D mip_extent = new(mip_width, mip_height, mip_depth);

                SlimImage mip_image = staging_images[(i32)i] = new SlimImage(
                    device            : used_device            ,
                    type              : vk_image_type          ,
                    format            : vk_format              ,
                    extent            : mip_extent             ,
                    mipLevels         : 1U                     ,
                    arrayLayers       : 1U                     ,
                    samples           : STAGING_SAMPLES        , /* unrelated to DDS */
                    tiling            : STAGING_TILING         , /* unrelated to DDS */
                    usage             : STAGING_USAGES         , /* unrelated to DDS */
                    sharingMode       : STAGING_SHARING        ,
                    queueFamilyIndices: ReadOnlySpan<u32>.Empty,
                    initialLayout     : STAGING_LAYOUT           /* unrelated to DDS */
                );
                
                mip_image.GetMemoryRequirements(used_device, out staging_memory_requirements[(i32)i]);

                total_data_required += staging_memory_requirements[(i32)i].Size;

                make_transfer_ready_barriers[(i32)i] = image_ready_barrier with
                {
                    Image            = mip_image                           ,
                    NewLayout        = vk.ImageLayout.TransferSrcOptimal   ,
                    DstAccessMask    = vk.AccessFlags.AccessTransferReadBit,
                    SubresourceRange = mip_image_subresource
                };
                
                mip_width  = Math.Max(1, mip_width  / 2);
                mip_height = Math.Max(1, mip_height / 2);
                mip_depth  = Math.Max(1, mip_depth  / 2);
            }
        }

        const MemoryPropertyFlags STAGING_MEMORY_PROPERTIES = MemoryPropertyFlags.HostCoherent |
                                                              MemoryPropertyFlags.HostVisible  ;

        using VulkanMemory staging_memory = new(
            device: used_device,
            size: total_data_required,
            memoryTypeIndex: VK.GPU.PhysicalDevice.FindMemoryType(
                typeFilter: staging_reqs.MemoryTypeBits,
                properties: STAGING_MEMORY_PROPERTIES
            )
        );

        using MemoryMap<u8> map = staging_memory.Map();

        vk.ImageSubresourceLayers mip_subresource = new(
            aspectMask    : vk.ImageAspectFlags.ImageAspectColorBit,
            baseArrayLayer: 0U                                     ,
            layerCount    : 1U                                     ,
            mipLevel      : 0U                                     
        );

        u64 data_index = 0UL;

        for (i32 i = 0; i < total_staging_images; i++)
        {
            u64 size = staging_memory_requirements[i].Size;
            
            staging_images[i].BindMemory(used_device, staging_memory.Segment(data_index, size));
            data_index += size;
        }

        SlimCommandPool transfer_pool = new(
            device: used_device,
            queueFamilyIndex: 0
        );
        transfer_pool.AllocateCommandBuffer(
            device: used_device, 
            level: CommandBufferLevel.Primary,
            out SlimCommandBuffer cmd
        );
        
        data_index = 0UL;
        
        using (CommandRecorder recorder = new(
            commandBuffer: cmd,
            CommandBufferUsageFlags.OneTimeSubmit))
        {
            recorder.PipelineBarrier(
                sourceStageMask     : PipelineStageFlags.TopOfPipe,
                destinationStageMask: PipelineStageFlags.Transfer,
                imageMemoryBarriers : make_transfer_ready_barriers
            );

            for (u32 array = 0U; array < vk_array_layers; array++)
            {
                u32 mip_width = vk_width, mip_height = vk_height, mip_depth = vk_depth;
                for (u32 mip = 0U; mip < vk_mip_levels; mip++)
                {
                    u32 i = AMath.To1D(mip, array, vk_array_layers);
                    u64 mip_size = mip_lengths[(i32)mip];

                    stream.Read(map.AsSpan(data_index, mip_size));

                    data_index += staging_memory_requirements[(i32)i].Size;
                    
                    
                    vk.Extent3D mip_extent = new(mip_width, mip_height, mip_depth);

                    vk.ImageCopy copy_region = new(
                        srcOffset: default,
                        srcSubresource: mip_subresource,
                        dstOffset: default,
                        dstSubresource: mip_subresource with
                        {
                            BaseArrayLayer = array,
                            MipLevel       = mip  ,
                        },
                        extent: mip_extent
                    );
                    
                    recorder.CopyImage(
                        sourceImage           : staging_images[(i32)i]           ,
                        sourceImageLayout     : vk.ImageLayout.TransferSrcOptimal,
                        destinationImage      : final_image                      ,
                        destinationImageLayout: vk.ImageLayout.TransferDstOptimal,
                        in copy_region
                    );
                    
                    
                    mip_width  = Math.Max(1, mip_width  / 2);
                    mip_height = Math.Max(1, mip_height / 2);
                    mip_depth  = Math.Max(1, mip_depth  / 2);
                }
            }
            
            recorder.PipelineBarrier(
                sourceStageMask     : PipelineStageFlags.Transfer,
                destinationStageMask: stage                      ,
                imageMemoryBarriers : image_end_barrier.AsSpan()
            );

        }

        vk.CommandBuffer* p_cmd = (vk.CommandBuffer*)&cmd;

        vk.SubmitInfo submission = new(
            commandBufferCount: 1U,
            pCommandBuffers   : p_cmd
        );

        SlimFence upload_fence = new(used_device);
        using (MutexLock<vk.Queue> queue = VK.Queue.Lock())
        {
            VK.API.QueueSubmit(queue.Data, 1U, in submission, upload_fence);
            upload_fence.Wait(used_device);
        }

        // cleanup
        upload_fence.Destroy(used_device);
        transfer_pool.Destroy(used_device);

        for (u32 i = 0; i < total_staging_images; i++)
        {
            staging_images[(i32)i].Destroy(used_device);
        }
        
        return new Image(
            resolution   : Unsafe.As<vk.Extent3D, Vector3D<u32>>(ref image_extent),
            format       : vk_format        ,
            imageType    : vk_image_type    ,
            tiling       : FINAL_TILING     ,
            usage        : final_usages     ,
            queueFamilies: final_queues     ,
            mipLevels    : vk_mip_levels    ,
            arrayLayers  : vk_array_layers  ,
            multisampling: final_samples    ,
            sharingMode  : final_sharing    ,
            layout       : final_layout     ,
            memory       : final_memory   ,
            image        : final_image    ,
            device       : used_device
        );
    }
}