using System.Runtime.CompilerServices;

using Atom.Engine.Vulkan;
using Silk.NET.Maths;


namespace Atom.Engine;



public class CommandRecorder : IDisposable
{
    public readonly SlimCommandBuffer CommandBuffer;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal unsafe CommandRecorder(SlimCommandBuffer commandBuffer, CommandBufferUsageFlags flags = 0)
    {
        CommandBuffer = commandBuffer;
        
        vk.CommandBufferBeginInfo begin_info = new(flags: flags.ToVk());
        VK.API.BeginCommandBuffer(CommandBuffer.Handle, in begin_info);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        VK.API.EndCommandBuffer(CommandBuffer.Handle);
        GC.SuppressFinalize(this);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    ~CommandRecorder() => Dispose();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void PipelineBarrier(
        PipelineStageFlags sourceStageMask,
        PipelineStageFlags destinationStageMask,
        ReadOnlySpan<vk.ImageMemoryBarrier> imageMemoryBarriers)
    {
        fixed (vk.ImageMemoryBarrier* p_image_barriers = imageMemoryBarriers) VK.API.CmdPipelineBarrier(
            commandBuffer            :CommandBuffer                  ,
            srcStageMask             : sourceStageMask.ToVk()         ,
            dstStageMask             : destinationStageMask.ToVk()    ,
            dependencyFlags          : 0U                             ,
            memoryBarrierCount       : 0U                             ,
            pMemoryBarriers          : null                           ,
            bufferMemoryBarrierCount : 0U                             ,
            pBufferMemoryBarriers    : null                           ,
            imageMemoryBarrierCount  : (u32)imageMemoryBarriers.Length,
            pImageMemoryBarriers     : p_image_barriers
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyImage(
        SlimImage sourceImage, vk.ImageLayout sourceImageLayout,
        SlimImage destinationImage, vk.ImageLayout destinationImageLayout,
        ReadOnlySpan<vk.ImageCopy> regions
        )
    {
        VK.API.CmdCopyImage(
            commandBuffer : CommandBuffer       ,
            srcImage      : sourceImage         ,
            srcImageLayout: sourceImageLayout    ,
            dstImage      : destinationImage    ,
            dstImageLayout: destinationImageLayout,
            pRegions      : regions
        );
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyImage(
        SlimImage sourceImage, vk.ImageLayout sourceImageLayout,
        SlimImage destinationImage, vk.ImageLayout destinationImageLayout,
        in vk.ImageCopy regions
    )
    {
        VK.API.CmdCopyImage(
            commandBuffer : CommandBuffer       ,
            srcImage      : sourceImage         ,
            srcImageLayout: sourceImageLayout    ,
            dstImage      : destinationImage    ,
            dstImageLayout: destinationImageLayout,
            regionCount   : 1U,
            pRegions      : regions
        );
    }

    public class RenderPassRecorder : IDisposable
    {
        public SlimCommandBuffer CommandBuffer => Recorder.CommandBuffer;
        public readonly CommandRecorder Recorder;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe RenderPassRecorder(
            CommandRecorder recorder, vk.RenderPass renderPass,
            vk.Rect2D renderArea, SlimFramebuffer framebuffer, ReadOnlySpan<vk.ClearValue> clearValues,
            vk.SubpassContents subpassContents = vk.SubpassContents.Inline)
        {
            Recorder = recorder;
            
            fixed (vk.ClearValue* p_clear_values = clearValues)
            {
                vk.RenderPassBeginInfo pass_info = new(
                    renderPass     : renderPass             ,
                    renderArea     : renderArea             ,
                    framebuffer    : framebuffer           ,
                    clearValueCount: (u32)clearValues.Length,
                    pClearValues   : p_clear_values
                );
                VK.API.CmdBeginRenderPass(CommandBuffer, in pass_info, subpassContents);
            }
        }
        
        public void DrawIndexed(u32 indexCount, u32 instanceCount = 1U, u32 firstIndex = 0U, i32 vertexOffset = 0, u32 firstInstance = 0U)
        {
            VK.API.CmdDrawIndexed(CommandBuffer, indexCount, 1U, 0U, 0, 0U);
        }
        
        public void Draw(u32 vertexCount, u32 instanceCount = 1U, u32 firstVertex = 0U, u32 firstInstance = 0U)
        {
            VK.API.CmdDraw(CommandBuffer, vertexCount, instanceCount, firstVertex, firstInstance);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void NextSubpass(vk.SubpassContents subpassContents = vk.SubpassContents.Inline)
        => VK.API.CmdNextSubpass(CommandBuffer, subpassContents);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            VK.API.CmdEndRenderPass(CommandBuffer);
            GC.SuppressFinalize(this);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        ~RenderPassRecorder() => Dispose();
    }

    public RenderPassRecorder RenderPass(vk.RenderPass renderPass,
        vk.Rect2D renderArea, SlimFramebuffer framebuffer, ReadOnlySpan<vk.ClearValue> clearValues,
        vk.SubpassContents subpassContents = vk.SubpassContents.Inline)
    => new (this, renderPass, renderArea, framebuffer, clearValues, subpassContents);
    
    public void Blit(ImageSubresource source, ImageSubresource destination, 
        vk.Filter filter = vk.Filter.Nearest)
    {
        /*vk.ImageBlit2 whole_region_base_mip = new(
            srcSubresource: new vk.ImageSubresourceLayers(
                aspectMask    : source.Aspects.ToVk(),
                mipLevel      : source.BaseMip       ,
                baseArrayLayer: source.BaseArray     ,
                layerCount    : source.ArrayCount
            ),
            dstSubresource: new vk.ImageSubresourceLayers(
                aspectMask    : destination.Aspects.ToVk(),
                mipLevel      : destination.BaseMip       ,
                baseArrayLayer: destination.BaseArray     ,
                layerCount    : destination.ArrayCount
            )
        );

        Vector3D<u32> src_res = source.Image.Resolution;
        vk.Offset3D src_whole = Unsafe.As<Vector3D<u32>, vk.Offset3D>(ref src_res);
        whole_region_base_mip.SrcOffsets.Element1 = src_whole;
        
        Vector3D<u32> dst_res = destination.Image.Resolution;
        vk.Offset3D dst_whole = Unsafe.As<Vector3D<u32>, vk.Offset3D>(ref dst_res);
        whole_region_base_mip.DstOffsets.Element1 = dst_whole;
        
        
        vk.BlitImageInfo2 info = new(
            srcImage      : (SlimImage)source.Image       ,
            srcImageLayout: source.Image.Layout           ,
            dstImage      : (SlimImage)destination.Image  ,
            dstImageLayout: destination.Image.Layout      ,
            regionCount   : 1U                            ,
            pRegions      : &whole_region_base_mip        ,
            filter        : filter 
        );
        
        VK.API.CmdBlitImage2(CommandBuffer, in info);*/

        vk.ImageBlit whole_region_base_mip = new(
            srcSubresource: new vk.ImageSubresourceLayers(
                aspectMask    : source.Aspects.ToVk(),
                mipLevel      : source.BaseMip       ,
                baseArrayLayer: source.BaseArray     ,
                layerCount    : source.ArrayCount
            ),
            dstSubresource: new vk.ImageSubresourceLayers(
                aspectMask    : destination.Aspects.ToVk(),
                mipLevel      : destination.BaseMip       ,
                baseArrayLayer: destination.BaseArray     ,
                layerCount    : destination.ArrayCount
            )
        );
        
        Vector3D<u32> src_res = source.Image.Resolution;
        vk.Offset3D src_whole = Unsafe.As<Vector3D<u32>, vk.Offset3D>(ref src_res);
        whole_region_base_mip.SrcOffsets.Element1 = src_whole;
        
        Vector3D<u32> dst_res = destination.Image.Resolution;
        vk.Offset3D dst_whole = Unsafe.As<Vector3D<u32>, vk.Offset3D>(ref dst_res);
        whole_region_base_mip.DstOffsets.Element1 = dst_whole;
        
        VK.API.CmdBlitImage(CommandBuffer,
            srcImage      : (SlimImage)source.Image,
            srcImageLayout: source.Image.Layout,
            dstImage      : (SlimImage)destination.Image,
            dstImageLayout: destination.Image.Layout,
            regionCount   : 1U,
            pRegions      : whole_region_base_mip,
            filter        : filter
        );
    }
    
    public void Blit(Image source, Image destination, 
        ImageAspectFlags aspect = ImageAspectFlags.Color,
        vk.Filter filter = vk.Filter.Nearest)
    {
        vk.ImageBlit whole_region_base_mip = new(
            srcSubresource: new vk.ImageSubresourceLayers(
                aspectMask    : aspect.ToVk()     ,
                mipLevel      : 0U                ,
                baseArrayLayer: 0U                ,
                layerCount    : source.ArrayLayers
            ),
            dstSubresource: new vk.ImageSubresourceLayers(
                aspectMask    : aspect.ToVk()     ,
                mipLevel      : 0U                ,
                baseArrayLayer: 0U                ,
                layerCount    : destination.ArrayLayers
            )
        );
        
        Vector3D<u32> src_res = source.Resolution;
        vk.Offset3D src_whole = Unsafe.As<Vector3D<u32>, vk.Offset3D>(ref src_res);
        whole_region_base_mip.SrcOffsets.Element1 = src_whole;
        
        Vector3D<u32> dst_res = destination.Resolution;
        vk.Offset3D dst_whole = Unsafe.As<Vector3D<u32>, vk.Offset3D>(ref dst_res);
        whole_region_base_mip.DstOffsets.Element1 = dst_whole;
        
        VK.API.CmdBlitImage(CommandBuffer,
            srcImage      : (SlimImage)source,
            srcImageLayout: source.Layout,
            dstImage      : (SlimImage)destination,
            dstImageLayout: destination.Layout,
            regionCount   : 1U,
            pRegions      : whole_region_base_mip,
            filter        : filter
        );
    }
}