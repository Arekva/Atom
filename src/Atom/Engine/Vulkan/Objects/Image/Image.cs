using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;

namespace Atom.Engine.Vulkan;

public struct Image
{
    private static ConcurrentDictionary<SlimImage, Device> _images = new();

    public SlimImage Handle;
    
    public Device Device => _images[Handle];
    
#region Creation & Non-API stuff

    public Image(
        ImageType type, ImageFormat format, Extent3D extent,
        uint mipLevels, uint arrayLayers,
        SampleCountFlags samples,
        ImageTiling tiling, ImageUsageFlags usage,
        SharingMode sharingMode, ReadOnlySpan<uint> queueFamilyIndices,
        ImageLayout initialLayout,
        ImageCreateFlags flags = 0, Device? device = null)
    {
        Device used_device = device ?? VK.Device;

        Handle = new SlimImage(used_device,
            type, format, extent,
            mipLevels, arrayLayers, samples, 
            tiling, usage,
            sharingMode, queueFamilyIndices, 
            initialLayout, 
            flags);

        _images.TryAdd(Handle, used_device);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Handle.GetHashCode();

#endregion

#region Standard API Proxying 
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Destroy()
    {
        if(_images.TryRemove(Handle, out Device device))
        {
            Handle.Destroy(device);
        }
    }

#endregion
}