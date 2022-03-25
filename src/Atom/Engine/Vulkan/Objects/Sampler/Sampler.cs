using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;

namespace Atom.Engine.Vulkan;

public struct Sampler
{
    private static ConcurrentDictionary<SlimSampler, Device> _samplers = new();

    public SlimSampler Handle;
    
    public Device Device => _samplers[Handle];
    
#region Creation & Non-API stuff

    public Sampler(
        Filter magFilter = Filter.Linear, Filter minFilter = Filter.Linear,
        SamplerMipmapMode mipmapMode = SamplerMipmapMode.Linear,
        SamplerAddressMode addressModeU = SamplerAddressMode.Repeat, SamplerAddressMode addressModeV = SamplerAddressMode.Repeat,  SamplerAddressMode addressModeW = SamplerAddressMode.Repeat,
        f32 mipLodBias = 0.0F,
        bool anisotropyEnable = true, f32 maxAnisotropy = 16.0F,
        bool compareEnable = false, CompareOp compareOp = CompareOp.Always,
        f32 minLod = 0.0F, f32 maxLod = 0.0F,
        BorderColor borderColor = BorderColor.IntOpaqueBlack, bool unnormalizedCoordinates = false,
        vk.SamplerCreateFlags flags = 0,
        Device? device = null)
    {
        Device used_device = device ?? VK.Device;

        Handle = new SlimSampler(used_device,
            magFilter, minFilter, 
            mipmapMode,
            addressModeU, addressModeV, addressModeW,
            mipLodBias,
            anisotropyEnable, maxAnisotropy,
            compareEnable, compareOp,
            minLod, maxLod,
            borderColor,  unnormalizedCoordinates, 
            flags);

        _samplers.TryAdd(Handle, used_device);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Handle.GetHashCode();

#endregion

#region Standard API Proxying 
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Destroy()
    {
        if(_samplers.TryRemove(Handle, out Device device))
        {
            Handle.Destroy(device);
        }
    }

#endregion
}