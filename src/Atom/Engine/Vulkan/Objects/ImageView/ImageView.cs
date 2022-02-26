using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;

namespace Atom.Engine.Vulkan;

public struct ImageView
{
    private static ConcurrentDictionary<SlimImageView, Device> _views = new();

    public SlimImageView Handle;
    
    public Device Device => _views[Handle];
    
#region Creation & Non-API stuff

    public ImageView(
        SlimImage image,
        ImageViewType viewType, Format format,
        ComponentMapping components,
        vk.ImageSubresourceRange subresourceRange,
        ImageViewCreateFlags flags = 0,
        Device? device = null)
    {
        Device used_device = device ?? VK.Device;

        Handle = new SlimImageView(used_device, image,
            viewType, format,
            components, subresourceRange, 
            flags);

        _views.TryAdd(Handle, used_device);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Handle.GetHashCode();

#endregion

#region Standard API Proxying 
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Destroy()
    {
        if(_views.TryRemove(Handle, out Device device))
        {
            Handle.Destroy(device);
        }
    }

#endregion
}