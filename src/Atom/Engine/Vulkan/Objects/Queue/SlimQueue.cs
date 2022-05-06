using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace Atom.Engine.Vulkan;

public struct SlimQueue
{
    public vk.Queue Handle;
    
#region Creation & Non-API stuff

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override i32 GetHashCode() => Handle.GetHashCode();
    
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Queue(in SlimQueue queue)
        => Unsafe.As<SlimQueue, Queue>(ref Unsafe.AsRef(in queue));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SlimQueue(in Queue queue)
        => Unsafe.As<Queue, SlimQueue>(ref Unsafe.AsRef(in queue));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Silk.NET.Vulkan.Queue(in SlimQueue queue)
        => Unsafe.As<SlimQueue, Silk.NET.Vulkan.Queue>(ref Unsafe.AsRef(in queue));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SlimQueue(in Silk.NET.Vulkan.Queue queue)
        => Unsafe.As<Silk.NET.Vulkan.Queue, SlimQueue>(ref Unsafe.AsRef(in queue));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator u64(in SlimQueue queue)
        => Unsafe.As<SlimQueue, u64>(ref Unsafe.AsRef(in queue));

#endregion
    
#region Standard API Proxying

    public unsafe void Present(khr.KhrSwapchain extension,
        ReadOnlySpan<SlimSemaphore> waitSemaphores,
        ReadOnlySpan<vk.SwapchainKHR> swapchains, 
        ReadOnlySpan<u32> imageIndices)
    {
        Span<vk.Result> results = stackalloc vk.Result[swapchains.Length];
        
        fixed (vk.Result* p_results             = results)
        fixed (u32* p_image_indices             = imageIndices)
        fixed (vk.SwapchainKHR* p_swapchains    = swapchains)
        fixed (SlimSemaphore* p_wait_semaphores = waitSemaphores)
        {
            vk.PresentInfoKHR info = new(
                waitSemaphoreCount: (u32)waitSemaphores.Length,
                pWaitSemaphores   : (vk.Semaphore*)p_wait_semaphores,
                
                swapchainCount    : (u32)swapchains.Length,
                pSwapchains       : p_swapchains,
                pImageIndices     : p_image_indices,
                pResults          : p_results
            );
        
            khr.KhrSwapchainOverloads.QueuePresent(extension, this, info.AsSpan());
        }
    }

    public unsafe void Submit( 
        ReadOnlySpan<SlimSemaphore> waitSemaphores,
        PipelineStageFlags waitStage,
        ReadOnlySpan<SlimCommandBuffer> commandBuffers,
        ReadOnlySpan<SlimSemaphore> signalSemaphores,
        SlimFence? signalFence = null
    )
    {
        vk.PipelineStageFlags vk_wait_stage = waitStage.ToVk();
        
        fixed (SlimSemaphore*     p_wait_semaphores   = waitSemaphores  )
        fixed (SlimCommandBuffer* p_command_buffers   = commandBuffers  )
        fixed (SlimSemaphore*     p_signal_semaphores = signalSemaphores)
        {
            vk.SubmitInfo info = new(
                waitSemaphoreCount  : (u32)waitSemaphores.Length            ,
                pWaitSemaphores     : (vk.Semaphore*)    p_wait_semaphores  ,
                pWaitDstStageMask   : &vk_wait_stage                         ,
                commandBufferCount  : (u32)commandBuffers.Length            ,
                pCommandBuffers     : (vk.CommandBuffer*)p_command_buffers  ,
                signalSemaphoreCount: (u32)signalSemaphores.Length          ,
                pSignalSemaphores   : (vk.Semaphore*)    p_signal_semaphores
            );

            VK.API.QueueSubmit(Handle, 1U, in info, signalFence ?? new SlimFence());
        }
    }

#endregion

#region User defined

    public unsafe void Submit(SlimCommandBuffer commandBuffer, PipelineStageFlags waitStage, SlimFence fence)
    {
        vk.PipelineStageFlags vk_wait_stage = waitStage.ToVk();
        vk.CommandBuffer vk_cmd = commandBuffer;
        
        vk.SubmitInfo info = new(
            pWaitDstStageMask   : &vk_wait_stage,
            commandBufferCount  : 1U            ,
            pCommandBuffers     : &vk_cmd
        );
        
        VK.API.QueueSubmit(Handle, 1U, in info, fence);
        /*vk.CommandBufferSubmitInfo cmd_info = new(
            commandBuffer: commandBuffer
        );
        
        vk.SubmitInfo2 info = new(
            commandBufferInfoCount: 1,
            pCommandBufferInfos: &cmd_info
        );
        
        vk.VkOverloads.QueueSubmit2(VK.API, this, 1U, info.AsSpan(), fence);*/
    }

#endregion
}