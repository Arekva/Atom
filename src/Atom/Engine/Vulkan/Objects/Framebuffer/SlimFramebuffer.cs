using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;

namespace Atom.Engine.Vulkan;

public struct SlimFramebuffer
{
    public vk.Framebuffer Handle;
    
#region Creation & Non-API stuff

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe SlimFramebuffer(
        Device device,
        RenderPass renderPass,
        ReadOnlySpan<SlimImageView> attachments,
        uint width, uint height, uint layers,
        FramebufferCreateFlags flags = 0)
    {
        fixed (SlimImageView* p_attachments = attachments)
        {
            FramebufferCreateInfo create_info = new(
                flags: flags,
                renderPass: renderPass,
                attachmentCount: (uint) attachments.Length,
                pAttachments: (vk.ImageView*)p_attachments,
                width: width,
                height: height,
                layers: layers
            );
            
            Result result = VK.API.CreateFramebuffer(device, in create_info, null, out Handle);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Handle.GetHashCode();
    
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Framebuffer(in SlimFramebuffer framebuffer)
        => Unsafe.As<SlimFramebuffer, Framebuffer>(ref Unsafe.AsRef(in framebuffer));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SlimFramebuffer(in Framebuffer framebuffer)
        => Unsafe.As<Framebuffer, SlimFramebuffer>(ref Unsafe.AsRef(in framebuffer));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Silk.NET.Vulkan.Framebuffer(in SlimFramebuffer framebuffer)
        => Unsafe.As<SlimFramebuffer, Silk.NET.Vulkan.Framebuffer>(ref Unsafe.AsRef(in framebuffer));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SlimFramebuffer(in Silk.NET.Vulkan.Framebuffer framebuffer)
        => Unsafe.As<Silk.NET.Vulkan.Framebuffer, SlimFramebuffer>(ref Unsafe.AsRef(in framebuffer));

#endregion
    
#region Standard API Proxying 

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Destroy(Device device) 
        => VK.API.DestroyFramebuffer(device, Handle, ReadOnlySpan<AllocationCallbacks>.Empty);

#endregion

#region User defined

    
    
#endregion
}