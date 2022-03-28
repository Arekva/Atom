using System.Runtime.CompilerServices;
using Atom.Engine.Vulkan;

namespace Atom.Engine;

public class CommandRecorder : IDisposable
{
    public readonly SlimCommandBuffer CommandBuffer;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal unsafe CommandRecorder(SlimCommandBuffer commandBuffer, vk.CommandBufferUsageFlags flags = 0)
    {
        CommandBuffer = commandBuffer;
        
        vk.CommandBufferBeginInfo begin_info = new(flags: flags);
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
        vk.PipelineStageFlags sourceStageMask,
        vk.PipelineStageFlags destinationStageMask,
        ReadOnlySpan<vk.ImageMemoryBarrier> imageMemoryBarriers)
    {
        fixed (vk.ImageMemoryBarrier* p_image_barriers = imageMemoryBarriers) VK.API.CmdPipelineBarrier(
            commandBuffer            :CommandBuffer                  ,
            srcStageMask             : sourceStageMask                ,
            dstStageMask             : destinationStageMask           ,
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
        public readonly SlimCommandBuffer CommandBuffer;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe RenderPassRecorder(
            SlimCommandBuffer commandBuffer, vk.RenderPass renderPass,
            vk.Rect2D renderArea, SlimFramebuffer framebuffer, ReadOnlySpan<vk.ClearValue> clearValues,
            vk.SubpassContents subpassContents = vk.SubpassContents.Inline)
        {
            CommandBuffer = commandBuffer;
            
            fixed (vk.ClearValue* p_clear_values = clearValues)
            {
                vk.RenderPassBeginInfo pass_info = new(
                    renderPass     : renderPass             ,
                    renderArea     : renderArea             ,
                    framebuffer    : framebuffer           ,
                    clearValueCount: (u32)clearValues.Length,
                    pClearValues   : p_clear_values
                );
                VK.API.CmdBeginRenderPass(commandBuffer, in pass_info, subpassContents);
            }
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
    => new (CommandBuffer, renderPass, renderArea, framebuffer, clearValues, subpassContents);
}