using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;

namespace Atom.Engine.Vulkan;

public struct Framebuffer
{
    private static ConcurrentDictionary<SlimFramebuffer, Device> _framebuffers = new();

    public SlimFramebuffer Handle;
    
    public Device Device => _framebuffers[Handle];
    
#region Creation & Non-API stuff

    public Framebuffer(
        RenderPass renderPass,
        ReadOnlySpan<SlimImageView> attachments,
        uint width, uint height, uint layers,
        FramebufferCreateFlags flags = 0,
        Device? device = null)
    {
        Device used_device = device ?? VK.Device;

        Handle = new SlimFramebuffer(used_device, 
            renderPass: renderPass,
            attachments: attachments,
            width: width,
            height: height,
            layers: layers,
            flags
        );

        _framebuffers.TryAdd(Handle, used_device);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Handle.GetHashCode();

#endregion

#region Standard API Proxying 
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Destroy()
    {
        if(_framebuffers.TryRemove(Handle, out Device device))
        {
            Handle.Destroy(device);
        }
    }

#endregion
}