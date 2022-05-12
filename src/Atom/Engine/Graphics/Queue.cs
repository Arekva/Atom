using Atom.Engine.Vulkan;



namespace Atom.Engine;



/// <summary> Thread safe wrapper for Vk.Queue / SlimQueue </summary>
public class Queue
{
    private Mutex<SlimQueue> _internal;
    private u32              _family  ;



    public u32       Family => _family       ;
    public SlimQueue Unsafe => _internal.Data;

    
    
    internal Queue(SlimQueue queue, u32 family) => (_internal, _family) = (queue, family);



    public void Submit(
        SlimCommandBuffer  commandBuffer                          , 
        PipelineStageFlags waitStage     = PipelineStageFlags.None, 
        SlimFence          signalFence   = default                )
    {
        using MutexLock<SlimQueue> queue_lock = _internal.Lock();
        
        queue_lock.Data.Submit(commandBuffer, waitStage, signalFence);
    }

    public void Submit(
        ReadOnlySpan<SlimSemaphore> waitSemaphores,
        PipelineStageFlags waitStage,
        ReadOnlySpan<SlimCommandBuffer> commandBuffers,
        ReadOnlySpan<SlimSemaphore> signalSemaphores,
        SlimFence? signalFence = null
    )
    {
        using MutexLock<SlimQueue> queue_lock = _internal.Lock();
        
        queue_lock.Data.Submit(waitSemaphores, waitStage, commandBuffers, signalSemaphores, signalFence);
    }

    public void Present(
        khr.KhrSwapchain extension                ,
        ReadOnlySpan<SlimSemaphore> waitSemaphores,
        ReadOnlySpan<vk.SwapchainKHR> swapchains  , 
        ReadOnlySpan<u32> imageIndices
    )
    {
        using MutexLock<SlimQueue> queue_lock = _internal.Lock();
        
        queue_lock.Data.Present(extension, waitSemaphores, swapchains, imageIndices);
    }
}