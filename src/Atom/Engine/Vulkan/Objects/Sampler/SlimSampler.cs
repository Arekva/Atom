using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;

namespace Atom.Engine.Vulkan;

public struct SlimSampler
{
    public vk.Sampler Handle;
    
#region Creation & Non-API stuff

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe SlimSampler(Device device,
        Filter magFilter = Filter.Linear, Filter minFilter = Filter.Linear,
        SamplerMipmapMode mipmapMode = SamplerMipmapMode.Linear,
        SamplerAddressMode addressModeU = SamplerAddressMode.Repeat, SamplerAddressMode addressModeV = SamplerAddressMode.Repeat,  SamplerAddressMode addressModeW = SamplerAddressMode.Repeat,
        f32 mipLodBias = 0.0F,
        bool anisotropyEnable = true, f32 maxAnisotropy = 16.0F,
        bool compareEnable = false, CompareOp compareOp = CompareOp.Always,
        f32 minLod = 0.0F, f32 maxLod = 0.0F,
        BorderColor borderColor = BorderColor.IntOpaqueBlack, bool unnormalizedCoordinates = false,
        vk.SamplerCreateFlags flags = 0)
    {
        SamplerCreateInfo create_info = new(
            flags: flags,
            magFilter: magFilter,
            minFilter: minFilter,
            mipmapMode: mipmapMode,
            addressModeU: addressModeU,
            addressModeV: addressModeV,
            addressModeW: addressModeW,
            mipLodBias: mipLodBias,
            anisotropyEnable: anisotropyEnable,
            maxAnisotropy: maxAnisotropy,
            compareEnable: compareEnable,
            compareOp: compareOp,
            minLod: minLod,
            maxLod: maxLod,
            borderColor: borderColor,
            unnormalizedCoordinates: unnormalizedCoordinates
        );
        
        Result result = VK.API.CreateSampler(device, in create_info, null, out Handle);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Handle.GetHashCode();
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Sampler(in SlimSampler sampler)
        => Unsafe.As<SlimSampler, Sampler>(ref Unsafe.AsRef(in sampler));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SlimSampler(in Sampler sampler)
        => Unsafe.As<Sampler, SlimSampler>(ref Unsafe.AsRef(in sampler));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Silk.NET.Vulkan.Sampler(in SlimSampler sampler)
        => Unsafe.As<SlimSampler, Silk.NET.Vulkan.Sampler>(ref Unsafe.AsRef(in sampler));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SlimSampler(in Silk.NET.Vulkan.Sampler sampler)
        => Unsafe.As<Silk.NET.Vulkan.Sampler, SlimSampler>(ref Unsafe.AsRef(in sampler));

#endregion
    
#region Standard API Proxying 

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Destroy(Device device) => VK.API.DestroySampler(device, Handle, ReadOnlySpan<AllocationCallbacks>.Empty);

#endregion

#region User defined


    
#endregion
}