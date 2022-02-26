using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;

namespace Atom.Engine;

public struct SlimSemaphore
{
    public vk.Semaphore Handle;
    
#region Creation & Non-API stuff

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe SlimSemaphore(
        Device device,
        SemaphoreCreateFlags flags = 0)
    {
        SemaphoreCreateInfo create_info = new(flags: (uint)flags);
        
        Result result = VK.API.CreateSemaphore(device, in create_info, null, out Handle);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Handle.GetHashCode();
    
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Semaphore(in SlimSemaphore view)
        => Unsafe.As<SlimSemaphore, Semaphore>(ref Unsafe.AsRef(in view));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SlimSemaphore(in Semaphore view)
        => Unsafe.As<Semaphore, SlimSemaphore>(ref Unsafe.AsRef(in view));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Silk.NET.Vulkan.Semaphore(in SlimSemaphore view)
        => Unsafe.As<SlimSemaphore, Silk.NET.Vulkan.Semaphore>(ref Unsafe.AsRef(in view));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SlimSemaphore(in Silk.NET.Vulkan.Semaphore view)
        => Unsafe.As<Silk.NET.Vulkan.Semaphore, SlimSemaphore>(ref Unsafe.AsRef(in view));

#endregion
    
#region Standard API Proxying 

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Destroy(Device device) => VK.API.DestroySemaphore(device, Handle, ReadOnlySpan<AllocationCallbacks>.Empty);

#endregion

#region User defined


    
#endregion
}